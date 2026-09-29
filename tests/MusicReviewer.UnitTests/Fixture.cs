using System.Text.Json;
using MusicReviewer.Infrastructure.MusicBrainz;

namespace MusicReviewer.UnitTests;

/// <summary>Recorded responses from MusicBrainz and Wikidata (September 2026), stored in Fixtures/.</summary>
internal static class Fixture
{
    public static string Read(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    public static T MusicBrainz<T>(string name) =>
        JsonSerializer.Deserialize<T>(Read(name), MusicBrainzMapper.JsonOptions)
            ?? throw new InvalidOperationException($"Fixture {name} deserialized to null.");

    public static T Json<T>(string name) =>
        JsonSerializer.Deserialize<T>(Read(name), JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException($"Fixture {name} deserialized to null.");

    public static readonly Guid MilesDavis = Guid.Parse("561d854a-6a28-4aa7-8c99-323e6ce46c2a");
}
