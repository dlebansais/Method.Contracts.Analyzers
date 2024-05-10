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

    private static bool KeepNodeForPipeline<T>(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        where T : Attribute
    {
        // Only accept methods with the 'Verified' suffix in their name.
        if (syntaxNode is not MethodDeclarationSyntax MethodDeclaration)
            return false;

        string MethodName = MethodDeclaration.Identifier.ToString();
        string VerifiedSuffix = Settings.VerifiedSuffix;

        // The suffix can't be empty: if invalid in user settings, it's the default suffix.
        Debug.Assert(VerifiedSuffix != string.Empty);

        if (!MethodName.EndsWith(VerifiedSuffix, StringComparison.Ordinal) || MethodName.Length == VerifiedSuffix.Length)
            return false;

        // Get a list of all supported attributes for this method.
        List<string> AttributeNames = new();
        bool IsInvalidAttributeFound = false;

        for (int IndexList = 0; IndexList < MethodDeclaration.AttributeLists.Count; IndexList++)
        {
            AttributeListSyntax AttributeList = MethodDeclaration.AttributeLists[IndexList];

            for (int Index = 0; Index < AttributeList.Attributes.Count; Index++)
            {
                AttributeSyntax Attribute = AttributeList.Attributes[Index];

                string AttributeName = ToAttributeName(Attribute);
                if (SupportedAttributeNames.Contains(AttributeName) && Attribute.ArgumentList is AttributeArgumentListSyntax AttributeArgumentList)
                {
                    bool HasAtLeastOneArgument = AttributeArgumentList.Arguments.Any();
                    bool AreAllArgumentsValid = AttributeArgumentList.Arguments.All(IsValidAttributeArgument);

                    if (HasAtLeastOneArgument && AreAllArgumentsValid)
                        AttributeNames.Add(AttributeName);
                    else
                        IsInvalidAttributeFound = true;
                }
            }
        }

        // One of these attributes has to be the first, and we only return true for this one.
        // This way, multiple calls with different T return true exactly once.
        if (IsInvalidAttributeFound || AttributeNames.Count == 0 || AttributeNames[0] != typeof(T).Name)
            return false;

        return true;
    }

    private static bool IsValidAttributeArgument(AttributeArgumentSyntax attributeArgument)
    {
        if (attributeArgument.Expression is InvocationExpressionSyntax InvocationExpression &&
            InvocationExpression.Expression is IdentifierNameSyntax IdentifierName &&
            IdentifierName.Identifier.Text == "nameof" &&
            InvocationExpression.ArgumentList.Arguments.Count == 1 &&
            InvocationExpression.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax)
            return true;

        if (attributeArgument.Expression is LiteralExpressionSyntax LiteralExpression &&
            LiteralExpression.Kind() == SyntaxKind.StringLiteralExpression)
        {
            string ArgumentText = LiteralExpression.Token.Text;
            ArgumentText = ArgumentText.Trim('"');
            if (ArgumentText != string.Empty)
                return true;
        }

        return false;
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

        for (int IndexList = 0; IndexList < MethodDeclaration.AttributeLists.Count; IndexList++)
        {
            AttributeListSyntax AttributeList = MethodDeclaration.AttributeLists[IndexList];

            for (int Index = 0; Index < AttributeList.Attributes.Count; Index++)
            {
                AttributeSyntax Attribute = AttributeList.Attributes[Index];

                string AttributeName = ToAttributeName(Attribute);
                if (SupportedAttributeNames.Contains(AttributeName) && Attribute.ArgumentList is AttributeArgumentListSyntax AttributeArgumentList)
                {
                    List<string> Arguments = new();

                    for (int IndexArgument = 0; IndexArgument < AttributeArgumentList.Arguments.Count; IndexArgument++)
                    {
                        AttributeArgumentSyntax AttributeArgument = AttributeArgumentList.Arguments[IndexArgument];
                        Debug.Assert(IsValidAttributeArgument(AttributeArgument), $"Attribute argument '{AttributeArgument}' is expected to be valid.");

                        string ArgumentText = string.Empty;

                        if (AttributeArgument.Expression is InvocationExpressionSyntax InvocationExpression)
                        {
                            Debug.Assert(InvocationExpression.Expression is IdentifierNameSyntax);
                            IdentifierNameSyntax IdentifierName = (IdentifierNameSyntax)InvocationExpression.Expression;

                            Debug.Assert(IdentifierName.Identifier.Text == "nameof", $"Expected nameof but got: '{IdentifierName.Identifier.Text}'.");
                            Debug.Assert(InvocationExpression.ArgumentList.Arguments.Count == 1, $"Expected one name but got {InvocationExpression.ArgumentList.Arguments.Count}.");

                            ExpressionSyntax FirstArgumentExpression = InvocationExpression.ArgumentList.Arguments[0].Expression;
                            Debug.Assert(FirstArgumentExpression is IdentifierNameSyntax, $"Expected a name but got: '{FirstArgumentExpression}'.");

                            IdentifierNameSyntax ArgumentIdentifierName = (IdentifierNameSyntax)FirstArgumentExpression;
                            ArgumentText = ArgumentIdentifierName.Identifier.Text;
                        }

                        if (AttributeArgument.Expression is LiteralExpressionSyntax LiteralExpression)
                        {
                            Debug.Assert(LiteralExpression.Kind() == SyntaxKind.StringLiteralExpression, $"Expected a literal string but got: '{LiteralExpression.Kind()}'.");
                            ArgumentText = LiteralExpression.Token.Text;
                            ArgumentText = ArgumentText.Trim('"');
                        }

                        Debug.Assert(ArgumentText != string.Empty, $"Argument name found empty.");

                        Arguments.Add(ArgumentText);
                    }

                    AttributeModel Model = new(AttributeName, Arguments);

                    Result.Add(Model);
                }
            }
        }

        return Result;
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
