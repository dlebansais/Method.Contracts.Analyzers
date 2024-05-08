namespace Contracts.Analyzers.Helper;

/// <summary>
/// Helper class for the code generator.
/// </summary>
internal static class SettingHelper
{
    /// <summary>
    /// Returns <paramref name="text"/> prefixed with <paramref name="prefix"/> and followed with <paramref name="suffix"/> if not empty; otherwise just return <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The string to compare to the empty string.</param>
    /// <param name="prefix">The prefix.</param>
    /// <param name="suffix">The suffix.</param>
    public static string AddPrefixAndSuffixIfNotEmpty(string text, string prefix, string suffix)
    {
        return text == string.Empty ? string.Empty : $"{prefix}{text}{suffix}";
    }
}