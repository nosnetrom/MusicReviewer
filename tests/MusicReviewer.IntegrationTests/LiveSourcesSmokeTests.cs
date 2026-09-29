using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicReviewer.Application;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Domain.Catalog;
using MusicReviewer.Infrastructure;
using MusicReviewer.Infrastructure.MusicBrainz;

namespace MusicReviewer.IntegrationTests;

/// <summary>
/// Calls the real MusicBrainz, Wikidata and Cover Art Archive through the production HTTP
/// pipeline. Explicit: skipped by default and in CI; run with
/// <c>dotnet test --project tests/MusicReviewer.IntegrationTests -- --explicit only</c>.
/// </summary>
public class LiveSourcesSmokeTests
{
    private static readonly Guid MilesDavis = Guid.Parse("561d854a-6a28-4aa7-8c99-323e6ce46c2a");
    private static readonly Guid KindOfBlue = Guid.Parse("8e8a594f-2175-38c7-a871-abb68ec363e7");

    [Fact(Explicit = true)]
    public async Task Real_sources_return_expected_shapes()
    {
        var ct = TestContext.Current.CancellationToken;
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:MusicReviewer"] = "unused" })
                .Build());
        services.AddApplication().AddInfrastructure();
        await using var provider = services.BuildServiceProvider();

        var gateway = provider.GetRequiredService<MusicBrainzGateway>();
        await gateway.StartAsync(ct);
        try
        {
            var musicBrainz = provider.GetRequiredService<IMusicBrainzClient>();

            var page = await musicBrainz.SearchReleaseGroupsAsync(MilesDavis, ReleaseCategories.Studio, 0, ct);
            Assert.True(page.Total > 40, $"Expected Miles Davis to have 40+ studio albums and EPs, got {page.Total}.");
            Assert.InRange(page.Fetched, 1, 25);

            var candidates = await musicBrainz.GetReleaseCandidatesAsync(KindOfBlue, 1959, ct);
            var original = RepresentativeRelease.Pick(candidates, 1959);
            Assert.Equal("1959-08-17", original?.Date);

            var links = await provider.GetRequiredService<IWikidataClient>().GetReleaseGroupLinksAsync([KindOfBlue], ct);
            Assert.Equal("Kind of Blue", links[KindOfBlue].EnglishWikipediaTitle);

            var cover = await provider.GetRequiredService<ICoverArtClient>().GetFrontCoverUrlAsync(KindOfBlue, ct);
            Assert.NotNull(cover);
        }
        finally
        {
            await gateway.StopAsync(CancellationToken.None);
        }
    }
}
