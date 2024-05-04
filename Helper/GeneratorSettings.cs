namespace Contracts.Analyzers.Helper;

/// <summary>
/// Represents the settings of the code generator.
/// </summary>
/// <param name="VerifiedSuffix">The suffix of verified methods.</param>
/// <param name="TabLength">The tab length in generated code.</param>
/// <param name="ResultIdentifier">The name of the result variable for queries.</param>
internal record GeneratorSettings(string VerifiedSuffix, int TabLength, string ResultIdentifier);
