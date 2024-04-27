namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Represents a code generator.
/// </summary>
[Generator]
public class ContractGenerator : IIncrementalGenerator
{
    private static readonly List<string> SupportedAttributeNames = new()
    {
        nameof(AccessAttribute),
        nameof(RequireNotNullAttribute),
        nameof(RequireAttribute),
        nameof(EnsureAttribute),
    };

    private const string VerifiedSuffix = "Verified";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        const string Namespace = "Contracts";

        RegisterAccessAttribute(context, Namespace);
        RegisterRequireNotNullAttribute(context, Namespace);
        RegisterRequireAttribute(context, Namespace);
        RegisterEnsureAttribute(context, Namespace);

        var pipelineAccess = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: $"{Namespace}.{nameof(AccessAttribute)}",
            predicate: KeepNodeForPipeline<AccessAttribute>,
            transform: TransformContractAttributes);

        var pipelineRequireNotNull = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: $"{Namespace}.{nameof(RequireNotNullAttribute)}",
            predicate: KeepNodeForPipeline<RequireNotNullAttribute>,
            transform: TransformContractAttributes);

        var pipelineRequire = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: $"{Namespace}.{nameof(RequireAttribute)}",
            predicate: KeepNodeForPipeline<RequireAttribute>,
            transform: TransformContractAttributes);

        var pipelineEnsure = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: $"{Namespace}.{nameof(EnsureAttribute)}",
            predicate: KeepNodeForPipeline<EnsureAttribute>,
            transform: TransformContractAttributes);

        context.RegisterSourceOutput(pipelineAccess, OutputContractMethod);
        context.RegisterSourceOutput(pipelineRequireNotNull, OutputContractMethod);
        context.RegisterSourceOutput(pipelineRequire, OutputContractMethod);
        context.RegisterSourceOutput(pipelineEnsure, OutputContractMethod);
    }

    private static void RegisterAccessAttribute(IncrementalGeneratorInitializationContext context, string @namespace)
    {
        string FileName = $"Method.{@namespace}.{nameof(AccessAttribute)}.cs";
        string NamespaceSpecifier = $"namespace {@namespace};";

        context.RegisterPostInitializationOutput(postInitializationContext =>
            postInitializationContext.AddSource(FileName, SourceText.From(NamespaceSpecifier + """

                using System;

                /// <summary>
                /// Represents the generated method access specifiers attribute.
                /// </summary>
                [AttributeUsage(AttributeTargets.Method)]
                public sealed class AccessAttribute : Attribute
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="AccessAttribute"/> class.
                    /// </summary>
                    /// <param name="specifiers">The method access specifiers.</param>
                    public AccessAttribute(params string[] specifiers)
                    {
                        Specifiers = specifiers;
                    }

                    /// <summary>
                    /// Gets the method access specifiers.
                    /// </summary>
                    public string[] Specifiers { get; }
                }
                """,
                Encoding.UTF8)));
    }

    private static void RegisterRequireNotNullAttribute(IncrementalGeneratorInitializationContext context, string @namespace)
    {
        string FileName = $"Method.{@namespace}.{nameof(RequireNotNullAttribute)}.cs";
        string NamespaceSpecifier = $"namespace {@namespace};";

        context.RegisterPostInitializationOutput(postInitializationContext =>
            postInitializationContext.AddSource(FileName, SourceText.From(NamespaceSpecifier + """

                using System;

                /// <summary>
                /// Represents one or more arguments that must not be null.
                /// </summary>
                [AttributeUsage(AttributeTargets.Method)]
                public sealed class RequireNotNullAttribute : Attribute
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="RequireNotNullAttribute"/> class.
                    /// </summary>
                    /// <param name="argumentNames">The argument names.</param>
                    public RequireNotNullAttribute(params string[] argumentNames)
                    {
                        ArgumentNames = argumentNames;
                    }

                    /// <summary>
                    /// Gets the argument names.
                    /// </summary>
                    public string[] ArgumentNames { get; }
                }
                """,
                Encoding.UTF8)));
    }

    private static void RegisterRequireAttribute(IncrementalGeneratorInitializationContext context, string @namespace)
    {
        string FileName = $"Method.{@namespace}.{nameof(RequireAttribute)}.cs";
        string NamespaceSpecifier = $"namespace {@namespace};";

        context.RegisterPostInitializationOutput(postInitializationContext =>
            postInitializationContext.AddSource(FileName, SourceText.From(NamespaceSpecifier + """

                using System;

                /// <summary>
                /// Represents one or more requirements.
                /// </summary>
                [AttributeUsage(AttributeTargets.Method)]
                public sealed class RequireAttribute : Attribute
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="RequireAttribute"/> class.
                    /// </summary>
                    /// <param name="requirements">The requirements.</param>
                    public RequireAttribute(params string[] requirements)
                    {
                        Requirements = requirements;
                    }

                    /// <summary>
                    /// Gets the requirements.
                    /// </summary>
                    public string[] Requirements { get; }
                }
                """,
                Encoding.UTF8)));
    }

    private static void RegisterEnsureAttribute(IncrementalGeneratorInitializationContext context, string @namespace)
    {
        string FileName = $"Method.{@namespace}.{nameof(EnsureAttribute)}.cs";
        string NamespaceSpecifier = $"namespace {@namespace};";

        context.RegisterPostInitializationOutput(postInitializationContext =>
            postInitializationContext.AddSource(FileName, SourceText.From(NamespaceSpecifier + """

                using System;

                /// <summary>
                /// Represents one or more guarantees.
                /// </summary>
                [AttributeUsage(AttributeTargets.Method)]
                public sealed class EnsureAttribute : Attribute
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="EnsureAttribute"/> class.
                    /// </summary>
                    /// <param name="guarantees">The guarantees.</param>
                    public EnsureAttribute(params string[] guarantees)
                    {
                        Guarantees = guarantees;
                    }

                    /// <summary>
                    /// Gets the guarantees.
                    /// </summary>
                    public string[] Guarantees { get; }
                }
                """,
                Encoding.UTF8)));
    }

    private static bool KeepNodeForPipeline<T>(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        where T : Attribute
    {
        // Only accept methods with the 'Verified' suffix in their name.
        if (syntaxNode is not MethodDeclarationSyntax MethodDeclaration)
            return false;

        string MethodName = MethodDeclaration.Identifier.ToString();
        if (!MethodName.EndsWith(VerifiedSuffix, StringComparison.Ordinal) || MethodName.Length == VerifiedSuffix.Length)
            return false;

        // Get a list of all supported attributes for this method.
        List<string> AttributeNames = new();
        foreach (var MethodAttributeList in MethodDeclaration.AttributeLists)
            if (MethodAttributeList is AttributeListSyntax AttributeList)
            {
                foreach (var MethodAttribute in AttributeList.Attributes)
                    if (MethodAttribute is AttributeSyntax Attribute)
                    {
                        string AttributeName = ToAttributeName(Attribute);
                        if (SupportedAttributeNames.Contains(AttributeName))
                            AttributeNames.Add(AttributeName);
                    }
            }

        // One of these attributes has to be the first, and we only return true for this one.
        // This way, multiple calls with different T return true exactly once.
        if (AttributeNames.Count == 0 || AttributeNames[0] != typeof(T).Name)
            return false;

        return true;
    }

    private static ContractModel TransformContractAttributes(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        ContractModel Model = GetModelWithoutContract(context, cancellationToken);
        Model = Model with { Attributes = GetModelContract(context, cancellationToken) };
        Model = Model with { GeneratedMethodDeclaration = GetGeneratedMethodDeclaration(Model, context, cancellationToken) };

        return Model;
    }

    private static ContractModel GetModelWithoutContract(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var containingClass = context.TargetSymbol.ContainingType;

        // Note: this is a simplified example. You will also need to handle the case where the type is in a global namespace, nested, etc.
        string Namespace = containingClass.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))!;
        string ClassName = containingClass.Name;
        string SymbolName = context.TargetSymbol.Name;

        Debug.Assert(SymbolName.EndsWith(VerifiedSuffix, StringComparison.Ordinal));
        Debug.Assert(SymbolName.Length > VerifiedSuffix.Length);
        string ShortMethodName = SymbolName.Substring(0, SymbolName.Length - VerifiedSuffix.Length);

        return new ContractModel(
            Namespace: Namespace,
            ClassName: ClassName,
            ShortMethodName: ShortMethodName,
            Attributes: new List<AttributeModel>(),
            GeneratedMethodDeclaration: string.Empty);
    }

    private static List<AttributeModel> GetModelContract(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        SyntaxNode TargetNode = context.TargetNode;

        Debug.Assert(TargetNode is MethodDeclarationSyntax);

        MethodDeclarationSyntax MethodDeclaration = (MethodDeclarationSyntax)TargetNode;

        List<AttributeModel> Result = new();

        foreach (var MethodAttributeList in MethodDeclaration.AttributeLists)
            if (MethodAttributeList is AttributeListSyntax AttributeList)
            {
                foreach (var MethodAttribute in AttributeList.Attributes)
                    if (MethodAttribute is AttributeSyntax Attribute)
                    {
                        string AttributeName = ToAttributeName(Attribute);
                        if (SupportedAttributeNames.Contains(AttributeName))
                        {
                            List<string> Arguments = new();

                            if (Attribute.ArgumentList is AttributeArgumentListSyntax AttributeArgumentList)
                            {
                                foreach (var Argument in AttributeArgumentList.Arguments)
                                    if (Argument is AttributeArgumentSyntax AttributeArgument)
                                    {
                                        if (AttributeArgument.Expression is LiteralExpressionSyntax LiteralExpression)
                                        {
                                            string TokenText = LiteralExpression.GetText().ToString();
                                            Arguments.Add(TokenText);
                                        }
                                    }
                            }

                            AttributeModel Model = new(AttributeName, Arguments);

                            Result.Add(Model);
                        }
                    }
            }

        return Result;
    }

    private static string GetGeneratedMethodDeclaration(ContractModel model, GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        SyntaxNode TargetNode = context.TargetNode;

        Debug.Assert(TargetNode is MethodDeclarationSyntax);
        MethodDeclarationSyntax MethodDeclaration = (MethodDeclarationSyntax)TargetNode;

        SyntaxTriviaList? LeadingTrivia = GetModifiersLeadingTrivia(MethodDeclaration);
        SyntaxTriviaList? TrailingTrivia = GetModifiersTrailingTrivia(MethodDeclaration);

        SyntaxList<AttributeListSyntax> EmptyAttributes = SyntaxFactory.List(new List<AttributeListSyntax>());
        MethodDeclaration = MethodDeclaration.WithAttributeLists(EmptyAttributes);

        SyntaxToken ShortIdentifier = SyntaxFactory.Identifier(model.ShortMethodName);
        MethodDeclaration = MethodDeclaration.WithIdentifier(ShortIdentifier);

        List<SyntaxToken> ModifierTokens = new();
        if (model.Attributes.Find(m => m.Name == nameof(AccessAttribute)) is AttributeModel AccessAttributeModel)
        {
            for (int i = 0; i < AccessAttributeModel.Arguments.Count; i++)
            {
                string Argument = AccessAttributeModel.Arguments[i];
                SyntaxToken ModifierToken = SyntaxFactory.Identifier(Argument.Trim('"'));

                if (i > 0)
                    ModifierToken = ModifierToken.WithLeadingTrivia(SyntaxFactory.Space);

                if (i + 1 == AccessAttributeModel.Arguments.Count)
                {
                    if (TrailingTrivia is not null)
                        ModifierToken = ModifierToken.WithTrailingTrivia(TrailingTrivia);
                }

                ModifierTokens.Add(ModifierToken);
            }
        }
        else
        {
            SyntaxToken ModifierToken = SyntaxFactory.Identifier("public");
            if (TrailingTrivia is not null)
                ModifierToken = ModifierToken.WithTrailingTrivia(TrailingTrivia);

            ModifierTokens.Add(ModifierToken);
        }

        SyntaxTokenList Modifiers = SyntaxFactory.TokenList(ModifierTokens);
        MethodDeclaration = MethodDeclaration.WithModifiers(Modifiers);

        if (LeadingTrivia is not null)
            MethodDeclaration = MethodDeclaration.WithLeadingTrivia(LeadingTrivia);

        return MethodDeclaration.ToFullString();
    }

    private static SyntaxTriviaList? GetModifiersLeadingTrivia(MethodDeclarationSyntax methodDeclaration)
    {
        SyntaxTriviaList? LeadingTrivia = null;

        if (methodDeclaration.HasLeadingTrivia)
            LeadingTrivia = methodDeclaration.GetLeadingTrivia();
        else
        {
            AttributeSyntax? FirstAttribute = null;
            foreach (var MethodAttributeList in methodDeclaration.AttributeLists)
                if (MethodAttributeList is AttributeListSyntax AttributeList)
                {
                    foreach (var MethodAttribute in AttributeList.Attributes)
                        if (MethodAttribute is AttributeSyntax Attribute)
                        {
                            FirstAttribute = Attribute;
                            break;
                        }

                    if (FirstAttribute is not null)
                        break;
                }

            if (FirstAttribute is not null && FirstAttribute.HasLeadingTrivia)
                LeadingTrivia = FirstAttribute.GetLeadingTrivia();
        }

        return LeadingTrivia;
    }

    private static SyntaxTriviaList? GetModifiersTrailingTrivia(MethodDeclarationSyntax methodDeclaration)
    {
        return methodDeclaration.Modifiers.Count > 0 ? methodDeclaration.Modifiers.Last().TrailingTrivia : null;
    }

    private static string ToAttributeName(AttributeSyntax attribute)
    {
        return $"{attribute.Name.GetText()}{nameof(Attribute)}";
    }

    private static void OutputContractMethod(SourceProductionContext context, ContractModel model)
    {
        var sourceText = SourceText.From($$"""
                namespace {{model.Namespace}};

                using System;

                partial class {{model.ClassName}}
                {{{model.GeneratedMethodDeclaration}}}
                """,
            Encoding.UTF8);

        context.AddSource($"{model.ClassName}_{model.ShortMethodName}.g.cs", sourceText);
    }

    private record ContractModel(string Namespace, string ClassName, string ShortMethodName, List<AttributeModel> Attributes, string GeneratedMethodDeclaration);
    private record AttributeModel(string Name, List<string> Arguments);
}
