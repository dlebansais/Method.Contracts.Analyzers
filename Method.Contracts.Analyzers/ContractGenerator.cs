namespace Contracts.Analyzers;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
    // List of supported attributes by their name.
    private static readonly List<string> SupportedAttributeNames = new()
    {
        nameof(AccessAttribute),
        nameof(RequireNotNullAttribute),
        nameof(RequireAttribute),
        nameof(EnsureAttribute),
    };

    // The suffix that a method must have for code to be generated.
    private const string VerifiedSuffix = "Verified";

    // One tab.
    private const string Tab = "    ";

    // Namespace of the Method.Contracts assemblies.
    private const string ContractNamespace = "Contract";

    // Namespace of the generated code.
    private const string ContractAnalyzerNamespace = "Contracts";

    // Name of the intermediate variable for methods that return a result.
    private const string ResultIdentifierName = "Result";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipelineAccess = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: GetFullyQualifiedMetadataName<AccessAttribute>(),
            predicate: KeepNodeForPipeline<AccessAttribute>,
            transform: TransformContractAttributes);

        var pipelineRequireNotNull = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: GetFullyQualifiedMetadataName<RequireNotNullAttribute>(),
            predicate: KeepNodeForPipeline<RequireNotNullAttribute>,
            transform: TransformContractAttributes);

        var pipelineRequire = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: GetFullyQualifiedMetadataName<RequireAttribute>(),
            predicate: KeepNodeForPipeline<RequireAttribute>,
            transform: TransformContractAttributes);

        var pipelineEnsure = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: GetFullyQualifiedMetadataName<EnsureAttribute>(),
            predicate: KeepNodeForPipeline<EnsureAttribute>,
            transform: TransformContractAttributes);

        context.RegisterSourceOutput(pipelineAccess, OutputContractMethod);
        context.RegisterSourceOutput(pipelineRequireNotNull, OutputContractMethod);
        context.RegisterSourceOutput(pipelineRequire, OutputContractMethod);
        context.RegisterSourceOutput(pipelineEnsure, OutputContractMethod);
    }

    private static string GetFullyQualifiedMetadataName<T>()
    {
        return $"{ContractAnalyzerNamespace}.{typeof(T).Name}";
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
        for (int IndexList = 0; IndexList < MethodDeclaration.AttributeLists.Count; IndexList++)
        {
            AttributeListSyntax AttributeList = MethodDeclaration.AttributeLists[IndexList];

            for (int Index = 0; Index < AttributeList.Attributes.Count; Index++)
            {
                AttributeSyntax Attribute = AttributeList.Attributes[Index];

                string AttributeName = ToAttributeName(Attribute);
                if (SupportedAttributeNames.Contains(AttributeName) && Attribute.ArgumentList is AttributeArgumentListSyntax AttributeArgumentList)
                {
                    bool AreAllArgumentsValid = AttributeArgumentList.Arguments.All(attributeArgument => attributeArgument.Expression is LiteralExpressionSyntax LiteralExpression && LiteralExpression.Kind() == SyntaxKind.StringLiteralExpression);
                    /*
                    for (int IndexArgument = 0; IndexArgument < AttributeArgumentList.Arguments.Count; IndexArgument++)
                    {
                        AttributeArgumentSyntax AttributeArgument = AttributeArgumentList.Arguments[IndexArgument];

                        if (AttributeArgument.Expression is LiteralExpressionSyntax LiteralExpression && LiteralExpression.Kind() == SyntaxKind.StringLiteralExpression)
                        {
                            string ArgumentText = LiteralExpression.Token.Text;
                            ArgumentText = ArgumentText.Trim('"');

                            Arguments.Add(ArgumentText);
                        }
                    }*/

                    if (AreAllArgumentsValid)
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

        Debug.Assert(TargetNode is MethodDeclarationSyntax);

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

                        Debug.Assert(AttributeArgument.Expression is LiteralExpressionSyntax);
                        LiteralExpressionSyntax LiteralExpression = (LiteralExpressionSyntax)AttributeArgument.Expression;

                        Debug.Assert(LiteralExpression.Kind() == SyntaxKind.StringLiteralExpression);
                        string ArgumentText = LiteralExpression.Token.Text;
                        ArgumentText = ArgumentText.Trim('"');

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

        SyntaxTriviaList LeadingTrivia = GetLeadingTriviaWithLineEnd();
        SyntaxTriviaList LeadingTriviaWithoutLineEnd = GetLeadingTriviaWithoutLineEnd();
        SyntaxTriviaList? TrailingTrivia = GetModifiersTrailingTrivia(MethodDeclaration);

        SyntaxList<AttributeListSyntax> CodeAttributes = GenerateCodeAttributes(LeadingTrivia);
        MethodDeclaration = MethodDeclaration.WithAttributeLists(CodeAttributes);

        SyntaxToken ShortIdentifier = SyntaxFactory.Identifier(model.ShortMethodName);
        MethodDeclaration = MethodDeclaration.WithIdentifier(ShortIdentifier);

        SyntaxTokenList Modifiers = GenerateContractModifiers(model, MethodDeclaration, LeadingTrivia, TrailingTrivia);
        MethodDeclaration = MethodDeclaration.WithModifiers(Modifiers);

        BlockSyntax MethodBody = GenerateBody(model, MethodDeclaration, LeadingTrivia, LeadingTriviaWithoutLineEnd);
        MethodDeclaration = MethodDeclaration.WithBody(MethodBody);

        MethodDeclaration = MethodDeclaration.WithLeadingTrivia(LeadingTriviaWithoutLineEnd);

        return MethodDeclaration.ToFullString();
    }

    private static SyntaxList<AttributeListSyntax> GenerateCodeAttributes(SyntaxTriviaList leadingTrivia)
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

    private static SyntaxTokenList GenerateContractModifiers(ContractModel model, MethodDeclarationSyntax methodDeclaration, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia)
    {
        List<SyntaxToken> ModifierTokens = new();

        if (model.Attributes.Find(m => m.Name == nameof(AccessAttribute)) is AttributeModel AccessAttributeModel)
            ModifierTokens = GenerateContractExplicitModifiers(AccessAttributeModel, leadingTrivia, trailingTrivia);
        else
            ModifierTokens = GenerateContractDefaultModifiers(methodDeclaration, leadingTrivia, trailingTrivia);

        return SyntaxFactory.TokenList(ModifierTokens);
    }

    private static List<SyntaxToken> GenerateContractExplicitModifiers(AttributeModel accessAttributeModel, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia)
    {
        List<SyntaxToken> ModifierTokens = new();

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
        }

        return ModifierTokens;
    }

    private static List<SyntaxToken> GenerateContractDefaultModifiers(MethodDeclarationSyntax methodDeclaration, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia)
    {
        List<SyntaxToken> ModifierTokens = new();

        SyntaxToken PublicModifierToken = SyntaxFactory.Identifier("public");
        PublicModifierToken = PublicModifierToken.WithLeadingTrivia(leadingTrivia);
        ModifierTokens.Add(PublicModifierToken);

        // If the method is static, add the same static modifier to the generated code.
        foreach (var Modifier in methodDeclaration.Modifiers)
            if (Modifier.Text == "static")
            {
                SyntaxToken StaticModifierToken = SyntaxFactory.Identifier("static");
                StaticModifierToken = StaticModifierToken.WithLeadingTrivia(SyntaxFactory.Space);
                ModifierTokens.Add(StaticModifierToken);
                break;
            }

        if (trailingTrivia is not null)
        {
            int LastItemIndex = methodDeclaration.Modifiers.Count - 1;
            ModifierTokens[LastItemIndex] = ModifierTokens[LastItemIndex].WithTrailingTrivia(trailingTrivia);
        }

        return ModifierTokens;
    }

    private static SyntaxTriviaList GetLeadingTriviaWithLineEnd()
    {
        List<SyntaxTrivia> Trivias = new()
        {
            SyntaxFactory.EndOfLine("\r\n"),
            SyntaxFactory.Whitespace(Tab),
        };

        return SyntaxFactory.TriviaList(Trivias);
    }

    private static SyntaxTriviaList GetLeadingTriviaWithoutLineEnd()
    {
        List<SyntaxTrivia> Trivias = new()
        {
            SyntaxFactory.Whitespace(Tab),
        };

        return SyntaxFactory.TriviaList(Trivias);
    }

    private static SyntaxTriviaList? GetModifiersTrailingTrivia(MethodDeclarationSyntax methodDeclaration)
    {
        return methodDeclaration.Modifiers.Count > 0 ? methodDeclaration.Modifiers.Last().TrailingTrivia : null;
    }

    private static BlockSyntax GenerateBody(ContractModel model, MethodDeclarationSyntax methodDeclaration, SyntaxTriviaList tabTrivia, SyntaxTriviaList tabTriviaWithoutLineEnd)
    {
        SyntaxToken OpenBraceToken = SyntaxFactory.Token(SyntaxKind.OpenBraceToken);
        OpenBraceToken = OpenBraceToken.WithLeadingTrivia(tabTriviaWithoutLineEnd);

        List<SyntaxTrivia> TrivialList = new(tabTrivia);
        TrivialList.Add(SyntaxFactory.Whitespace(Tab));
        SyntaxTriviaList TabStatementTrivia = SyntaxFactory.TriviaList(TrivialList);

        List<SyntaxTrivia> TrivialListExtraLineEnd = new(tabTrivia);
        TrivialListExtraLineEnd.Insert(0, SyntaxFactory.EndOfLine("\r\n"));
        TrivialListExtraLineEnd.Add(SyntaxFactory.Whitespace(Tab));
        SyntaxTriviaList TabStatementExtraLineEndTrivia = SyntaxFactory.TriviaList(TrivialListExtraLineEnd);

        SyntaxToken CloseBraceToken = SyntaxFactory.Token(SyntaxKind.CloseBraceToken);
        CloseBraceToken = CloseBraceToken.WithLeadingTrivia(tabTrivia);

        List<StatementSyntax> Statements = GenerateStatements(model, methodDeclaration, TabStatementTrivia, TabStatementExtraLineEndTrivia);

        return SyntaxFactory.Block(OpenBraceToken, SyntaxFactory.List(Statements), CloseBraceToken);
    }

    private static List<StatementSyntax> GenerateStatements(ContractModel model, MethodDeclarationSyntax methodDeclaration, SyntaxTriviaList tabStatementTrivia, SyntaxTriviaList tabStatementExtraLineEndTrivia)
    {
        Dictionary<string, string> ParameterNameReplacementTable = new();
        bool IsContainingRequire = false;

        foreach (AttributeModel Item in model.Attributes)
            if (Item.Name == nameof(RequireNotNullAttribute))
            {
                foreach (string Argument in Item.Arguments)
                    ParameterNameReplacementTable.Add(Argument, ToIdentifierLocalName(Argument));

                IsContainingRequire = true;
            }
            else if (Item.Name == nameof(RequireAttribute))
                IsContainingRequire = true;

        List<StatementSyntax> Statements = new();

        StatementSyntax CallStatement;
        StatementSyntax? ReturnStatement;

        if (methodDeclaration.ReturnType is PredefinedTypeSyntax PredefinedType && PredefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword))
        {
            CallStatement = GenerateCommandStatement(model.ShortMethodName, methodDeclaration.ParameterList, ParameterNameReplacementTable);
            ReturnStatement = null;
        }
        else
        {
            CallStatement = GenerateQueryStatement(model.ShortMethodName, methodDeclaration.ParameterList, ParameterNameReplacementTable);
            ReturnStatement = GenerateReturnStatement();
        }

        if (IsContainingRequire)
            CallStatement = CallStatement.WithLeadingTrivia(tabStatementExtraLineEndTrivia);
        else
            CallStatement = CallStatement.WithLeadingTrivia(tabStatementTrivia);

        int CallStatementIndex = -1;
        foreach (AttributeModel AttributeModel in model.Attributes)
            if (AttributeModel.Name != nameof(AccessAttribute))
            {
                bool FirstEnsure = false;
                if (CallStatementIndex < 0 && AttributeModel.Name == nameof(EnsureAttribute))
                {
                    CallStatementIndex = Statements.Count;
                    FirstEnsure = true;
                }

                List<StatementSyntax> AttributeStatements = GenerateAttributeStatements(AttributeModel, methodDeclaration);
                foreach (StatementSyntax Statement in AttributeStatements)
                {
                    if (FirstEnsure)
                    {
                        FirstEnsure = false;
                        Statements.Add(Statement.WithLeadingTrivia(tabStatementExtraLineEndTrivia));
                    }
                    else
                        Statements.Add(Statement.WithLeadingTrivia(tabStatementTrivia));
                }
            }

        if (CallStatementIndex < 0)
            CallStatementIndex = Statements.Count;

        Statements.Insert(CallStatementIndex, CallStatement);

        if (ReturnStatement is not null)
            Statements.Add(ReturnStatement.WithLeadingTrivia(tabStatementExtraLineEndTrivia));

        return Statements;
    }

    private static ExpressionStatementSyntax GenerateCommandStatement(string methodName, ParameterListSyntax parameterList, Dictionary<string, string> parameterNameReplacementTable)
    {
        SyntaxTriviaList WhitespaceTrivia = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
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
        ExpressionStatementSyntax ExpressionStatement = SyntaxFactory.ExpressionStatement(CallExpression);

        return ExpressionStatement;
    }

    private static LocalDeclarationStatementSyntax GenerateQueryStatement(string methodName, ParameterListSyntax parameterList, Dictionary<string, string> parameterNameReplacementTable)
    {
        SyntaxTriviaList WhitespaceTrivia = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
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

        IdentifierNameSyntax VarIdentifier = SyntaxFactory.IdentifierName("var");
        SyntaxToken ResultIdentifier = SyntaxFactory.Identifier(ResultIdentifierName);
        EqualsValueClauseSyntax Initializer = SyntaxFactory.EqualsValueClause(CallExpression).WithLeadingTrivia(WhitespaceTrivia);
        VariableDeclaratorSyntax VariableDeclarator = SyntaxFactory.VariableDeclarator(ResultIdentifier, null, Initializer).WithLeadingTrivia(WhitespaceTrivia);
        VariableDeclarationSyntax Declaration = SyntaxFactory.VariableDeclaration(VarIdentifier, SyntaxFactory.SeparatedList(new List<VariableDeclaratorSyntax>() { VariableDeclarator }));
        LocalDeclarationStatementSyntax LocalDeclarationStatement = SyntaxFactory.LocalDeclarationStatement(Declaration);

        return LocalDeclarationStatement;
    }

    private static ReturnStatementSyntax GenerateReturnStatement()
    {
        SyntaxTriviaList WhitespaceTrivia = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
        IdentifierNameSyntax ResultIdentifier = SyntaxFactory.IdentifierName(ResultIdentifierName).WithLeadingTrivia(WhitespaceTrivia);
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
        ExpressionSyntax ContractName = SyntaxFactory.IdentifierName(ContractNamespace);
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
        ExpressionSyntax ContractName = SyntaxFactory.IdentifierName(ContractNamespace);
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

    private static void OutputContractMethod(SourceProductionContext context, ContractModel model)
    {
        var sourceText = SourceText.From($$"""
                namespace {{model.Namespace}};

                using System;
                using System.CodeDom.Compiler;

                partial class {{model.ClassName}}
                {
                {{model.GeneratedMethodDeclaration}}
                }
                """,
            Encoding.UTF8);

        context.AddSource($"{model.ClassName}_{model.ShortMethodName}.g.cs", sourceText);
    }

    private record ContractModel(string Namespace, string ClassName, string ShortMethodName, List<AttributeModel> Attributes, string GeneratedMethodDeclaration);
    private record AttributeModel(string Name, List<string> Arguments);
}
