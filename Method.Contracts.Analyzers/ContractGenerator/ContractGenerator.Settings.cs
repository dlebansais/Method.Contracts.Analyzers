namespace Contracts.Analyzers;

using System.Collections.Generic;
using System.Threading;
using Contracts.Analyzers.Helper;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents a code generator.
/// </summary>
public partial class ContractGenerator
{
    /// <summary>
    /// The key in .csproj for the suffix that a method must have for code to be generated.
    /// </summary>
    public const string VerifiedSuffixKey = "VerifiedSuffix";

    /// <summary>
    /// The default value for the suffix that a method must have for code to be generated.
    /// </summary>
    public const string DefaultVerifiedSuffix = "Verified";

    /// <summary>
    /// The key in .csproj for the tab length in generated code.
    /// </summary>
    public const string TabLengthKey = "TabLength";

    /// <summary>
    /// The default value for the tab length in generated code.
    /// </summary>
    public const int DefaultTabLength = 4;

    /// <summary>
    /// The key in .csproj for the name of the result identifier in generated queries.
    /// </summary>
    public const string ResultIdentifierKey = "ResultIdentifier";

    /// <summary>
    /// The default value for the name of the result identifier in generated queries.
    /// </summary>
    public const string DefaultResultIdentifier = "Result";

    /// <summary>
    /// The key in .csproj for the name of the value identifier in generated queries.
    /// </summary>
    public const string ValueIdentifierKey = "ValueIdentifier";

    /// <summary>
    /// The default value for the name of the value identifier in generated queries.
    /// </summary>
    public const string DefaultValueIdentifier = "Value";

    /// <summary>
    /// The key in .csproj for the comma-separated list of disabled warnings in generated code.
    /// </summary>
    public const string DisabledWarningsKey = "DisabledWarnings";

    /// <summary>
    /// The default value for the name of the value identifier in generated queries.
    /// </summary>
    public const string DefaultDisabledWarnings = "";

    // The settings values.
    private static readonly GeneratorSettingsEntry VerifiedSuffixSetting = new(BuildKey: VerifiedSuffixKey, DefaultValue: DefaultVerifiedSuffix);
    private static readonly GeneratorSettingsEntry TabLengthSetting = new(BuildKey: TabLengthKey, DefaultValue: $"{DefaultTabLength}");
    private static readonly GeneratorSettingsEntry ResultIdentifierSetting = new(BuildKey: ResultIdentifierKey, DefaultValue: DefaultResultIdentifier);
    private static readonly GeneratorSettingsEntry ValueIdentifierSetting = new(BuildKey: ValueIdentifierKey, DefaultValue: DefaultValueIdentifier);
    private static readonly GeneratorSettingsEntry DisabledWarningsSetting = new(BuildKey: DisabledWarningsKey, DefaultValue: DefaultDisabledWarnings);
    private static GeneratorSettings Settings = new(VerifiedSuffix: DefaultVerifiedSuffix, TabLength: DefaultTabLength, ResultIdentifier: DefaultResultIdentifier, ValueIdentifier: DefaultValueIdentifier, DisabledWarnings: DefaultDisabledWarnings);

    /// <summary>
    /// Reads settings.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    internal static IEnumerable<GeneratorSettings> ReadSettings(AnalyzerConfigOptionsProvider options, CancellationToken cancellationToken)
    {
        string VerifiedSuffix = VerifiedSuffixSetting.ReadAsString(options, out _);
        int TabLength = TabLengthSetting.ReadAsInt(options, out _);
        string ResultIdentifier = ResultIdentifierSetting.ReadAsString(options, out _);
        string ValueIdentifier = ValueIdentifierSetting.ReadAsString(options, out _);
        string DisabledWarnings = DisabledWarningsSetting.ReadAsString(options, out _);

        Settings = Settings with
        {
            VerifiedSuffix = VerifiedSuffix,
            TabLength = TabLength,
            ResultIdentifier = ResultIdentifier,
            ValueIdentifier = ValueIdentifier,
            DisabledWarnings = DisabledWarnings,
        };

        return [Settings];
    }
}
