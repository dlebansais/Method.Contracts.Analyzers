namespace Contracts.Analyzers;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Represents a code generator.
/// </summary>
public partial class ContractGenerator
{
    private static string GetGeneratedMethodDeclaration(ContractModel model, GeneratorAttributeSyntaxContext context, out bool isAsync)
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

        SyntaxTokenList Modifiers = GenerateContractModifiers(model, MethodDeclaration, LeadingTrivia, TrailingTrivia, out isAsync);
        MethodDeclaration = MethodDeclaration.WithModifiers(Modifiers);

        BlockSyntax MethodBody = GenerateBody(model, MethodDeclaration, LeadingTrivia, LeadingTriviaWithoutLineEnd, isAsync, Tab);
        MethodDeclaration = MethodDeclaration.WithBody(MethodBody);

        if (HasUpdatedParameterList(model, MethodDeclaration, out ParameterListSyntax ParameterList))
            MethodDeclaration = MethodDeclaration.WithParameterList(ParameterList);

        if (isAsync && IsTaskType(MethodDeclaration.ReturnType))
            MethodDeclaration = MethodDeclaration.WithReturnType(SyntaxFactory.IdentifierName("Task").WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "))));

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
            AttributeArgumentModel ArgumentModel = accessAttributeModel.Arguments[i];
            string ArgumentValue = ArgumentModel.Value;
            SyntaxToken ModifierToken = SyntaxFactory.Identifier(ArgumentValue);

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

            if (ArgumentValue is "async")
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

    private static bool HasUpdatedParameterList(ContractModel model, MethodDeclarationSyntax methodDeclaration, out ParameterListSyntax updatedParameterList)
    {
        ParameterListSyntax ParameterList = methodDeclaration.ParameterList;
        updatedParameterList = ParameterList;

        SeparatedSyntaxList<ParameterSyntax> Parameters = ParameterList.Parameters;
        SeparatedSyntaxList<ParameterSyntax> UpdatedParameters = Parameters;

        foreach (var Parameter in UpdatedParameters)
            if (ModifiedParameterTypeOrName(model, Parameter, out ParameterSyntax UpdatedParameter))
            {
                UpdatedParameters = UpdatedParameters.Replace(Parameter, UpdatedParameter);
                updatedParameterList = updatedParameterList.WithParameters(UpdatedParameters);
            }

        return updatedParameterList != ParameterList;
    }

    private static bool ModifiedParameterTypeOrName(ContractModel model, ParameterSyntax parameter, out ParameterSyntax updatedParameter)
    {
        updatedParameter = parameter;

        foreach (AttributeModel Attribute in model.Attributes)
            if (AttributeHasTypeOrName(Attribute, out string ParameterName, out string Type, out string Name) && ParameterName == parameter.Identifier.Text)
            {
                if (Type != string.Empty)
                {
                    TypeSyntax UpatedType = SyntaxFactory.IdentifierName(Type).WithTrailingTrivia(WhitespaceTrivia);
                    updatedParameter = updatedParameter.WithType(UpatedType);
                }

                if (Name != string.Empty)
                {
                    SyntaxToken UpdatedIdentifier = SyntaxFactory.Identifier(Name);
                    updatedParameter = updatedParameter.WithIdentifier(UpdatedIdentifier);
                }
            }

        return updatedParameter != parameter;
    }

    private static bool AttributeHasTypeOrName(AttributeModel attribute, out string parameterName, out string type, out string name)
    {
        parameterName = string.Empty;
        type = string.Empty;
        name = string.Empty;

        foreach (AttributeArgumentModel AttributeArgument in attribute.Arguments)
        {
            if (AttributeArgument.Name == string.Empty)
                parameterName = AttributeArgument.Value;
            if (AttributeArgument.Name == nameof(RequireNotNullAttribute.Type))
                type = AttributeArgument.Value;
            if (AttributeArgument.Name == nameof(RequireNotNullAttribute.Name))
                name = AttributeArgument.Value;
        }

        Debug.Assert(parameterName != string.Empty, "Valid attribute for RequireNotNull always have a parameter name.");

        return type != string.Empty || name != string.Empty;
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

        GetParameterReplacementTable(model, out Dictionary<string, string> AliasNameReplacementTable, out bool IsContainingRequire);
        GetCallAndReturnStatements(model,
                                   methodDeclaration,
                                   tabStatementTrivia,
                                   tabStatementExtraLineEndTrivia,
                                   AliasNameReplacementTable,
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

    private static void GetParameterReplacementTable(ContractModel model, out Dictionary<string, string> aliasNameReplacementTable, out bool isContainingRequire)
    {
        aliasNameReplacementTable = new();
        isContainingRequire = false;

        foreach (AttributeModel Item in model.Attributes)
            if (Item.Name == nameof(RequireNotNullAttribute))
            {
                List<AttributeArgumentModel> Arguments = Item.Arguments;

                if (Arguments.Count > 0 && Arguments.Any(argument => argument.Name != string.Empty))
                {
                    Debug.Assert(Arguments[0].Name == string.Empty);
                    string ParameterName = Arguments[0].Value;
                    string OriginalParameterName = ParameterName;

                    GetModifiedIdentifiers(Arguments, ref ParameterName, out string AliasName);

                    aliasNameReplacementTable.Add(OriginalParameterName, AliasName);
                }
                else
                {
                    foreach (var Argument in Item.Arguments)
                    {
                        string ParameterName = Argument.Value;
                        aliasNameReplacementTable.Add(ParameterName, ToIdentifierLocalName(ParameterName));
                    }
                }

                isContainingRequire = true;
            }
            else if (Item.Name == nameof(RequireAttribute))
                isContainingRequire = true;
    }

    private static void GetCallAndReturnStatements(ContractModel model,
                                                   MethodDeclarationSyntax methodDeclaration,
                                                   SyntaxTriviaList tabStatementTrivia,
                                                   SyntaxTriviaList tabStatementExtraLineEndTrivia,
                                                   Dictionary<string, string> aliasNameReplacementTable,
                                                   bool isContainingRequire,
                                                   bool isAsync,
                                                   out StatementSyntax callStatement,
                                                   out StatementSyntax? returnStatement)
    {
        if (IsCommandMethod(methodDeclaration, isAsync))
        {
            callStatement = GenerateCommandStatement(model.ShortMethodName, methodDeclaration.ParameterList, aliasNameReplacementTable, isAsync);
            returnStatement = null;
        }
        else
        {
            callStatement = GenerateQueryStatement(model.ShortMethodName, methodDeclaration.ParameterList, aliasNameReplacementTable, isAsync);
            returnStatement = GenerateReturnStatement();
        }

        if (isContainingRequire)
            callStatement = callStatement.WithLeadingTrivia(tabStatementExtraLineEndTrivia);
        else
            callStatement = callStatement.WithLeadingTrivia(tabStatementTrivia);
    }

    private static bool IsCommandMethod(MethodDeclarationSyntax methodDeclaration, bool isAsync)
    {
        return (isAsync && IsTaskType(methodDeclaration.ReturnType)) || (!isAsync && IsVoidType(methodDeclaration.ReturnType));
    }

    private static bool IsTaskType(TypeSyntax returnType)
    {
        string? ReturnIdentifierWithNamespace = null;
        NameSyntax? Name = returnType as NameSyntax;

        while (Name is QualifiedNameSyntax QualifiedName)
        {
            if (ReturnIdentifierWithNamespace is null)
                ReturnIdentifierWithNamespace = $"{QualifiedName.Right}";
            else
                ReturnIdentifierWithNamespace = $"{QualifiedName.Right}.{ReturnIdentifierWithNamespace}";

            Name = QualifiedName.Left;
        }

        if (Name is IdentifierNameSyntax IdentifierName)
        {
            if (ReturnIdentifierWithNamespace is null)
                ReturnIdentifierWithNamespace = IdentifierName.Identifier.Text;
            else
                ReturnIdentifierWithNamespace = $"{IdentifierName.Identifier.Text}.{ReturnIdentifierWithNamespace}";
        }

        return ReturnIdentifierWithNamespace is "Task" or "System.Threading.Tasks.Task";
    }

    private static bool IsVoidType(TypeSyntax returnType)
    {
        return returnType is PredefinedTypeSyntax PredefinedType && PredefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword);
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

    private static ExpressionStatementSyntax GenerateCommandStatement(string methodName,
                                                                      ParameterListSyntax parameterList,
                                                                      Dictionary<string, string> aliasNameReplacementTable,
                                                                      bool isAsync)
    {
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
                if (aliasNameReplacementTable.TryGetValue(ParameterName, out string ReplacedParameterName))
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

    private static LocalDeclarationStatementSyntax GenerateQueryStatement(string methodName,
                                                                          ParameterListSyntax parameterList,
                                                                          Dictionary<string, string> aliasNameReplacementTable,
                                                                          bool isAsync)
    {
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
                if (aliasNameReplacementTable.TryGetValue(ParameterName, out string ReplacedParameterName))
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
        IdentifierNameSyntax ResultIdentifier = SyntaxFactory.IdentifierName(Settings.ResultIdentifier).WithLeadingTrivia(WhitespaceTrivia);
        ReturnStatementSyntax ReturnStatement = SyntaxFactory.ReturnStatement(ResultIdentifier);

        return ReturnStatement;
    }

    private static List<StatementSyntax> GenerateAttributeStatements(AttributeModel attributeModel, MethodDeclarationSyntax methodDeclaration)
    {
        Dictionary<string, Func<List<AttributeArgumentModel>, MethodDeclarationSyntax, List<StatementSyntax>>> GeneratorTable = new()
        {
            { nameof(RequireNotNullAttribute), GenerateRequireNotNullStatement },
            { nameof(RequireAttribute), GenerateRequireStatement },
            { nameof(EnsureAttribute), GenerateEnsureStatement },
        };

        Debug.Assert(GeneratorTable.ContainsKey(attributeModel.Name));
        return GeneratorTable[attributeModel.Name](attributeModel.Arguments, methodDeclaration);
    }

    private static List<StatementSyntax> GenerateRequireNotNullStatement(List<AttributeArgumentModel> arguments, MethodDeclarationSyntax methodDeclaration)
    {
        if (arguments.Count > 1 && arguments.Any(argument => argument.Name != string.Empty))
            return GenerateRequireNotNullStatementWithAlias(arguments, methodDeclaration);
        else
            return GenerateMultipleRequireNotNullStatement(arguments, methodDeclaration);
    }

    private static List<StatementSyntax> GenerateRequireNotNullStatementWithAlias(List<AttributeArgumentModel> arguments, MethodDeclarationSyntax methodDeclaration)
    {
        Debug.Assert(arguments.Count > 0);
        Debug.Assert(arguments[0].Name == string.Empty);
        string ParameterName = arguments[0].Value;

        bool IsParameterTypeValid = GetParameterType(ParameterName, methodDeclaration, out TypeSyntax Type);
        Debug.Assert(IsParameterTypeValid);

        GetModifiedIdentifiers(arguments, ref ParameterName, out string AliasName);

        ExpressionStatementSyntax ExpressionStatement = GenerateOneRequireNotNullStatement(ParameterName, Type, AliasName);

        return new List<StatementSyntax>() { ExpressionStatement };
    }

    private static void GetModifiedIdentifiers(List<AttributeArgumentModel> arguments, ref string parameterName, out string aliasName)
    {
        foreach (AttributeArgumentModel argument in arguments)
            if (argument.Name == nameof(RequireNotNullAttribute.Name))
                parameterName = argument.Value;

        aliasName = ToIdentifierLocalName(parameterName);

        foreach (AttributeArgumentModel argument in arguments)
            if (argument.Name == nameof(RequireNotNullAttribute.AliasName))
                aliasName = argument.Value;
    }

    private static List<StatementSyntax> GenerateMultipleRequireNotNullStatement(List<AttributeArgumentModel> arguments, MethodDeclarationSyntax methodDeclaration)
    {
        List<StatementSyntax> Statements = new();

        foreach (AttributeArgumentModel argument in arguments)
        {
            string ParameterName = argument.Value;
            string AliasName = ToIdentifierLocalName(ParameterName);

            bool IsParameterTypeValid = GetParameterType(ParameterName, methodDeclaration, out TypeSyntax Type);
            Debug.Assert(IsParameterTypeValid);

            ExpressionStatementSyntax ExpressionStatement = GenerateOneRequireNotNullStatement(ParameterName, Type, AliasName);
            Statements.Add(ExpressionStatement);
        }

        return Statements;
    }

    private static ExpressionStatementSyntax GenerateOneRequireNotNullStatement(string parameterName, TypeSyntax type, string aliasName)
    {
        ExpressionSyntax ContractName = SyntaxFactory.IdentifierName(ContractClassName);
        SimpleNameSyntax RequireNotNullName = SyntaxFactory.IdentifierName(ToNameWithoutAttribute<RequireNotNullAttribute>());
        MemberAccessExpressionSyntax MemberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ContractName, RequireNotNullName);

        SyntaxTriviaList WhitespaceTrivia = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
        IdentifierNameSyntax InputName = SyntaxFactory.IdentifierName(parameterName);
        ArgumentSyntax InputArgument = SyntaxFactory.Argument(InputName);

        SyntaxToken VariableName = SyntaxFactory.Identifier(aliasName);
        VariableDesignationSyntax VariableDesignation = SyntaxFactory.SingleVariableDesignation(VariableName);
        DeclarationExpressionSyntax DeclarationExpression = SyntaxFactory.DeclarationExpression(type, VariableDesignation.WithLeadingTrivia(WhitespaceTrivia));
        ArgumentSyntax OutputArgument = SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), DeclarationExpression.WithLeadingTrivia(WhitespaceTrivia));
        OutputArgument = OutputArgument.WithLeadingTrivia(WhitespaceTrivia);

        List<ArgumentSyntax> Arguments = new() { InputArgument, OutputArgument };
        ArgumentListSyntax ArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(Arguments));

        ExpressionSyntax CallExpression = SyntaxFactory.InvocationExpression(MemberAccessExpression, ArgumentList);
        ExpressionStatementSyntax ExpressionStatement = SyntaxFactory.ExpressionStatement(CallExpression);

        return ExpressionStatement;
    }

    private static List<StatementSyntax> GenerateRequireStatement(List<AttributeArgumentModel> arguments, MethodDeclarationSyntax methodDeclaration)
    {
        return GenerateRequireOrEnsureStatement(arguments, methodDeclaration, "Require");
    }

    private static List<StatementSyntax> GenerateEnsureStatement(List<AttributeArgumentModel> arguments, MethodDeclarationSyntax methodDeclaration)
    {
        return GenerateRequireOrEnsureStatement(arguments, methodDeclaration, "Ensure");
    }

    private static List<StatementSyntax> GenerateRequireOrEnsureStatement(List<AttributeArgumentModel> arguments, MethodDeclarationSyntax methodDeclaration, string contractMethodName)
    {
        List<StatementSyntax> Statements = new();

        foreach (AttributeArgumentModel argument in arguments)
        {
            ExpressionSyntax ContractName = SyntaxFactory.IdentifierName(ContractClassName);
            SimpleNameSyntax ContractMethodSimpleName = SyntaxFactory.IdentifierName(contractMethodName);
            MemberAccessExpressionSyntax MemberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ContractName, ContractMethodSimpleName);

            IdentifierNameSyntax InputName = SyntaxFactory.IdentifierName(argument.Value);
            ArgumentSyntax InputArgument = SyntaxFactory.Argument(InputName);
            List<ArgumentSyntax> Arguments = new() { InputArgument };
            ArgumentListSyntax ArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(Arguments));
            ExpressionSyntax CallExpression = SyntaxFactory.InvocationExpression(MemberAccessExpression, ArgumentList);
            ExpressionStatementSyntax ExpressionStatement = SyntaxFactory.ExpressionStatement(CallExpression);

            Statements.Add(ExpressionStatement);
        }

        return Statements;
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

    private static SyntaxTriviaList WhitespaceTrivia { get; } = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
}
