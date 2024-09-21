namespace Contracts.Analyzers;

using System.Collections.Generic;

/// <summary>
/// Represents the model of a contract attribute.
/// </summary>
/// <param name="Name">The attribute name.</param>
/// <param name="Arguments">The attribute arguments.</param>
internal record AttributeModel(string Name, List<AttributeArgumentModel> Arguments);
