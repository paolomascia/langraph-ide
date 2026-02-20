using System.Text.RegularExpressions;

namespace LangraphIDE.Helpers
{
    public static class StringHelpers
    {
        public static string ToDisplayName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            // Replace underscores with spaces
            var result = name.Replace("_", " ");

            // Insert space before capital letters (for camelCase/PascalCase)
            result = Regex.Replace(result, "([a-z])([A-Z])", "$1 $2");

            // Capitalize first letter of each word
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result.ToLower());
        }

        public static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            // Insert underscore before capital letters
            var result = Regex.Replace(name, "([a-z])([A-Z])", "$1_$2");
            return result.ToLower().Replace(" ", "_");
        }

        public static string ToPythonIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "unnamed";

            // Replace spaces and dashes with underscores
            var result = name.Replace(" ", "_").Replace("-", "_");

            // Remove non-alphanumeric characters except underscores
            result = Regex.Replace(result, "[^a-zA-Z0-9_]", "");

            // Ensure it doesn't start with a number
            if (char.IsDigit(result[0]))
                result = "_" + result;

            return result.ToLower();
        }
    }
}
