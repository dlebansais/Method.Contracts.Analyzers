namespace Contracts.Analyzers;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Contracts.Analyzers.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

    private static (string BeforeNamespaceDeclaration, string AfterNamespaceDeclaration) GetUsings(GeneratorAttributeSyntaxContext context, bool isAsync)
    {
        string RawBeforeNamespaceDeclaration = string.Empty;
        string RawAfterNamespaceDeclaration = string.Empty;

        SyntaxNode TargetNode = context.TargetNode;
        if (TargetNode.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>() is BaseNamespaceDeclarationSyntax BaseNamespaceDeclaration)
        {
            RawAfterNamespaceDeclaration = BaseNamespaceDeclaration.Usings.ToFullString();

            if (BaseNamespaceDeclaration.Parent is CompilationUnitSyntax CompilationUnit)
                RawBeforeNamespaceDeclaration = CompilationUnit.Usings.ToFullString();
        }

        return FixUsings(RawBeforeNamespaceDeclaration, RawAfterNamespaceDeclaration, isAsync);
    }

    private static (string BeforeNamespaceDeclaration, string AfterNamespaceDeclaration) FixUsings(string rawBeforeNamespaceDeclaration, string rawAfterNamespaceDeclaration, bool isAsync)
    {
        string BeforeNamespaceDeclaration = GeneratorHelper.SortUsings(rawBeforeNamespaceDeclaration);
        string AfterNamespaceDeclaration = GeneratorHelper.SortUsings(rawAfterNamespaceDeclaration);

        (BeforeNamespaceDeclaration, AfterNamespaceDeclaration) = AddMissingUsing(BeforeNamespaceDeclaration, AfterNamespaceDeclaration, "using Contracts;");
        (BeforeNamespaceDeclaration, AfterNamespaceDeclaration) = AddMissingUsing(BeforeNamespaceDeclaration, AfterNamespaceDeclaration, "using System.CodeDom.Compiler;");

        if (isAsync)
            (BeforeNamespaceDeclaration, AfterNamespaceDeclaration) = AddMissingUsing(BeforeNamespaceDeclaration, AfterNamespaceDeclaration, "using System.Threading.Tasks;");

        AfterNamespaceDeclaration = GeneratorHelper.SortUsings(AfterNamespaceDeclaration);

        return (BeforeNamespaceDeclaration, AfterNamespaceDeclaration);
    }

    private static (string BeforeNamespaceDeclaration, string AfterNamespaceDeclaration) AddMissingUsing(string beforeNamespaceDeclaration, string afterNamespaceDeclaration, string usingDirective)
    {
        if (!beforeNamespaceDeclaration.Contains(usingDirective) && !afterNamespaceDeclaration.Contains(usingDirective))
            afterNamespaceDeclaration += $"{usingDirective}\n";

        return (beforeNamespaceDeclaration, afterNamespaceDeclaration);
    }

    private static bool IsValidAttributeArgument(string attributeName, bool namelessArgumentIsFirstAndOnly, AttributeArgumentSyntax attributeArgument, MethodDeclarationSyntax methodDeclaration, out string argumentName, out string argumentValue)
    {
        if (!IsValidAttributeArgumentNameAndValue(attributeArgument, out argumentName, out argumentValue))
            return false;

        if (attributeName == nameof(RequireNotNullAttribute))
        {
            if (argumentName == string.Empty)
            {
                if (!GetParameterType(argumentValue, methodDeclaration, out _))
                    return false;
            }
            else if (argumentName is not nameof(RequireNotNullAttribute.AliasType) and not nameof(RequireNotNullAttribute.AliasName))
                return false;
        }

        return true;
    }

    private static List<int> GetNamelessArgumentPositions(AttributeArgumentListSyntax attributeArgumentList)
    {
        List<int> Result = new();
        int Index = 0;

        foreach (var AttributeArgument in attributeArgumentList.Arguments)
        {
            if (AttributeArgument.NameEquals is null)
                Result.Add(Index);

            Index++;
        }

        return Result;
    }

    private static bool IsValidAttributeArgumentNameAndValue(AttributeArgumentSyntax attributeArgument, out string argumentName, out string argumentValue)
    {
        if (attributeArgument.NameEquals is NameEqualsSyntax NameEquals)
            argumentName = NameEquals.Name.Identifier.Text;
        else
            argumentName = string.Empty;

        if (IsStringOrNameofAttributeArgument(attributeArgument, out argumentValue))
            return true;

        argumentValue = string.Empty;
        return false;
    }

    private static void OutputContractMethod(SourceProductionContext context, (GeneratorSettings Settings, ImmutableArray<ContractModel> Models) modelAndSettings)
    {
        string DisableWarnings = GeneratorHelper.AddPrefixAndSuffixIfNotEmpty(Settings.DisabledWarnings, "#pragma warning disable ", "\n");

        foreach (ContractModel Model in modelAndSettings.Models)
        {
            string SourceText = $$"""
                #nullable enable
                {{DisableWarnings}}{{Model.UsingsBeforeNamespace}}
                namespace {{Model.Namespace}};
                {{Model.UsingsAfterNamespace}}
                partial class {{Model.ClassName}}
                {
                {{Model.Documentation}}{{Model.GeneratedMethodDeclaration}}
                }
                """;
            SourceText = SourceText.Replace("\r\n", "\n");

            context.AddSource($"{Model.ClassName}_{Model.ShortMethodName}{Model.UniqueOverloadIdentifier}.g.cs", Microsoft.CodeAnalysis.Text.SourceText.From(SourceText, Encoding.UTF8));
        }
    }
}
