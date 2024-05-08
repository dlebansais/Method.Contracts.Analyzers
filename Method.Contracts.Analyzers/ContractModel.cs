namespace Contracts.Analyzers;

using System.Collections.Generic;

/// <summary>
/// Represents the model of a method contract.
/// </summary>
/// <param name="Namespace">The namespace containing the class that contains the method.</param>
/// <param name="ClassName">The name of the class containing the method..</param>
/// <param name="Documentation">The method documentation, if any.</param>
/// <param name="ShortMethodName">The method name, without the expected suffix.</param>
/// <param name="Attributes">The contract as attributes.</param>
/// <param name="GeneratedMethodDeclaration">The generated method.</param>
internal record ContractModel(string Namespace, string ClassName, string ShortMethodName, string Documentation, List<AttributeModel> Attributes, string GeneratedMethodDeclaration);
