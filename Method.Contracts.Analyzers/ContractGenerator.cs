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
public class ContractGenerator : IIncrementalGenerator
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

    /// <summary>
    /// The key in .editorconfig for the suffix that a method must have for code to be generated.
    /// </summary>
    public const string VerifiedSuffixKey = "VerifiedSuffix";

    /// <summary>
    /// The default value for the suffix that a method must have for code to be generated.
    /// </summary>
    public const string DefaultVerifiedSuffix = "Verified";

    /// <summary>
    /// The key in .editorconfig for the tab length in generated code.
    /// </summary>
    public const string TabLengthKey = "TabLength";

    /// <summary>
    /// The default value for the tab length in generated code.
    /// </summary>
    public const int DefaultTabLength = 4;

    /// <summary>
    /// The key in .editorconfig for the name of the result identifier in generated queries.
    /// </summary>
    public const string ResultIdentifierKey = "ResultIdentifier";

    /// <summary>
    /// The default value for the name of the result identifier in generated queries.
    /// </summary>
    public const string DefaultResultIdentifier = "Result";

    // The settings values.
    private static readonly GeneratorSettingsEntry VerifiedSuffixSetting = new(BuildKey: VerifiedSuffixKey, DefaultValue: DefaultVerifiedSuffix);
    private static readonly GeneratorSettingsEntry TabLengthSetting = new(BuildKey: TabLengthKey, DefaultValue: $"{DefaultTabLength}");
    private static readonly GeneratorSettingsEntry ResultIdentifierSetting = new(BuildKey: ResultIdentifierKey, DefaultValue: DefaultResultIdentifier);
    private static GeneratorSettings Settings = new(VerifiedSuffix: DefaultVerifiedSuffix, TabLength: DefaultTabLength, ResultIdentifier: DefaultResultIdentifier);

    /// <inheritdoc cref="IIncrementalGenerator.Initialize"/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var Settings = context.AnalyzerConfigOptionsProvider.SelectMany(ReadSettings);

        InitializePipeline<AccessAttribute>(context, Settings);
        InitializePipeline<RequireNotNullAttribute>(context, Settings);
        InitializePipeline<RequireAttribute>(context, Settings);
        InitializePipeline<EnsureAttribute>(context, Settings);
    }

    private static IEnumerable<GeneratorSettings> ReadSettings(AnalyzerConfigOptionsProvider options, CancellationToken cancellationToken)
    {
        string VerifiedSuffix = VerifiedSuffixSetting.ReadAsString(options, out _);
        int TabLength = TabLengthSetting.ReadAsInt(options, out _);
        string ResultIdentifier = ResultIdentifierSetting.ReadAsString(options, out _);

        Settings = Settings with
        {
            VerifiedSuffix = VerifiedSuffix,
            TabLength = TabLength,
            ResultIdentifier = ResultIdentifier,
        };

        return new List<GeneratorSettings>() { Settings };
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
                    bool AreAllArgumentsValid = AttributeArgumentList.Arguments.All(attributeArgument => IsValidAttributeArgument(attributeArgument));

                    if (HasAtLeastOneArgument && AreAllArgumentsValid)
                        AttributeNames.Add(AttributeName);
                }
            }
        }

        // One of these attributes has to be the first, and we only return true for this one.
        // This way, multiple calls with different T return true exactly once.
        if (AttributeNames.Count == 0 || AttributeNames[0] != typeof(T).Name)
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

        if (attributeArgument.Expression is LiteralExpressionSyntax LiteralExpression && LiteralExpression.Kind() == SyntaxKind.StringLiteralExpression)
            return true;

        return false;
    }

    private static ContractModel TransformContractAttributes(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        ContractModel Model = GetModelWithoutContract(context);
        Model = Model with { Attributes = GetModelContract(context) };
        Model = Model with { GeneratedMethodDeclaration = GetGeneratedMethodDeclaration(Model, context) };

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
            ClassName: ClassName,
            ShortMethodName: ShortMethodName,
            Attributes: new List<AttributeModel>(),
            GeneratedMethodDeclaration: string.Empty);
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
                if (SupportedAttributeNames.Contains(AttributeName))
                {
                    Debug.Assert(Attribute.ArgumentList is AttributeArgumentListSyntax, "Nodes for attributes without arguments are filtered away.");
                    AttributeArgumentListSyntax AttributeArgumentList = Attribute.ArgumentList!;

                    List<string> Arguments = new();

                    for (int IndexArgument = 0; IndexArgument < AttributeArgumentList.Arguments.Count; IndexArgument++)
                    {
                        AttributeArgumentSyntax AttributeArgument = AttributeArgumentList.Arguments[IndexArgument];
                        Debug.Assert(IsValidAttributeArgument(AttributeArgument), $"Invalid attribute argument: {AttributeArgument}.");

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

    private static string GetGeneratedMethodDeclaration(ContractModel model, GeneratorAttributeSyntaxContext context)
    {
        SyntaxNode TargetNode = context.TargetNode;

        Debug.Assert(TargetNode is MethodDeclarationSyntax);
        MethodDeclarationSyntax MethodDeclaration = (MethodDeclarationSyntax)TargetNode;

        string Tab = new(' ', Math.Max(Settings.TabLength, 1));
        SyntaxTriviaList LeadingTrivia = GetLeadingTriviaWithLineEnd(Tab);
        SyntaxTriviaList LeadingTriviaWithoutLineEnd = GetLeadingTriviaWithoutLineEnd(Tab);
        SyntaxTriviaList? TrailingTrivia = GetModifiersTrailingTrivia(MethodDeclaration);

        SyntaxList<AttributeListSyntax> CodeAttributes = GenerateCodeAttributes();
        MethodDeclaration = MethodDeclaration.WithAttributeLists(CodeAttributes);

        SyntaxToken ShortIdentifier = SyntaxFactory.Identifier(model.ShortMethodName);
        MethodDeclaration = MethodDeclaration.WithIdentifier(ShortIdentifier);

        SyntaxTokenList Modifiers = GenerateContractModifiers(model, MethodDeclaration, LeadingTrivia, TrailingTrivia, out bool IsAsync);
        MethodDeclaration = MethodDeclaration.WithModifiers(Modifiers);

        BlockSyntax MethodBody = GenerateBody(model, MethodDeclaration, LeadingTrivia, LeadingTriviaWithoutLineEnd, IsAsync, Tab);
        MethodDeclaration = MethodDeclaration.WithBody(MethodBody);

        MethodDeclaration = MethodDeclaration.WithLeadingTrivia(LeadingTriviaWithoutLineEnd);

        return MethodDeclaration.ToFullString();
    }

    private static SyntaxList<AttributeListSyntax> GenerateCodeAttributes()
    {
        NameSyntax AttributeName = SyntaxFactory.IdentifierName(nameof(GeneratedCodeAttribute));

        string ToolName = GetToolName();
        SyntaxToken ToolNameToken = SyntaxFactory.Literal(ToolName);
        LiteralExpressionSyntax ToolNameExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, ToolNameToken);
        AttributeArgumentSyntax ToolNameAttributeArgument = SyntaxFactory.AttributeArgument(ToolNameExpression);

        string ToolVersion = GetToolVersion();
        SyntaxToken ToolVersionToken = SyntaxFactory.Literal(ToolVersion);
        LiteralExpressionSyntax ToolVersionExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, ToolVersionToken);
        AttributeArgumentSyntax ToolVersionAttributeArgument = SyntaxFactory.AttributeArgument(ToolVersionExpression);

        AttributeArgumentListSyntax ArgumentList = SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(new List<AttributeArgumentSyntax>() { ToolNameAttributeArgument, ToolVersionAttributeArgument }));
        AttributeSyntax Attribute = SyntaxFactory.Attribute(AttributeName, ArgumentList);
        AttributeListSyntax AttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new List<AttributeSyntax>() { Attribute }));
        SyntaxList<AttributeListSyntax> Attributes = SyntaxFactory.List(new List<AttributeListSyntax>() { AttributeList });

        return Attributes;
    }

    private static string GetToolName()
    {
        AssemblyName ExecutingAssemblyName = Assembly.GetExecutingAssembly().GetName();
        return $"{ExecutingAssemblyName.Name}";
    }

    private static string GetToolVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }

    private static SyntaxTokenList GenerateContractModifiers(ContractModel model, MethodDeclarationSyntax methodDeclaration, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia, out bool isAsync)
    {
        List<SyntaxToken> ModifierTokens = new();

        if (model.Attributes.Find(m => m.Name == nameof(AccessAttribute)) is AttributeModel AccessAttributeModel)
            ModifierTokens = GenerateContractExplicitModifiers(AccessAttributeModel, leadingTrivia, trailingTrivia, out isAsync);
        else
            ModifierTokens = GenerateContractDefaultModifiers(methodDeclaration, leadingTrivia, trailingTrivia, out isAsync);

        return SyntaxFactory.TokenList(ModifierTokens);
    }

    private static List<SyntaxToken> GenerateContractExplicitModifiers(AttributeModel accessAttributeModel, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia, out bool isAsync)
    {
        List<SyntaxToken> ModifierTokens = new();
        isAsync = false;

        for (int i = 0; i < accessAttributeModel.Arguments.Count; i++)
        {
            string Argument = accessAttributeModel.Arguments[i];
            SyntaxToken ModifierToken = SyntaxFactory.Identifier(Argument);

            if (i == 0)
                ModifierToken = ModifierToken.WithLeadingTrivia(leadingTrivia);
            else
                ModifierToken = ModifierToken.WithLeadingTrivia(SyntaxFactory.Space);

            if (i + 1 == accessAttributeModel.Arguments.Count)
            {
                if (trailingTrivia is not null)
                    ModifierToken = ModifierToken.WithTrailingTrivia(trailingTrivia);
            }

            ModifierTokens.Add(ModifierToken);

            if (Argument is "async")
                isAsync = true;
        }

        return ModifierTokens;
    }

    private static List<SyntaxToken> GenerateContractDefaultModifiers(MethodDeclarationSyntax methodDeclaration, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia, out bool isAsync)
    {
        List<SyntaxToken> ModifierTokens = new();
        isAsync = false;

        SyntaxToken PublicModifierToken = SyntaxFactory.Identifier("public");
        PublicModifierToken = PublicModifierToken.WithLeadingTrivia(leadingTrivia);
        ModifierTokens.Add(PublicModifierToken);

        // If the method is static and/or async, add the same static modifier to the generated code.
        foreach (var Modifier in methodDeclaration.Modifiers)
        {
            string ModifierText = Modifier.Text;

            if (ModifierText is "static" or "async")
            {
                SyntaxToken StaticModifierToken = SyntaxFactory.Identifier(Modifier.Text);
                StaticModifierToken = StaticModifierToken.WithLeadingTrivia(SyntaxFactory.Space);
                ModifierTokens.Add(StaticModifierToken);

                if (ModifierText is "async")
                    isAsync = true;
            }
        }

        if (trailingTrivia is not null)
        {
            int LastItemIndex = methodDeclaration.Modifiers.Count - 1;
            ModifierTokens[LastItemIndex] = ModifierTokens[LastItemIndex].WithTrailingTrivia(trailingTrivia);
        }

        return ModifierTokens;
    }

    private static SyntaxTriviaList GetLeadingTriviaWithLineEnd(string tab)
    {
        List<SyntaxTrivia> Trivias = new()
        {
            SyntaxFactory.EndOfLine("\n"),
            SyntaxFactory.Whitespace(tab),
        };

        return SyntaxFactory.TriviaList(Trivias);
    }

    private static SyntaxTriviaList GetLeadingTriviaWithoutLineEnd(string tab)
    {
        List<SyntaxTrivia> Trivias = new()
        {
            SyntaxFactory.Whitespace(tab),
        };

        return SyntaxFactory.TriviaList(Trivias);
    }

    private static SyntaxTriviaList? GetModifiersTrailingTrivia(MethodDeclarationSyntax methodDeclaration)
    {
        return methodDeclaration.Modifiers.Count > 0 ? methodDeclaration.Modifiers.Last().TrailingTrivia : null;
    }

    private static BlockSyntax GenerateBody(ContractModel model, MethodDeclarationSyntax methodDeclaration, SyntaxTriviaList tabTrivia, SyntaxTriviaList tabTriviaWithoutLineEnd, bool isAsync, string tab)
    {
        SyntaxToken OpenBraceToken = SyntaxFactory.Token(SyntaxKind.OpenBraceToken);
        OpenBraceToken = OpenBraceToken.WithLeadingTrivia(tabTriviaWithoutLineEnd);

        List<SyntaxTrivia> TrivialList = new(tabTrivia);
        TrivialList.Add(SyntaxFactory.Whitespace(tab));
        SyntaxTriviaList TabStatementTrivia = SyntaxFactory.TriviaList(TrivialList);

        List<SyntaxTrivia> TrivialListExtraLineEnd = new(tabTrivia);
        TrivialListExtraLineEnd.Insert(0, SyntaxFactory.EndOfLine("\n"));
        TrivialListExtraLineEnd.Add(SyntaxFactory.Whitespace(tab));
        SyntaxTriviaList TabStatementExtraLineEndTrivia = SyntaxFactory.TriviaList(TrivialListExtraLineEnd);

        SyntaxToken CloseBraceToken = SyntaxFactory.Token(SyntaxKind.CloseBraceToken);
        CloseBraceToken = CloseBraceToken.WithLeadingTrivia(tabTrivia);

        List<StatementSyntax> Statements = GenerateStatements(model, methodDeclaration, TabStatementTrivia, TabStatementExtraLineEndTrivia, isAsync);

        return SyntaxFactory.Block(OpenBraceToken, SyntaxFactory.List(Statements), CloseBraceToken);
    }

    private static List<StatementSyntax> GenerateStatements(ContractModel model, MethodDeclarationSyntax methodDeclaration, SyntaxTriviaList tabStatementTrivia, SyntaxTriviaList tabStatementExtraLineEndTrivia, bool isAsync)
    {
        List<StatementSyntax> Statements = new();

        GetParameterReplacementTable(model, out Dictionary<string, string> ParameterNameReplacementTable, out bool IsContainingRequire);
        GetCallAndReturnStatements(model,
                                   methodDeclaration,
                                   tabStatementTrivia,
                                   tabStatementExtraLineEndTrivia,
                                   ParameterNameReplacementTable,
                                   IsContainingRequire,
                                   isAsync,
                                   out StatementSyntax CallStatement,
                                   out StatementSyntax? ReturnStatement);

        int CallStatementIndex = -1;
        foreach (AttributeModel AttributeModel in model.Attributes)
            if (AttributeModel.Name != nameof(AccessAttribute))
                AddStatement(methodDeclaration, tabStatementTrivia, tabStatementExtraLineEndTrivia, Statements, AttributeModel, ref CallStatementIndex);

        if (CallStatementIndex < 0)
            CallStatementIndex = Statements.Count;

        Statements.Insert(CallStatementIndex, CallStatement);

        if (ReturnStatement is not null)
            Statements.Add(ReturnStatement.WithLeadingTrivia(tabStatementExtraLineEndTrivia));

        return Statements;
    }

    private static void GetParameterReplacementTable(ContractModel model, out Dictionary<string, string> parameterNameReplacementTable, out bool isContainingRequire)
    {
        parameterNameReplacementTable = new();
        isContainingRequire = false;

        foreach (AttributeModel Item in model.Attributes)
            if (Item.Name == nameof(RequireNotNullAttribute))
            {
                foreach (string Argument in Item.Arguments)
                    parameterNameReplacementTable.Add(Argument, ToIdentifierLocalName(Argument));

                isContainingRequire = true;
            }
            else if (Item.Name == nameof(RequireAttribute))
                isContainingRequire = true;
    }

    private static void GetCallAndReturnStatements(ContractModel model,
                                                   MethodDeclarationSyntax methodDeclaration,
                                                   SyntaxTriviaList tabStatementTrivia,
                                                   SyntaxTriviaList tabStatementExtraLineEndTrivia,
                                                   Dictionary<string, string> parameterNameReplacementTable,
                                                   bool isContainingRequire,
                                                   bool isAsync,
                                                   out StatementSyntax callStatement,
                                                   out StatementSyntax? returnStatement)
    {
        if (IsCommandMethod(methodDeclaration, isAsync))
        {
            callStatement = GenerateCommandStatement(model.ShortMethodName, methodDeclaration.ParameterList, parameterNameReplacementTable, isAsync);
            returnStatement = null;
        }
        else
        {
            callStatement = GenerateQueryStatement(model.ShortMethodName, methodDeclaration.ParameterList, parameterNameReplacementTable, isAsync);
            returnStatement = GenerateReturnStatement();
        }

        if (isContainingRequire)
            callStatement = callStatement.WithLeadingTrivia(tabStatementExtraLineEndTrivia);
        else
            callStatement = callStatement.WithLeadingTrivia(tabStatementTrivia);
    }

    private static bool IsCommandMethod(MethodDeclarationSyntax methodDeclaration, bool isAsync)
    {
        return (isAsync && methodDeclaration.ReturnType is IdentifierNameSyntax IdentifierName && IdentifierName.Identifier.IsKind(SyntaxKind.IdentifierToken) && IdentifierName.Identifier.Text == "Task") ||
               (!isAsync && methodDeclaration.ReturnType is PredefinedTypeSyntax PredefinedType && PredefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword));
    }

    private static void AddStatement(MethodDeclarationSyntax methodDeclaration,
                                     SyntaxTriviaList tabStatementTrivia,
                                     SyntaxTriviaList tabStatementExtraLineEndTrivia,
                                     List<StatementSyntax> statements,
                                     AttributeModel attributeModel,
                                     ref int callStatementIndex)
    {
        bool FirstEnsure = false;
        if (callStatementIndex < 0 && attributeModel.Name == nameof(EnsureAttribute))
        {
            callStatementIndex = statements.Count;
            FirstEnsure = true;
        }

        List<StatementSyntax> AttributeStatements = GenerateAttributeStatements(attributeModel, methodDeclaration);
        foreach (StatementSyntax Statement in AttributeStatements)
        {
            if (FirstEnsure)
            {
                FirstEnsure = false;
                statements.Add(Statement.WithLeadingTrivia(tabStatementExtraLineEndTrivia));
            }
            else
                statements.Add(Statement.WithLeadingTrivia(tabStatementTrivia));
        }
    }

    private static ExpressionStatementSyntax GenerateCommandStatement(string methodName, ParameterListSyntax parameterList, Dictionary<string, string> parameterNameReplacementTable, bool isAsync)
    {
        SyntaxTriviaList WhitespaceTrivia = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
        string VerifiedSuffix = Settings.VerifiedSuffix;
        ExpressionSyntax Invocation = SyntaxFactory.IdentifierName(methodName + VerifiedSuffix);

        List<ArgumentSyntax> Arguments = new();
        foreach (var CallParameter in parameterList.Parameters)
            if (CallParameter is ParameterSyntax Parameter)
            {
                bool IsRef = false;
                bool IsOut = false;

                foreach (var Modifier in Parameter.Modifiers)
                {
                    if (Modifier.IsKind(SyntaxKind.RefKeyword))
                        IsRef = true;
                    if (Modifier.IsKind(SyntaxKind.OutKeyword))
                        IsOut = true;
                }

                string ParameterName = Parameter.Identifier.Text;
                if (parameterNameReplacementTable.TryGetValue(ParameterName, out string ReplacedParameterName))
                    ParameterName = ReplacedParameterName;

                IdentifierNameSyntax ParameterIdentifier = SyntaxFactory.IdentifierName(ParameterName);

                ArgumentSyntax Argument;
                if (IsRef)
                    Argument = SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.RefKeyword), ParameterIdentifier.WithLeadingTrivia(WhitespaceTrivia));
                else if (IsOut)
                    Argument = SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), ParameterIdentifier.WithLeadingTrivia(WhitespaceTrivia));
                else
                    Argument = SyntaxFactory.Argument(ParameterIdentifier);

                if (Arguments.Count > 0)
                    Argument = Argument.WithLeadingTrivia(WhitespaceTrivia);

                Arguments.Add(Argument);
            }

        ArgumentListSyntax ArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(Arguments));
        ExpressionSyntax CallExpression = SyntaxFactory.InvocationExpression(Invocation, ArgumentList);

        if (isAsync)
            CallExpression = SyntaxFactory.AwaitExpression(CallExpression.WithLeadingTrivia(WhitespaceTrivia));

        ExpressionStatementSyntax ExpressionStatement = SyntaxFactory.ExpressionStatement(CallExpression);

        return ExpressionStatement;
    }

    private static LocalDeclarationStatementSyntax GenerateQueryStatement(string methodName, ParameterListSyntax parameterList, Dictionary<string, string> parameterNameReplacementTable, bool isAsync)
    {
        SyntaxTriviaList WhitespaceTrivia = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
        string VerifiedSuffix = Settings.VerifiedSuffix;
        ExpressionSyntax Invocation = SyntaxFactory.IdentifierName(methodName + VerifiedSuffix);

        List<ArgumentSyntax> Arguments = new();
        foreach (var CallParameter in parameterList.Parameters)
            if (CallParameter is ParameterSyntax Parameter)
            {
                bool IsRef = false;
                bool IsOut = false;

                foreach (var Modifier in Parameter.Modifiers)
                {
                    if (Modifier.IsKind(SyntaxKind.RefKeyword))
                        IsRef = true;
                    if (Modifier.IsKind(SyntaxKind.OutKeyword))
                        IsOut = true;
                }

                string ParameterName = Parameter.Identifier.Text;
                if (parameterNameReplacementTable.TryGetValue(ParameterName, out string ReplacedParameterName))
                    ParameterName = ReplacedParameterName;

                IdentifierNameSyntax ParameterIdentifier = SyntaxFactory.IdentifierName(ParameterName);

                ArgumentSyntax Argument;
                if (IsRef)
                    Argument = SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.RefKeyword), ParameterIdentifier.WithLeadingTrivia(WhitespaceTrivia));
                else if (IsOut)
                    Argument = SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), ParameterIdentifier.WithLeadingTrivia(WhitespaceTrivia));
                else
                    Argument = SyntaxFactory.Argument(ParameterIdentifier);

                if (Arguments.Count > 0)
                    Argument = Argument.WithLeadingTrivia(WhitespaceTrivia);

                Arguments.Add(Argument);
            }

        ArgumentListSyntax ArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(Arguments));
        ExpressionSyntax CallExpression = SyntaxFactory.InvocationExpression(Invocation, ArgumentList).WithLeadingTrivia(WhitespaceTrivia);

        if (isAsync)
            CallExpression = SyntaxFactory.AwaitExpression(CallExpression).WithLeadingTrivia(WhitespaceTrivia);

        IdentifierNameSyntax VarIdentifier = SyntaxFactory.IdentifierName("var");
        SyntaxToken ResultIdentifier = SyntaxFactory.Identifier(Settings.ResultIdentifier);
        EqualsValueClauseSyntax Initializer = SyntaxFactory.EqualsValueClause(CallExpression).WithLeadingTrivia(WhitespaceTrivia);
        VariableDeclaratorSyntax VariableDeclarator = SyntaxFactory.VariableDeclarator(ResultIdentifier, null, Initializer).WithLeadingTrivia(WhitespaceTrivia);
        VariableDeclarationSyntax Declaration = SyntaxFactory.VariableDeclaration(VarIdentifier, SyntaxFactory.SeparatedList(new List<VariableDeclaratorSyntax>() { VariableDeclarator }));
        LocalDeclarationStatementSyntax LocalDeclarationStatement = SyntaxFactory.LocalDeclarationStatement(Declaration);

        return LocalDeclarationStatement;
    }

    private static ReturnStatementSyntax GenerateReturnStatement()
    {
        SyntaxTriviaList WhitespaceTrivia = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
        IdentifierNameSyntax ResultIdentifier = SyntaxFactory.IdentifierName(Settings.ResultIdentifier).WithLeadingTrivia(WhitespaceTrivia);
        ReturnStatementSyntax ReturnStatement = SyntaxFactory.ReturnStatement(ResultIdentifier);

        return ReturnStatement;
    }

    private static List<StatementSyntax> GenerateAttributeStatements(AttributeModel attributeModel, MethodDeclarationSyntax methodDeclaration)
    {
        Dictionary<string, Func<string, MethodDeclarationSyntax, StatementSyntax>> GeneratorTable = new()
        {
            { nameof(RequireNotNullAttribute), GenerateRequireNotNullStatement },
            { nameof(RequireAttribute), GenerateRequireStatement },
            { nameof(EnsureAttribute), GenerateEnsureStatement },
        };

        Debug.Assert(GeneratorTable.ContainsKey(attributeModel.Name));

        List<StatementSyntax> Statements = new();
        foreach (string ArgumentName in attributeModel.Arguments)
            Statements.Add(GeneratorTable[attributeModel.Name](ArgumentName, methodDeclaration));

        return Statements;
    }

    private static StatementSyntax GenerateRequireNotNullStatement(string argumentName, MethodDeclarationSyntax methodDeclaration)
    {
        ExpressionSyntax ContractName = SyntaxFactory.IdentifierName(ContractClassName);
        SimpleNameSyntax RequireNotNullName = SyntaxFactory.IdentifierName(ToNameWithoutAttribute<RequireNotNullAttribute>());
        MemberAccessExpressionSyntax MemberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ContractName, RequireNotNullName);

        SyntaxTriviaList WhitespaceTrivia = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
        IdentifierNameSyntax InputName = SyntaxFactory.IdentifierName(argumentName);
        ArgumentSyntax InputArgument = SyntaxFactory.Argument(InputName);

        TypeSyntax ParameterType = GetParameterType(argumentName, methodDeclaration);
        SyntaxToken VariableName = SyntaxFactory.Identifier(ToIdentifierLocalName(argumentName));
        VariableDesignationSyntax VariableDesignation = SyntaxFactory.SingleVariableDesignation(VariableName);
        DeclarationExpressionSyntax DeclarationExpression = SyntaxFactory.DeclarationExpression(ParameterType, VariableDesignation.WithLeadingTrivia(WhitespaceTrivia));
        ArgumentSyntax OutputArgument = SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), DeclarationExpression.WithLeadingTrivia(WhitespaceTrivia));
        OutputArgument = OutputArgument.WithLeadingTrivia(WhitespaceTrivia);

        List<ArgumentSyntax> Arguments = new() { InputArgument, OutputArgument };
        ArgumentListSyntax ArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(Arguments));

        ExpressionSyntax CallExpression = SyntaxFactory.InvocationExpression(MemberAccessExpression, ArgumentList);
        ExpressionStatementSyntax ExpressionStatement = SyntaxFactory.ExpressionStatement(CallExpression);

        return ExpressionStatement;
    }

    private static PredefinedTypeSyntax GetParameterType(string argumentName, MethodDeclarationSyntax methodDeclaration)
    {
        return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword));
    }

    private static ExpressionStatementSyntax GenerateRequireStatement(string argumentName, MethodDeclarationSyntax methodDeclaration)
    {
        return GenerateRequireOrEnsureStatement(argumentName, methodDeclaration, "Require");
    }

    private static ExpressionStatementSyntax GenerateEnsureStatement(string argumentName, MethodDeclarationSyntax methodDeclaration)
    {
        return GenerateRequireOrEnsureStatement(argumentName, methodDeclaration, "Ensure");
    }

    private static ExpressionStatementSyntax GenerateRequireOrEnsureStatement(string argumentName, MethodDeclarationSyntax methodDeclaration, string contractMethodName)
    {
        ExpressionSyntax ContractName = SyntaxFactory.IdentifierName(ContractClassName);
        SimpleNameSyntax ContractMethodSimpleName = SyntaxFactory.IdentifierName(contractMethodName);
        MemberAccessExpressionSyntax MemberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ContractName, ContractMethodSimpleName);

        IdentifierNameSyntax InputName = SyntaxFactory.IdentifierName(argumentName);
        ArgumentSyntax InputArgument = SyntaxFactory.Argument(InputName);
        List<ArgumentSyntax> Arguments = new() { InputArgument };
        ArgumentListSyntax ArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(Arguments));
        ExpressionSyntax CallExpression = SyntaxFactory.InvocationExpression(MemberAccessExpression, ArgumentList);
        ExpressionStatementSyntax ExpressionStatement = SyntaxFactory.ExpressionStatement(CallExpression);

        return ExpressionStatement;
    }

    private static string ToAttributeName(AttributeSyntax attribute)
    {
        return $"{attribute.Name.GetText()}{nameof(Attribute)}";
    }

    private static string ToNameWithoutAttribute<T>()
    {
        string LongName = typeof(T).Name;
        return LongName.Substring(0, LongName.Length - nameof(Attribute).Length);
    }

    private static string ToIdentifierLocalName(string text)
    {
        Debug.Assert(text.Length > 0);

        char FirstLetter = text[0];
        string OtherLetters = text.Substring(1);

        if (char.IsLower(FirstLetter))
            return $"{char.ToUpper(FirstLetter, CultureInfo.InvariantCulture)}{OtherLetters}";
        else
            return $"_{text}";
    }

    private static void OutputContractMethod(SourceProductionContext context, (GeneratorSettings Settings, ImmutableArray<ContractModel> Models) modelAndSettings)
    {
        foreach (ContractModel Model in modelAndSettings.Models)
        {
            string SourceText = $$"""
                namespace {{Model.Namespace}};

                using System;
                using System.CodeDom.Compiler;
                using Contracts;

                partial class {{Model.ClassName}}
                {
                {{Model.GeneratedMethodDeclaration}}
                }
                """;
            SourceText = SourceText.Replace("\r\n", "\n");

            context.AddSource($"{Model.ClassName}_{Model.ShortMethodName}.g.cs", Microsoft.CodeAnalysis.Text.SourceText.From(SourceText, Encoding.UTF8));
        }
    }
}
