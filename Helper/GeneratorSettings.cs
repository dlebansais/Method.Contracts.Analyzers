namespace Contracts.Analyzers.Helper;

/// <summary>
/// Represents the settings of the code generator.
/// </summary>
/// <param name="VerifiedSuffix">The suffix of verified methods.</param>
/// <param name="TabLength">The tab length in generated code.</param>
/// <param name="ResultIdentifier">The name of the result variable for queries.</param>
/// <param name="ValueIdentifier">The name of the non-null value variable for properties.</param>
/// <param name="DisabledWarnings">The coma-separated list of disabled warnings in generated code.</param>
internal record GeneratorSettings(string VerifiedSuffix, int TabLength, string ResultIdentifier, string ValueIdentifier, string DisabledWarnings);
