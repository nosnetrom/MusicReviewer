using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Application.Ingestion;
using MusicReviewer.Domain.Catalog;
using MusicReviewer.Domain.Ingestion;

namespace MusicReviewer.Application.Catalog;

/// <summary>
/// Read side of the catalog. Serves from the database, creating artists and recordings from
/// MusicBrainz on first view and queueing imports for anything missing or stale.
/// </summary>
public sealed partial class CatalogService(
    IMusicReviewerDbContext db,
    IMusicBrainzClient musicBrainz,
    IIngestionScheduler scheduler,
    IMemoryCache cache,
    IOptions<CatalogOptions> options,
    TimeProvider clock,
    ILogger<CatalogService> logger)
{
    private const int MaxSearchResults = 12;
    private const int MaxFeaturedRecordings = 24;
    /// <summary>Albums per browse page; "Load more" fetches the next page.</summary>
    public const int BrowsePageSize = 60;
    public const int MaxBrowseRecordingsPerArtist = 3;

    /// <summary>Upper bound on albums considered for one browse view.</summary>
    private const int BrowseCandidates = 5000;
    private const int MaxGenres = 40;

    private static readonly string[] RoleOrder =
        ["Performer", "Vocals", "Conductor", "Arranger", "Composer", "Producer", "Engineer", "Recording engineer", "Mixing", "Mastering"];

    // ---- Search ------------------------------------------------------------------------

    public async Task<SearchResultDto> SearchAsync(string? query, CancellationToken cancellationToken)
    {
        var text = query?.Trim() ?? "";
        var normalized = NameNormalizer.Normalize(text);
        if (normalized.Length < 2)
            return new SearchResultDto(text, [], RemoteAvailable: true);

        var local = await db.Artists.AsNoTracking()
            .Where(a => a.NormalizedName.Contains(normalized))
            .OrderByDescending(a => a.IsFeatured)
            .ThenBy(a => a.Name.Length)
            .Take(MaxSearchResults)
            .Select(ToArtistSummary)
            .ToListAsync(cancellationToken);

        var (remote, remoteAvailable) = await SearchRemoteAsync(text, normalized, cancellationToken);

        var remoteIds = remote.Select(r => r.MusicBrainzId).ToList();
        var imported = (await db.Artists.AsNoTracking()
                .Where(a => remoteIds.Contains(a.MusicBrainzId) && a.LastSyncedUtc != null)
                .Select(a => a.MusicBrainzId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var artists = remote
            .Select(r => new ArtistSummaryDto(r.MusicBrainzId, r.Name, r.Disambiguation, r.Type, r.Country, r.BeginYear, r.EndYear, imported.Contains(r.MusicBrainzId)))
            .Concat(local.Where(l => !remoteIds.Contains(l.Mbid)))
            .Take(MaxSearchResults)
            .ToList();

        return new SearchResultDto(text, artists, remoteAvailable);
    }

    private async Task<(IReadOnlyList<ArtistInfo> Results, bool Available)> SearchRemoteAsync(string text, string normalized, CancellationToken cancellationToken)
    {
        var key = $"mb-artist-search:{normalized}";
        if (cache.TryGetValue(key, out IReadOnlyList<ArtistInfo>? cached) && cached is not null)
            return (cached, true);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.RemoteSearchTimeout);

        try
        {
            var results = await musicBrainz.SearchArtistsAsync(text, MaxSearchResults, timeout.Token);
            cache.Set(key, results, options.Value.SearchCacheDuration);
            return (results, true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogRemoteSearchUnavailable(logger, text, "timed out");
            return ([], false);
        }
        catch (ExternalServiceUnavailableException ex)
        {
            LogRemoteSearchUnavailable(logger, text, ex.Message);
            return ([], false);
        }
    }

    // ---- Artists -----------------------------------------------------------------------

    public async Task<ArtistDetailDto> GetArtistAsync(Guid mbid, CancellationToken cancellationToken)
    {
        var artist = await db.Artists.Include(a => a.Genres).FirstOrDefaultAsync(a => a.MusicBrainzId == mbid, cancellationToken)
            ?? await CreateArtistAsync(mbid, RequestPriority.Interactive, cancellationToken);

        await EnsureDiscographyAsync(artist, JobPriority.UserRequested, cancellationToken);

        var count = await db.Recordings.CountAsync(r => r.ArtistId == artist.Id, cancellationToken);
        return new ArtistDetailDto(
            artist.MusicBrainzId, artist.Name, artist.SortName, artist.Disambiguation, artist.Type, artist.Country,
            artist.BeginYear, artist.EndYear, artist.WikipediaTitle,
            [.. artist.Genres.OrderBy(g => FamilyOrder(g.Slug)).Select(g => new GenreDto(g.Name, g.Slug))],
            string.IsNullOrEmpty(artist.Styles) ? [] : artist.Styles.Split(", "),
            artist.SyncStatus, count);
    }

    public async Task<ArtistRecordingsDto> GetArtistRecordingsAsync(Guid mbid, RecordingFilter filter, RecordingSort sort, CancellationToken cancellationToken)
    {
        var artist = await db.Artists.FirstOrDefaultAsync(a => a.MusicBrainzId == mbid, cancellationToken)
            ?? throw new NotFoundException($"Artist {mbid} has not been loaded.");

        var category = filter switch
        {
            RecordingFilter.Live => ReleaseCategories.Live,
            RecordingFilter.Compilation => ReleaseCategories.Compilation,
            _ => ReleaseCategories.Studio,
        };

        var status = category == ReleaseCategories.Studio || artist.ImportedCategories.HasFlag(category)
            ? artist.ImportedCategories.HasFlag(ReleaseCategories.Studio) ? SyncStatus.Ready : artist.SyncStatus
            : await EnsureCategoryAsync(artist, category, cancellationToken);

        var query = db.Recordings.AsNoTracking().Where(r => r.ArtistId == artist.Id);
        query = filter switch
        {
            RecordingFilter.Studio => query.Where(r => r.SecondaryTypes == SecondaryReleaseTypes.None && r.PrimaryType == ReleaseType.Album),
            RecordingFilter.EP => query.Where(r => r.SecondaryTypes == SecondaryReleaseTypes.None && r.PrimaryType == ReleaseType.EP),
            RecordingFilter.Live => query.Where(r => (r.SecondaryTypes & SecondaryReleaseTypes.Live) != 0),
            RecordingFilter.Compilation => query.Where(r =>
                (r.SecondaryTypes & SecondaryReleaseTypes.Compilation) != 0 && (r.SecondaryTypes & SecondaryReleaseTypes.Live) == 0),
            _ => query,
        };

        query = sort == RecordingSort.Date
            ? query.OrderBy(r => r.FirstReleaseDate == null).ThenBy(r => r.FirstReleaseDate).ThenBy(r => r.Title)
            : query.OrderByDescending(r => r.NotabilityScore).ThenBy(r => r.FirstReleaseDate);

        var recordings = await query.Select(ToRecordingSummary).ToListAsync(cancellationToken);
        return new ArtistRecordingsDto(filter, status, recordings);
    }

    // ---- Recordings --------------------------------------------------------------------

    public async Task<RecordingDetailDto> GetRecordingAsync(Guid mbid, CancellationToken cancellationToken)
    {
        var recording = await db.Recordings
                .Include(r => r.Artist)
                .Include(r => r.Tracks)
                .Include(r => r.Credits)
                .FirstOrDefaultAsync(r => r.MusicBrainzId == mbid, cancellationToken)
            ?? await CreateRecordingAsync(mbid, cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;
        var stale = recording.DetailsSyncedUtc is null || now - recording.DetailsSyncedUtc > options.Value.RefreshAfter;
        if (recording.DetailsSyncStatus is SyncStatus.NotSynced or SyncStatus.Failed || (recording.DetailsSyncStatus == SyncStatus.Ready && stale))
        {
            var priority = recording.DetailsSyncStatus == SyncStatus.Ready ? JobPriority.Background : JobPriority.UserRequested;
            await scheduler.EnqueueAsync(IngestionJobType.RecordingDetails, recording.Id, priority: priority, cancellationToken: cancellationToken);
            if (recording.DetailsSyncStatus != SyncStatus.Ready)
            {
                recording.DetailsSyncStatus = SyncStatus.Syncing;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return new RecordingDetailDto(
            recording.MusicBrainzId, recording.Title, recording.ArtistCredit, recording.Artist.MusicBrainzId, recording.Artist.Name,
            recording.PrimaryType, recording.Category, recording.FirstReleaseDate, recording.FirstReleaseYear, recording.Label,
            recording.CoverArtUrl, recording.WikipediaTitle, recording.DetailsSyncStatus,
            [.. recording.Tracks.OrderBy(t => t.DiscNumber).ThenBy(t => t.Position).Select(t => new TrackDto(t.DiscNumber, t.Position, t.Title, t.DurationMs))],
            GroupCredits(recording.Credits));
    }

    // ---- Browse ------------------------------------------------------------------------

    public async Task<FeaturedDto> GetFeaturedAsync(CancellationToken cancellationToken)
    {
        var artists = await db.Artists.AsNoTracking()
            .Where(a => a.IsFeatured && a.LastSyncedUtc != null)
            .OrderBy(a => a.SortName)
            .Select(ToArtistSummary)
            .ToListAsync(cancellationToken);

        var candidates = await db.Recordings.AsNoTracking()
            .Where(r => r.Artist.IsFeatured && r.Artist.LastSyncedUtc != null
                && r.SecondaryTypes == SecondaryReleaseTypes.None && r.PrimaryType == ReleaseType.Album)
            .OrderByDescending(r => r.NotabilityScore)
            .Select(ToRecordingSummary)
            .ToListAsync(cancellationToken);

        // Each featured artist's most notable album, most notable first.
        var recordings = candidates
            .GroupBy(r => r.ArtistMbid)
            .Select(g => g.First())
            .OrderByDescending(r => r.Notability)
            .Take(MaxFeaturedRecordings)
            .ToList();

        return new FeaturedDto(artists, recordings);
    }

    public async Task<IReadOnlyList<GenreListItemDto>> GetGenresAsync(CancellationToken cancellationToken)
    {
        var genres = await db.Genres.AsNoTracking()
            .Select(g => new { g.Name, g.Slug, Count = g.Artists.Count(a => a.LastSyncedUtc != null) })
            .Where(g => g.Count > 0)
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Name)
            .Take(MaxGenres)
            .ToListAsync(cancellationToken);

        return [.. genres.Select(g => new GenreListItemDto(g.Name, g.Slug, g.Count))];
    }

    public async Task<BrowseRecordingsDto> BrowseRecordingsAsync(string? genre, string? decade, int offset, int? limit, CancellationToken cancellationToken)
    {
        var query = db.Recordings.AsNoTracking()
            .Where(r => r.Artist.LastSyncedUtc != null
                && r.SecondaryTypes == SecondaryReleaseTypes.None && r.PrimaryType == ReleaseType.Album);

        if (!string.IsNullOrWhiteSpace(genre))
            query = query.Where(r => r.Artist.Genres.Any(g => g.Slug == genre));

        var decadeStart = ParseDecade(decade);
        if (decadeStart is { } start)
            query = query.Where(r => r.FirstReleaseYear >= start && r.FirstReleaseYear < start + 10);

        var candidates = await query
            .OrderByDescending(r => r.NotabilityScore)
            .Take(BrowseCandidates)
            .Select(ToRecordingSummary)
            .ToListAsync(cancellationToken);

        var ranked = RankForBrowse(candidates);
        var first = Math.Max(0, offset);
        var size = Math.Clamp(limit ?? BrowsePageSize, 1, BrowsePageSize);
        var page = ranked.Skip(first).Take(size).ToList();

        return new BrowseRecordingsDto(
            genre,
            decadeStart is null ? null : $"{decadeStart}s",
            page,
            first,
            ranked.Count,
            HasMore: first + page.Count < ranked.Count);
    }

    /// <summary>
    /// Orders browse results in rounds of <see cref="MaxBrowseRecordingsPerArtist"/> albums per artist:
    /// every artist's top three (by notability) first, then everyone's next three, and so on. The first
    /// page never shows more than three albums by one artist, and "Load more" reaches every artist
    /// before repeating one.
    /// </summary>
    /// <param name="byNotability">Recordings already sorted most notable first.</param>
    public static List<RecordingSummaryDto> RankForBrowse(IEnumerable<RecordingSummaryDto> byNotability)
    {
        var seen = new Dictionary<Guid, int>();
        return
        [
            .. byNotability
                .Select((recording, index) =>
                {
                    var rankForArtist = seen.GetValueOrDefault(recording.ArtistMbid);
                    seen[recording.ArtistMbid] = rankForArtist + 1;
                    return (recording, index, round: rankForArtist / MaxBrowseRecordingsPerArtist);
                })
                .ToList()
                .OrderBy(x => x.round)
                .ThenBy(x => x.index)
                .Select(x => x.recording),
        ];
    }

    private static int FamilyOrder(string slug)
    {
        for (var i = 0; i < GenreFamilies.All.Count; i++)
        {
            if (GenreFamilies.All[i].Slug == slug)
                return i;
        }

        return int.MaxValue;
    }

    /// <summary>Accepts "1970s" or "1970".</summary>
    public static int? ParseDecade(string? decade)
    {
        if (string.IsNullOrWhiteSpace(decade))
            return null;

        var digits = decade.Trim().TrimEnd('s', 'S');
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var year) && year is >= 1900 and <= 2100 && year % 10 == 0
            ? year
            : null;
    }

    // ---- Creation and freshness ----------------------------------------------------------

    internal async Task<Artist> CreateArtistAsync(Guid mbid, RequestPriority priority, CancellationToken cancellationToken)
    {
        var info = await musicBrainz.GetArtistAsync(mbid, priority, cancellationToken)
            ?? throw new NotFoundException($"MusicBrainz has no artist {mbid}.");

        var artist = ArtistMapping.Create(info);
        await ArtistMapping.ApplyGenresAsync(db, artist, info.Genres, cancellationToken);
        db.Artists.Add(artist);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return artist;
        }
        catch (DbUpdateException)
        {
            // Another request created the same artist (or genre) first; use what it saved.
            db.ChangeTracker.Clear();
            return await db.Artists.Include(a => a.Genres).FirstOrDefaultAsync(a => a.MusicBrainzId == mbid, cancellationToken)
                ?? throw new InvalidOperationException($"Artist {mbid} could not be saved.");
        }
    }

    private async Task<Recording> CreateRecordingAsync(Guid mbid, CancellationToken cancellationToken)
    {
        var info = await musicBrainz.GetReleaseGroupAsync(mbid, RequestPriority.Interactive, cancellationToken)
            ?? throw new NotFoundException($"MusicBrainz has no release group {mbid}.");
        var artistMbid = info.PrimaryArtistId
            ?? throw new NotFoundException($"Release group {mbid} has no credited artist.");

        var artist = await db.Artists.FirstOrDefaultAsync(a => a.MusicBrainzId == artistMbid, cancellationToken)
            ?? await CreateArtistAsync(artistMbid, RequestPriority.Interactive, cancellationToken);
        await EnsureDiscographyAsync(artist, JobPriority.UserRequested, cancellationToken);

        var recording = new Recording
        {
            Id = Guid.CreateVersion7(),
            ArtistId = artist.Id,
            Artist = artist,
            MusicBrainzId = info.MusicBrainzId,
            Title = info.Title,
            ArtistCredit = info.ArtistCredit,
            PrimaryType = info.PrimaryType,
            SecondaryTypes = info.SecondaryTypes,
            FirstReleaseDate = info.FirstReleaseDate,
            FirstReleaseYear = PartialDate.Year(info.FirstReleaseDate),
            LastSyncedUtc = clock.GetUtcNow().UtcDateTime,
        };
        recording.RecalculateNotability();
        db.Recordings.Add(recording);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return recording;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return await db.Recordings.Include(r => r.Artist).Include(r => r.Tracks).Include(r => r.Credits)
                    .FirstOrDefaultAsync(r => r.MusicBrainzId == mbid, cancellationToken)
                ?? throw new InvalidOperationException($"Recording {mbid} could not be saved.");
        }
    }

    internal async Task EnsureDiscographyAsync(Artist artist, JobPriority priority, CancellationToken cancellationToken)
    {
        var needsImport = artist.SyncStatus is SyncStatus.NotSynced or SyncStatus.Failed;
        var stale = artist.SyncStatus == SyncStatus.Ready && artist.IsStale(clock.GetUtcNow().UtcDateTime, options.Value.RefreshAfter);
        if (!needsImport && !stale)
            return;

        await scheduler.EnqueueAsync(
            IngestionJobType.ArtistDiscography,
            artist.Id,
            priority: stale ? JobPriority.Background : priority,
            cancellationToken: cancellationToken);

        if (needsImport)
        {
            artist.SyncStatus = SyncStatus.Syncing;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<SyncStatus> EnsureCategoryAsync(Artist artist, ReleaseCategories category, CancellationToken cancellationToken)
    {
        var parameter = category.ToString();
        var latest = await db.IngestionJobs.AsNoTracking()
            .Where(j => j.Type == IngestionJobType.ArtistReleaseCategory && j.TargetId == artist.Id && j.Parameter == parameter)
            .OrderByDescending(j => j.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest?.Status is IngestionJobStatus.Queued or IngestionJobStatus.Running)
            return SyncStatus.Syncing;

        // Don't immediately retry something that just failed on every attempt.
        if (latest is { Status: IngestionJobStatus.Failed, CompletedUtc: { } failedAt }
            && clock.GetUtcNow().UtcDateTime - failedAt < TimeSpan.FromMinutes(5))
            return SyncStatus.Failed;

        await scheduler.EnqueueAsync(IngestionJobType.ArtistReleaseCategory, artist.Id, parameter, JobPriority.UserRequested, cancellationToken);
        return SyncStatus.Syncing;
    }

    // ---- Projections -------------------------------------------------------------------

    private static readonly Expression<Func<Artist, ArtistSummaryDto>> ToArtistSummary = a =>
        new ArtistSummaryDto(a.MusicBrainzId, a.Name, a.Disambiguation, a.Type, a.Country, a.BeginYear, a.EndYear, a.LastSyncedUtc != null);

    private static readonly Expression<Func<Recording, RecordingSummaryDto>> ToRecordingSummary = r =>
        new RecordingSummaryDto(
            r.MusicBrainzId,
            r.Title,
            r.ArtistCredit,
            r.Artist.MusicBrainzId,
            r.Artist.Name,
            r.PrimaryType,
            r.SecondaryTypes == SecondaryReleaseTypes.None ? ReleaseCategories.Studio
                : (r.SecondaryTypes & SecondaryReleaseTypes.Live) != 0 ? ReleaseCategories.Live
                : (r.SecondaryTypes & SecondaryReleaseTypes.Compilation) != 0 ? ReleaseCategories.Compilation
                : ReleaseCategories.None,
            r.FirstReleaseYear,
            r.CoverArtUrl,
            r.NotabilityScore,
            r.WikipediaTitle != null);

    private static List<CreditGroupDto> GroupCredits(IEnumerable<Credit> credits) =>
    [
        .. credits
            .GroupBy(c => c.Role)
            .OrderBy(g => Array.IndexOf(RoleOrder, g.Key) is var i && i >= 0 ? i : RoleOrder.Length)
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new CreditGroupDto(
                g.Key,
                [
                    .. g.GroupBy(c => c.PersonName).Select(person => new CreditDto(
                        person.Key,
                        string.Join(", ", person.Select(c => c.Instrument).OfType<string>().Distinct()) is { Length: > 0 } joined ? joined : null)),
                ])),
    ];

    [LoggerMessage(Level = LogLevel.Warning, Message = "Live MusicBrainz search for '{Query}' unavailable ({Reason}); returning local results only")]
    private static partial void LogRemoteSearchUnavailable(ILogger logger, string query, string reason);
}
