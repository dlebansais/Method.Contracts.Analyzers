namespace Contracts.Analyzers.Helper;

using System.Globalization;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents the model of a method contract.
/// </summary>
/// <param name="EditorConfigKey">The key in the .editorconfig file.</param>
/// <param name="DefaultValue">The default value.</param>
internal record GeneratorSettingsEntry(string EditorConfigKey, string DefaultValue)
{
    /// <summary>
    /// Reads the setting as a string, always returning a valid value, using the default value if necessary.
    /// </summary>
    /// <param name="options">The settings to read.</param>
    /// <param name="isDefault"><see langword="true"/> if the default value is returned; otherwise, <see langword="false"/>.</param>
    /// <returns>The current setting value as a string if valid, the default value otherwise.</returns>
    public string ReadAsString(AnalyzerConfigOptionsProvider options, out bool isDefault)
    {
        _ = options.GlobalOptions.TryGetValue(EditorConfigKey, out string? Value);
        return StringValueOrDefault(Value, out isDefault);
    }

    /// <summary>
    /// Returns <paramref name="value"/> as a string if valid, the default value otherwise.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="isDefault"><see langword="true"/> if the default value is returned; otherwise, <see langword="false"/>.</param>
    /// <returns><paramref name="value"/> as a string if valid, the default value otherwise.</returns>
    public string StringValueOrDefault(string? value, out bool isDefault)
    {
        if (value is not null && value != string.Empty)
        {
            isDefault = false;
            return value;
        }

        isDefault = true;
        return DefaultValue;
    }

    /// <summary>
    /// Reads the setting as an int, always returning a valid value, using the default value if necessary.
    /// </summary>
    /// <param name="options">The settings to read.</param>
    /// <param name="isDefault"><see langword="true"/> if the default value is returned; otherwise, <see langword="false"/>.</param>
    /// <returns>The current setting value as an int if valid, the default value otherwise.</returns>
    public int ReadAsInt(AnalyzerConfigOptionsProvider options, out bool isDefault)
    {
        _ = options.GlobalOptions.TryGetValue(EditorConfigKey, out string? Value);
        return IntValueOrDefault(Value, out isDefault);
    }

    /// <summary>
    /// Returns <paramref name="value"/> as an int if valid, the default value otherwise.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="isDefault"><see langword="true"/> if the default value is returned; otherwise, <see langword="false"/>.</param>
    /// <returns><paramref name="value"/> as an int if valid, the default value otherwise.</returns>
    public int IntValueOrDefault(string? value, out bool isDefault)
    {
        if (value is not null && int.TryParse(value, out int IntValue))
        {
            isDefault = false;
            return IntValue;
        }

        isDefault = true;
        return int.Parse(DefaultValue, CultureInfo.InvariantCulture);
    }
}
