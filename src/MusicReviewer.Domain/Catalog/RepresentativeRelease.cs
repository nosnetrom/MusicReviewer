namespace MusicReviewer.Domain.Catalog;

/// <summary>One edition of a recording, as considered when choosing which to display.</summary>
public sealed record ReleaseCandidate(Guid Id, string? Date, string? Country, IReadOnlyList<string> Formats, int TrackCount);

/// <summary>
/// Picks the edition whose track list and credits represent a recording: the original release.
/// Preference order: released in the recording's first year, fully dated, earliest, from a
/// major market, on a mainstream physical format.
/// </summary>
public static class RepresentativeRelease
{
    private static readonly string[] PreferredCountries = ["US", "GB", "XW", "XE"];
    private static readonly string[] PreferredFormatPrefixes = ["12\" Vinyl", "Vinyl", "CD", "Digital Media"];

    public static ReleaseCandidate? Pick(IEnumerable<ReleaseCandidate> candidates, int? firstReleaseYear)
    {
        return candidates
            .Where(c => c.TrackCount > 0)
            .OrderByDescending(c => firstReleaseYear is not null && PartialDate.Year(c.Date) == firstReleaseYear)
            .ThenByDescending(c => PartialDate.Precision(c.Date) == 3)
            .ThenBy(c => c.Date ?? "9999")
            .ThenBy(c => Rank(PreferredCountries, c.Country is null ? [] : [c.Country], exact: true))
            .ThenBy(c => Rank(PreferredFormatPrefixes, c.Formats, exact: false))
            .ThenBy(c => c.Id)
            .FirstOrDefault();
    }

    private static int Rank(string[] preferred, IReadOnlyList<string> values, bool exact)
    {
        for (var i = 0; i < preferred.Length; i++)
        {
            if (values.Any(v => exact
                    ? string.Equals(v, preferred[i], StringComparison.OrdinalIgnoreCase)
                    : v.StartsWith(preferred[i], StringComparison.OrdinalIgnoreCase)))
                return i;
        }

        return preferred.Length;
    }
}
