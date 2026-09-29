using System.Globalization;

namespace MusicReviewer.Domain.Catalog;

/// <summary>Helpers for MusicBrainz partial dates ("1959", "1959-08", "1959-08-17").</summary>
public static class PartialDate
{
    public static int? Year(string? value) =>
        value is { Length: >= 4 } && int.TryParse(value.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            ? year
            : null;

    /// <summary>0 = none, 1 = year, 2 = year-month, 3 = full date.</summary>
    public static int Precision(string? value) => value?.Length switch
    {
        >= 10 => 3,
        >= 7 => 2,
        >= 4 => 1,
        _ => 0,
    };
}
