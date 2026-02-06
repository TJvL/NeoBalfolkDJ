using System.Globalization;
using System.Text;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Provides string normalization utilities for consistent comparison of dance names.
/// </summary>
public static class StringNormalizer
{
    /// <summary>
    /// Normalizes a string for comparison by removing accents, special characters,
    /// and converting to lowercase.
    /// </summary>
    public static string NormalizeForComparison(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        
        // Normalize to decomposed form (separates base characters from diacritics)
        var normalized = input.Normalize(NormalizationForm.FormD);
        
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            // Skip non-spacing marks (diacritics/accents)
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                // Keep only letters, digits, and spaces
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }
        }
        
        // Normalize whitespace (collapse multiple spaces, trim)
        return string.Join(" ", sb.ToString().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
    }
    
    /// <summary>
    /// Compares two strings for equality after normalization.
    /// </summary>
    public static bool NormalizedEquals(string a, string b)
    {
        return NormalizeForComparison(a) == NormalizeForComparison(b);
    }
}

