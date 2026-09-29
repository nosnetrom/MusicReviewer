using Microsoft.EntityFrameworkCore;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Application.Catalog;
using MusicReviewer.Domain.Catalog;
using MusicReviewer.Domain.Ingestion;

namespace MusicReviewer.Application.Ingestion;

/// <summary>Artist details, Wikidata link, genres, and all studio albums and EPs.</summary>
public sealed class ArtistDiscographyJobHandler(
    IMusicReviewerDbContext db,
    IMusicBrainzClient musicBrainz,
    ArtistImporter importer,
    TimeProvider clock) : IIngestionJobHandler
{
    public IngestionJobType Type => IngestionJobType.ArtistDiscography;

    public async Task HandleAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        var artist = await db.Artists.Include(a => a.Genres).FirstOrDefaultAsync(a => a.Id == job.TargetId, cancellationToken);
        if (artist is null)
            return;

        // A refresh of an already-imported artist keeps showing the existing data.
        if (artist.SyncStatus != SyncStatus.Ready)
        {
            artist.SyncStatus = SyncStatus.Syncing;
            await db.SaveChangesAsync(cancellationToken);
        }

        var info = await musicBrainz.GetArtistAsync(artist.MusicBrainzId, RequestPriority.Background, cancellationToken);
        if (info is null)
        {
            artist.SyncStatus = SyncStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        ArtistMapping.Apply(artist, info);
        await ArtistMapping.ApplyGenresAsync(db, artist, info.Genres, cancellationToken);
        await importer.ApplyArtistWikidataAsync(artist, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await importer.ImportCategoryAsync(artist, ReleaseCategories.Studio, cancellationToken);

        artist.LastSyncedUtc = clock.GetUtcNow().UtcDateTime;
        artist.SyncStatus = SyncStatus.Ready;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task OnFailedAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        var artist = await db.Artists.FirstOrDefaultAsync(a => a.Id == job.TargetId, cancellationToken);
        if (artist is { SyncStatus: not SyncStatus.Ready })
        {
            artist.SyncStatus = SyncStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

/// <summary>Re-classifies an already-imported artist's genres with a single MusicBrainz lookup.</summary>
public sealed class ArtistGenresJobHandler(IMusicReviewerDbContext db, IMusicBrainzClient musicBrainz) : IIngestionJobHandler
{
    public IngestionJobType Type => IngestionJobType.ArtistGenres;

    public async Task HandleAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        var artist = await db.Artists.Include(a => a.Genres).FirstOrDefaultAsync(a => a.Id == job.TargetId, cancellationToken);
        if (artist is null)
            return;

        var info = await musicBrainz.GetArtistAsync(artist.MusicBrainzId, RequestPriority.Background, cancellationToken);
        if (info is null)
            return;

        await ArtistMapping.ApplyGenresAsync(db, artist, info.Genres, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task OnFailedAsync(IngestionJob job, CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Live albums or compilations, imported only when a visitor asks for them.</summary>
public sealed class ArtistReleaseCategoryJobHandler(IMusicReviewerDbContext db, ArtistImporter importer) : IIngestionJobHandler
{
    public IngestionJobType Type => IngestionJobType.ArtistReleaseCategory;

    public async Task HandleAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ReleaseCategories>(job.Parameter, ignoreCase: true, out var category) || category == ReleaseCategories.None)
            throw new InvalidOperationException($"Unknown release category '{job.Parameter}'.");

        var artist = await db.Artists.FirstOrDefaultAsync(a => a.Id == job.TargetId, cancellationToken);
        if (artist is null)
            return;

        await importer.ImportCategoryAsync(artist, category, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task OnFailedAsync(IngestionJob job, CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Track list, label and personnel from the recording's representative release.</summary>
public sealed class RecordingDetailsJobHandler(
    IMusicReviewerDbContext db,
    IMusicBrainzClient musicBrainz,
    TimeProvider clock) : IIngestionJobHandler
{
    public IngestionJobType Type => IngestionJobType.RecordingDetails;

    public async Task HandleAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        var recording = await db.Recordings
            .Include(r => r.Tracks)
            .Include(r => r.Credits)
            .FirstOrDefaultAsync(r => r.Id == job.TargetId, cancellationToken);
        if (recording is null)
            return;

        if (recording.DetailsSyncStatus != SyncStatus.Ready)
        {
            recording.DetailsSyncStatus = SyncStatus.Syncing;
            await db.SaveChangesAsync(cancellationToken);
        }

        var candidates = await musicBrainz.GetReleaseCandidatesAsync(recording.MusicBrainzId, recording.FirstReleaseYear, cancellationToken);
        var chosen = RepresentativeRelease.Pick(candidates, recording.FirstReleaseYear);
        var detail = chosen is null ? null : await musicBrainz.GetReleaseAsync(chosen.Id, cancellationToken);

        if (detail is not null)
        {
            db.Tracks.RemoveRange(recording.Tracks);
            db.Credits.RemoveRange(recording.Credits);
            recording.Tracks.Clear();
            recording.Credits.Clear();

            // Added through the DbSets: with client-generated keys, EF would treat items attached
            // via the navigation collection as existing rows and issue UPDATEs.
            db.Tracks.AddRange(detail.Tracks.Select(track => new Track
            {
                Id = Guid.CreateVersion7(),
                RecordingId = recording.Id,
                DiscNumber = track.DiscNumber,
                Position = track.Position,
                Title = track.Title,
                DurationMs = track.DurationMs,
            }));

            db.Credits.AddRange(detail.Credits.Select(credit => new Credit
            {
                Id = Guid.CreateVersion7(),
                RecordingId = recording.Id,
                PersonName = credit.PersonName,
                Role = credit.Role,
                Instrument = credit.Instrument,
            }));

            recording.Label = detail.Label ?? recording.Label;
            recording.RepresentativeReleaseId = detail.MusicBrainzId;
        }

        recording.DetailsSyncedUtc = clock.GetUtcNow().UtcDateTime;
        recording.DetailsSyncStatus = SyncStatus.Ready;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task OnFailedAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        var recording = await db.Recordings.FirstOrDefaultAsync(r => r.Id == job.TargetId, cancellationToken);
        if (recording is { DetailsSyncStatus: not SyncStatus.Ready })
        {
            recording.DetailsSyncStatus = SyncStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
