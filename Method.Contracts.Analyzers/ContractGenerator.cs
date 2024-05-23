namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.Analyzers.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Represents a code generator.
/// </summary>
[Generator]
public partial class ContractGenerator : IIncrementalGenerator
{
    // List of supported attributes by their name.
    private static readonly List<string> SupportedAttributeNames = new()
    {
        nameof(AccessAttribute),
        nameof(RequireNotNullAttribute),
        nameof(RequireAttribute),
        nameof(EnsureAttribute),
    };

    /// <summary>
    /// The namespace of the Method.Contracts assemblies.
    /// </summary>
    public const string ContractsNamespace = "Contracts";

    /// <summary>
    /// The class name of Method.Contracts methods.
    /// </summary>
    public const string ContractClassName = "Contract";

    /// <inheritdoc cref="IIncrementalGenerator.Initialize"/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var Settings = context.AnalyzerConfigOptionsProvider.SelectMany(ReadSettings);

        InitializePipeline<AccessAttribute>(context, Settings);
        InitializePipeline<RequireNotNullAttribute>(context, Settings);
        InitializePipeline<RequireAttribute>(context, Settings);
        InitializePipeline<EnsureAttribute>(context, Settings);
    }

    private static void InitializePipeline<T>(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<GeneratorSettings> settings)
        where T : Attribute
    {
        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: GetFullyQualifiedMetadataName<T>(),
            predicate: KeepNodeForPipeline<T>,
            transform: TransformContractAttributes);

        context.RegisterSourceOutput(settings.Combine(pipeline.Collect()), OutputContractMethod);
    }

    private static string GetFullyQualifiedMetadataName<T>()
    {
        return $"{ContractsNamespace}.{typeof(T).Name}";
    }

    private static bool GetParameterType(string argumentName, MethodDeclarationSyntax methodDeclaration, out TypeSyntax parameterType)
    {
        TypeSyntax? ResultType = null;
        ParameterListSyntax ParameterList = methodDeclaration.ParameterList;

        foreach (ParameterSyntax Parameter in ParameterList.Parameters)
        {
            string ParameterName = Parameter.Identifier.Text;

            if (ParameterName == argumentName)
            {
                ResultType = Parameter.Type;
                break;
            }
        }

        if (ResultType is not null)
        {
            parameterType = ResultType.WithoutLeadingTrivia().WithoutTrailingTrivia();
            return true;
        }

        parameterType = null!;
        return false;
    }
}
