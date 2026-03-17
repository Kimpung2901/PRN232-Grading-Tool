using System.Text.RegularExpressions;

namespace Api_RestAPI_gradingTool.Validation;

public static class NameRules
{
    private static readonly Regex AllowedNameRegex = new(@"^[\p{L}\p{N} _-]+$", RegexOptions.Compiled);

    public static string? Validate(string? name, int minLength, int maxLength, string fieldName = "Name")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return $"{fieldName} is required.";
        }

        var trimmed = name.Trim();
        if (trimmed.Length < minLength || trimmed.Length > maxLength)
        {
            return $"{fieldName} length must be between {minLength} and {maxLength}.";
        }

        if (!AllowedNameRegex.IsMatch(trimmed))
        {
            return $"{fieldName} contains invalid characters. Only letters, numbers, spaces, '_' and '-' are allowed.";
        }

        return null;
    }
}
