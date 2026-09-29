namespace MusicReviewer.Domain.Catalog;

/// <summary>
/// Ranks an artist's recordings. Signals, strongest first:
/// an English Wikipedia article, broad coverage across Wikipedias (Wikidata sitelinks),
/// and being a full studio album rather than an EP, live album or compilation.
/// </summary>
public static class Notability
{
    public const double EnglishArticleBonus = 20;
    public const double SitelinkWeight = 15;

    public static double Score(Recording recording)
    {
        var score = recording.Category switch
        {
            ReleaseCategories.Studio => 10.0,
            ReleaseCategories.Live => 4.0,
            ReleaseCategories.Compilation => 1.0,
            _ => 2.0,
        };

        if (recording.PrimaryType == ReleaseType.EP)
            score -= 4;

        if (recording.WikipediaTitle is not null)
            score += EnglishArticleBonus;

        // Log scale: the difference between 1 and 10 Wikipedias matters more than 40 vs 50.
        score += SitelinkWeight * Math.Log10(1 + (recording.WikidataSitelinks ?? 0));

        return Math.Round(score, 3);
    }
}
