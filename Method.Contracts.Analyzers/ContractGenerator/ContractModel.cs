namespace Contracts.Analyzers;

using System.Collections.Generic;

/// <summary>
/// Represents the model of a method or property contract.
/// </summary>
/// <param name="Namespace">The namespace containing the class that contains the method or property.</param>
/// <param name="UsingsBeforeNamespace">Using directives before the namespace declaration.</param>
/// <param name="UsingsAfterNamespace">Using directives after the namespace declaration.</param>
/// <param name="ClassName">The name of the class containing the method or property.</param>
/// <param name="DeclarationTokens">The token(s) to use for declaration (either 'class', 'struct', 'record' or 'record struct').</param>
/// <param name="FullClassName">The name of the class with type parameter and constraints.</param>
/// <param name="ShortName">The method or property name, without the expected suffix.</param>
/// <param name="UniqueOverloadIdentifier">The unique identifier used to identify each overload of a multiply generated method (if the contract applies to a property this identifier is always the same).</param>
/// <param name="Documentation">The method or property documentation, if any.</param>
/// <param name="Attributes">The contract as attributes.</param>
/// <param name="GeneratedMethodDeclaration">The generated method (if the contract applies to a method, otherwise <see cref="string.Empty"/>).</param>
/// <param name="GeneratedPropertyDeclaration">The generated property (if the contract applies to a property, otherwise <see cref="string.Empty"/>).</param>
/// <param name="IsAsync">Whether the generated method is asynchronous (if the contract applies to a method).</param>
internal record ContractModel(string Namespace,
                              string UsingsBeforeNamespace,
                              string UsingsAfterNamespace,
                              string ClassName,
                              string DeclarationTokens,
                              string FullClassName,
                              string ShortName,
                              string UniqueOverloadIdentifier,
                              string Documentation,
                              List<AttributeModel> Attributes,
                              string GeneratedMethodDeclaration,
                              string GeneratedPropertyDeclaration,
                              bool IsAsync);
