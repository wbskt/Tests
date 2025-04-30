using System.Text.RegularExpressions;

namespace Wbskt.Bdd.Tests;

public static partial class PlaceholderReplacer
{
    public static string ReplacePlaceholders(string json)
    {
        // Dictionary to store the generated values for each placeholder
        var placeholderMap = new Dictionary<string, string>();

        // Regex pattern to match placeholders like {TestUser0.guid}
        var regex = MyRegex();

        // Replace each match
        var result = regex.Replace(json, match =>
        {
            var placeholder = match.Groups[1].Value;

            // If already generated, return the stored value
            if (placeholderMap.TryGetValue(placeholder, out var existingValue))
            {
                return existingValue;
            }

            // Otherwise, generate new GUID, store and return
            var newValue = placeholder.Contains("GUID") ? Guid.NewGuid().ToString() : $"{placeholder}.{Guid.NewGuid()}";
            placeholderMap[placeholder] = newValue;
            return newValue;
        });

        return result;
    }

    [GeneratedRegex(@"\{([^\{\}]+?)\}")]
    private static partial Regex MyRegex();
}
