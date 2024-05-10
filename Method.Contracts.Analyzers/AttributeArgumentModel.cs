namespace Contracts.Analyzers;

using System.Collections.Generic;

/// <summary>
/// Represents the model of a contract attribute argument.
/// </summary>
/// <param name="Name">The argument name.</param>
/// <param name="Value">The argument value.</param>
internal record AttributeArgumentModel(string Name, string Value);
