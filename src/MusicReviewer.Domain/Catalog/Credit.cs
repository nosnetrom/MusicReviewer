namespace MusicReviewer.Domain.Catalog;

/// <summary>A personnel credit on a recording, e.g. "John Coltrane — tenor saxophone".</summary>
public class Credit
{
    public Guid Id { get; set; }
    public Guid RecordingId { get; set; }
    public required string PersonName { get; set; }
    public required string Role { get; set; }
    public string? Instrument { get; set; }
}
