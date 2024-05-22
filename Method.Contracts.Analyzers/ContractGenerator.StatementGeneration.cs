namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Represents a code generator.
/// </summary>
public partial class ContractGenerator
{
    private static List<StatementSyntax> GenerateStatements(ContractModel model, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration, SyntaxTriviaList tabStatementTrivia, SyntaxTriviaList tabStatementExtraLineEndTrivia, bool isAsync)
    {
        List<StatementSyntax> Statements = new();

        GetParameterReplacementTable(model, isDebugGeneration, out Dictionary<string, string> AliasNameReplacementTable, out bool IsContainingRequire);
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
                AddAttributeStatements(methodDeclaration, isDebugGeneration, tabStatementTrivia, tabStatementExtraLineEndTrivia, Statements, AttributeModel, ref CallStatementIndex);

        if (CallStatementIndex < 0)
            CallStatementIndex = Statements.Count;

        Statements.Insert(CallStatementIndex, CallStatement);

        if (ReturnStatement is not null)
            Statements.Add(ReturnStatement.WithLeadingTrivia(tabStatementExtraLineEndTrivia));

        return Statements;
    }

    private static void GetParameterReplacementTable(ContractModel model, bool isDebugGeneration, out Dictionary<string, string> aliasNameReplacementTable, out bool isContainingRequire)
    {
        aliasNameReplacementTable = new();
        isContainingRequire = false;

        foreach (AttributeModel Item in model.Attributes)
        {
            List<AttributeArgumentModel> Arguments = Item.Arguments;

            if (Item.Name == nameof(RequireNotNullAttribute))
            {
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
            {
                if (Arguments.Count <= 1 || Arguments[1].Name == string.Empty || Arguments[1].Value == "false" || isDebugGeneration)
                    isContainingRequire = true;
            }
        }
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
        if (returnType is not PredefinedTypeSyntax PredefinedType)
            return false;

        if (!PredefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword))
            return false;

        return true;
    }

    private static void AddAttributeStatements(MethodDeclarationSyntax methodDeclaration,
                                               bool isDebugGeneration,
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

        List<StatementSyntax> AttributeStatements = GenerateAttributeStatements(attributeModel, methodDeclaration, isDebugGeneration);

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
        foreach (ParameterSyntax Parameter in parameterList.Parameters)
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
        foreach (ParameterSyntax Parameter in parameterList.Parameters)
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

    private static List<StatementSyntax> GenerateAttributeStatements(AttributeModel attributeModel, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration)
    {
        Dictionary<string, Func<List<AttributeArgumentModel>, MethodDeclarationSyntax, bool, List<StatementSyntax>>> GeneratorTable = new()
        {
            { nameof(RequireNotNullAttribute), GenerateRequireNotNullStatement },
            { nameof(RequireAttribute), GenerateRequireStatement },
            { nameof(EnsureAttribute), GenerateEnsureStatement },
        };

        Debug.Assert(GeneratorTable.ContainsKey(attributeModel.Name));
        return GeneratorTable[attributeModel.Name](attributeModel.Arguments, methodDeclaration, isDebugGeneration);
    }

    private static List<StatementSyntax> GenerateRequireNotNullStatement(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration)
    {
        if (attributeArguments.Count > 1 && attributeArguments.Any(argument => argument.Name != string.Empty))
            return GenerateRequireNotNullStatementWithAlias(attributeArguments, methodDeclaration);
        else
            return GenerateMultipleRequireNotNullStatement(attributeArguments, methodDeclaration);
    }

    private static List<StatementSyntax> GenerateRequireNotNullStatementWithAlias(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration)
    {
        Debug.Assert(attributeArguments.Count > 0);
        Debug.Assert(attributeArguments[0].Name == string.Empty);
        string ParameterName = attributeArguments[0].Value;

        bool IsParameterTypeValid = GetParameterType(ParameterName, methodDeclaration, out TypeSyntax Type);
        Debug.Assert(IsParameterTypeValid);

        GetModifiedIdentifiers(attributeArguments, ref ParameterName, out string AliasName);

        ExpressionStatementSyntax ExpressionStatement = GenerateOneRequireNotNullStatement(ParameterName, Type, AliasName);

        return new List<StatementSyntax>() { ExpressionStatement };
    }

    private static void GetModifiedIdentifiers(List<AttributeArgumentModel> attributeArguments, ref string parameterName, out string aliasName)
    {
        foreach (AttributeArgumentModel argument in attributeArguments)
            if (argument.Name == nameof(RequireNotNullAttribute.Name))
                parameterName = argument.Value;

        aliasName = ToIdentifierLocalName(parameterName);

        foreach (AttributeArgumentModel argument in attributeArguments)
            if (argument.Name == nameof(RequireNotNullAttribute.AliasName))
                aliasName = argument.Value;
    }

    private static List<StatementSyntax> GenerateMultipleRequireNotNullStatement(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration)
    {
        List<StatementSyntax> Statements = new();

        foreach (AttributeArgumentModel argument in attributeArguments)
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

    private static List<StatementSyntax> GenerateRequireStatement(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration)
    {
        return GenerateRequireOrEnsureStatement(attributeArguments, methodDeclaration, isDebugGeneration, "Require");
    }

    private static List<StatementSyntax> GenerateEnsureStatement(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration)
    {
        return GenerateRequireOrEnsureStatement(attributeArguments, methodDeclaration, isDebugGeneration, "Ensure");
    }

    private static List<StatementSyntax> GenerateRequireOrEnsureStatement(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration, string contractMethodName)
    {
        if (attributeArguments.Count > 1 && attributeArguments.Any(argument => argument.Name != string.Empty))
            return GenerateRequireOrEnsureStatementWithDebugOnly(attributeArguments, methodDeclaration, isDebugGeneration, contractMethodName);
        else
            return GenerateMultipleRequireOrEnsureStatement(attributeArguments, methodDeclaration, contractMethodName);
    }

    private static List<StatementSyntax> GenerateRequireOrEnsureStatementWithDebugOnly(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration, string contractMethodName)
    {
        Debug.Assert(attributeArguments.Count == 2, "This is the result of TransformRequireOrEnsureAttributeWithDebugOnly().");

        if (attributeArguments[1].Value == "false" || isDebugGeneration)
        {
            List<AttributeArgumentModel> SingleAttributeArgument = new() { attributeArguments[0] };
            return GenerateMultipleRequireOrEnsureStatement(SingleAttributeArgument, methodDeclaration, contractMethodName);
        }
        else
            return new List<StatementSyntax>();
    }

    private static List<StatementSyntax> GenerateMultipleRequireOrEnsureStatement(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration, string contractMethodName)
    {
        List<StatementSyntax> Statements = new();

        foreach (AttributeArgumentModel AttributeArgument in attributeArguments)
        {
            ExpressionSyntax ContractName = SyntaxFactory.IdentifierName(ContractClassName);
            SimpleNameSyntax ContractMethodSimpleName = SyntaxFactory.IdentifierName(contractMethodName);
            MemberAccessExpressionSyntax MemberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ContractName, ContractMethodSimpleName);

            IdentifierNameSyntax InputName = SyntaxFactory.IdentifierName(AttributeArgument.Value);
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
}
