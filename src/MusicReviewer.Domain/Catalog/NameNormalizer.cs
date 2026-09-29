using System.Globalization;
using System.Text;

namespace MusicReviewer.Domain.Catalog;

/// <summary>
/// Produces a search key for names: lowercase, accents removed, punctuation dropped,
/// whitespace collapsed. "Björk" and "BJORK" both become "bjork"; "AC/DC" becomes "ac dc".
/// </summary>
public static class NameNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingSpace = false;

        foreach (var c in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
            {
                if (pendingSpace && builder.Length > 0)
                    builder.Append(' ');
                pendingSpace = false;
                builder.Append(char.ToLowerInvariant(c));
            }
            else if (c is '\'' or '’')
            {
                // "Guns N' Roses" → "guns n roses"; apostrophes join rather than split words.
            }
            else
            {
                pendingSpace = true;
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
