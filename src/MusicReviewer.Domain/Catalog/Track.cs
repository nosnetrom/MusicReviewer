namespace MusicReviewer.Domain.Catalog;

public class Track
{
    public Guid Id { get; set; }
    public Guid RecordingId { get; set; }
    public int DiscNumber { get; set; } = 1;
    public int Position { get; set; }
    public required string Title { get; set; }
    public int? DurationMs { get; set; }
}
