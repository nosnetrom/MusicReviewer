using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.UnitTests.Catalog;

public class RecordingTests
{
    [Fact]
    public void Album_with_no_secondary_types_is_a_studio_album()
    {
        var recording = new Recording { Title = "Kind of Blue", PrimaryType = ReleaseType.Album };

        Assert.True(recording.IsStudioAlbum);
    }

    [Theory]
    [InlineData(ReleaseType.Album, SecondaryReleaseTypes.Live)]
    [InlineData(ReleaseType.Album, SecondaryReleaseTypes.Compilation)]
    [InlineData(ReleaseType.Album, SecondaryReleaseTypes.Live | SecondaryReleaseTypes.Compilation)]
    [InlineData(ReleaseType.EP, SecondaryReleaseTypes.None)]
    [InlineData(ReleaseType.Single, SecondaryReleaseTypes.None)]
    public void Other_release_shapes_are_not_studio_albums(ReleaseType primary, SecondaryReleaseTypes secondary)
    {
        var recording = new Recording { Title = "Any", PrimaryType = primary, SecondaryTypes = secondary };

        Assert.False(recording.IsStudioAlbum);
    }
}
