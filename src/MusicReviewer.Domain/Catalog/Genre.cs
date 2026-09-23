namespace MusicReviewer.Domain.Catalog;

public class Genre
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }

    public List<Artist> Artists { get; } = [];
    public List<Recording> Recordings { get; } = [];
}
