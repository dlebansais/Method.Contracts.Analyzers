namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Represents an analysis assertion that checks for an initializer.
/// </summary>
internal class InitializerAnalysisAssertion : IAnalysisAssertion
{
    /// <summary>
    /// Gets the list of initializers if the assertion is true.
    /// </summary>
    public List<IMethodSymbol> InitializerMethodSymbols { get; } = new();

    /// <inheritdoc cref="IAnalysisAssertion.IsTrue(SyntaxNodeAnalysisContext)" />
    public bool IsTrue(SyntaxNodeAnalysisContext context)
    {
        BaseObjectCreationExpressionSyntax ObjectCreationExpression = (BaseObjectCreationExpressionSyntax)context.Node;
        var TypeInfo = context.SemanticModel.GetTypeInfo(ObjectCreationExpression);

        if (TypeInfo.Type is IErrorTypeSymbol)
            return false;

        if (context.SemanticModel.GetOperation(ObjectCreationExpression) is not IObjectCreationOperation ObjectCreationOperation)
            return false;

        IMethodSymbol ConstructorSymbol = Contract.AssertNotNull(ObjectCreationOperation.Constructor);

        ITypeSymbol TypeSymbol = Contract.AssertNotNull(TypeInfo.Type);
        if (TypeSymbol.TypeKind != TypeKind.Class)
            return false;

        return HasInitializeWithAttribute(context, ConstructorSymbol);
    }

    private bool HasInitializeWithAttribute(SyntaxNodeAnalysisContext context, IMethodSymbol constructorSymbol)
    {
        ITypeSymbol ClassSymbol = constructorSymbol.ContainingType;

        if (HasInitializeWithAttribute(context, constructorSymbol.GetAttributes(), ClassSymbol))
            return true;

        if (HasInitializeWithAttribute(context, ClassSymbol.GetAttributes(), ClassSymbol))
            return true;

        return false;
    }

    private bool HasInitializeWithAttribute(SyntaxNodeAnalysisContext context, IEnumerable<AttributeData> attributes, ITypeSymbol classSymbol)
    {
        foreach (AttributeData Attribute in attributes)
        {
            if (Attribute.AttributeClass is IErrorTypeSymbol)
                continue;

            INamedTypeSymbol AttributeClass = Contract.AssertNotNull(Attribute.AttributeClass);

            if (AnalyzerTools.IsExpectedAttribute<InitializeWithAttribute>(context, AttributeClass))
            {
                if (TryGetInitializers(classSymbol, Attribute, out List<IMethodSymbol> Initializers))
                {
                    InitializerMethodSymbols.Clear();
                    InitializerMethodSymbols.AddRange(Initializers);
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetInitializers(ITypeSymbol classSymbol, AttributeData attribute, out List<IMethodSymbol> initializers)
    {
        Contract.RequireNotNull(classSymbol, out ITypeSymbol ClassSymbol);
        Contract.RequireNotNull(attribute, out AttributeData Attribute);

        if (Attribute.ConstructorArguments.Length != 1)
        {
            Contract.Unused(out initializers);
            return false;
        }

        TypedConstant FirstArgument = Attribute.ConstructorArguments.First();
        string ArgumentValue = Contract.AssertNotNull(FirstArgument.Value as string);

        List<IMethodSymbol> InitializerOverloads = new();
        ImmutableArray<ISymbol> Members = ClassSymbol.GetMembers();

        foreach (var Member in Members)
            if (Member is IMethodSymbol MethodSymbol && MethodSymbol.Name == ArgumentValue)
                InitializerOverloads.Add(MethodSymbol);

        if (InitializerOverloads.Count == 0)
        {
            Contract.Unused(out initializers);
            return false;
        }

        initializers = InitializerOverloads;
        return true;
    }
}
