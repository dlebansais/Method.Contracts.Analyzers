namespace Contracts.Analyzers.Helper;

/// <summary>
/// Represents the model of a method contract.
/// </summary>
/// <param name="EditorConfigKey">The key in the .editorconfig file.</param>
/// <param name="DefaultValue">The default value.</param>
internal record GeneratorSettingsEntry(string EditorConfigKey, string DefaultValue);
