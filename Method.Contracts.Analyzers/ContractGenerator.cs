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

    private static ContractModel TransformContractAttributes(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        ContractModel Model = GetModelWithoutContract(context);
        Model = Model with { Documentation = GetMethodDocumentation(context) };
        Model = Model with { Attributes = GetModelContract(context) };
        Model = Model with { GeneratedMethodDeclaration = GetGeneratedMethodDeclaration(Model, context, out bool IsAsync) };
        (string UsingsBeforeNamespace, string UsingsAfterNamespace) = GetUsings(context, IsAsync);
        Model = Model with { UsingsBeforeNamespace = UsingsBeforeNamespace, UsingsAfterNamespace = UsingsAfterNamespace, IsAsync = IsAsync };

        return Model;
    }

    private static ContractModel GetModelWithoutContract(GeneratorAttributeSyntaxContext context)
    {
        var containingClass = context.TargetSymbol.ContainingType;

        // Note: this is a simplified example. You will also need to handle the case where the type is in a global namespace, nested, etc.
        string Namespace = containingClass.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))!;
        string ClassName = containingClass.Name;
        string SymbolName = context.TargetSymbol.Name;

        string VerifiedSuffix = Settings.VerifiedSuffix;
        Debug.Assert(SymbolName.EndsWith(VerifiedSuffix, StringComparison.Ordinal));
        Debug.Assert(SymbolName.Length > VerifiedSuffix.Length);
        string ShortMethodName = SymbolName.Substring(0, SymbolName.Length - VerifiedSuffix.Length);

        return new ContractModel(
            Namespace: Namespace,
            UsingsBeforeNamespace: string.Empty,
            UsingsAfterNamespace: string.Empty,
            ClassName: ClassName,
            ShortMethodName: ShortMethodName,
            UniqueOverloadIdentifier: GetUniqueOverloadIdentifier(context),
            Documentation: string.Empty,
            Attributes: new List<AttributeModel>(),
            GeneratedMethodDeclaration: string.Empty,
            IsAsync: false);
    }

    private static string GetUniqueOverloadIdentifier(GeneratorAttributeSyntaxContext context)
    {
        SyntaxNode TargetNode = context.TargetNode;

        Debug.Assert(TargetNode is MethodDeclarationSyntax, $"Expected MethodDeclarationSyntax, but got instead: '{TargetNode}'.");
        MethodDeclarationSyntax MethodDeclaration = (MethodDeclarationSyntax)TargetNode;

        ParameterListSyntax ParameterList = MethodDeclaration.ParameterList;
        string Result = string.Empty;

        foreach (var CallParameter in ParameterList.Parameters)
            if (CallParameter is ParameterSyntax Parameter)
            {
                if (Parameter.Type is TypeSyntax Type)
                {
                    string TypeAsString = Type.ToString();
                    uint HashCode = unchecked((uint)GeneratorHelper.GetStableHashCode(TypeAsString));
                    Result += $"_{HashCode}";
                }
            }

        return Result;
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

    private static string GetMethodDocumentation(GeneratorAttributeSyntaxContext context)
    {
        SyntaxNode TargetNode = context.TargetNode;

        Debug.Assert(TargetNode is MethodDeclarationSyntax, $"Expected MethodDeclarationSyntax, but got instead: '{TargetNode}'.");
        MethodDeclarationSyntax MethodDeclaration = (MethodDeclarationSyntax)TargetNode;

        string Documentation = string.Empty;

        if (MethodDeclaration.HasLeadingTrivia)
        {
            var LeadingTrivia = MethodDeclaration.GetLeadingTrivia();

            foreach (var Trivia in LeadingTrivia)
                if (Trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    Documentation = LeadingTrivia.ToString().Trim('\r').Trim('\n').TrimEnd(' ');
                    break;
                }
        }

        return Documentation;
    }

    private static List<AttributeModel> GetModelContract(GeneratorAttributeSyntaxContext context)
    {
        SyntaxNode TargetNode = context.TargetNode;

        Debug.Assert(TargetNode is MethodDeclarationSyntax, $"Expected MethodDeclarationSyntax, but got instead: '{TargetNode}'.");
        MethodDeclarationSyntax MethodDeclaration = (MethodDeclarationSyntax)TargetNode;

        List<AttributeModel> Result = new();
        List<AttributeSyntax> MethodAttributes = GeneratorHelper.GetMethodSupportedAttributes(MethodDeclaration, SupportedAttributeNames);

        foreach (AttributeSyntax Attribute in MethodAttributes)
        {
            string AttributeName = GeneratorHelper.ToAttributeName(Attribute);
            if (SupportedAttributeNames.Contains(AttributeName) && Attribute.ArgumentList is AttributeArgumentListSyntax AttributeArgumentList)
            {
                List<AttributeArgumentModel> Arguments = new();
                List<int> NamelessArgumentPositions = GetNamelessArgumentPositions(AttributeArgumentList);
                bool NamelessArgumentIsFirstAndOnly = NamelessArgumentPositions.Count == 1 && NamelessArgumentPositions[0] == 0;

                for (int IndexArgument = 0; IndexArgument < AttributeArgumentList.Arguments.Count; IndexArgument++)
                {
                    AttributeArgumentSyntax AttributeArgument = AttributeArgumentList.Arguments[IndexArgument];
                    bool IsValid = IsValidAttributeArgument(AttributeName, NamelessArgumentIsFirstAndOnly, AttributeArgument, MethodDeclaration, out string ArgumentName, out string ArgumentValue);
                    Debug.Assert(IsValid, $"Attribute argument '{AttributeArgument}' is expected to be valid.");
                    Debug.Assert(ArgumentValue != string.Empty, $"Argument value found empty.");

                    Arguments.Add(new AttributeArgumentModel(Name: ArgumentName, Value: ArgumentValue));
                }

                AttributeModel Model = new(AttributeName, Arguments);

                Result.Add(Model);
            }
        }

        return Result;
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
