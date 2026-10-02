namespace MusicReviewer.Application.Abstractions;

public interface IBrowseRecordingQuery
{
    Task<BrowseRecordingPage> GetPageAsync(string? genre, int? decadeStart, int offset, int limit, CancellationToken cancellationToken);
}

public sealed record BrowseRecordingPage(IReadOnlyList<Guid> MusicBrainzIds, int Total);