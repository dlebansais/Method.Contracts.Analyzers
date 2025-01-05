namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
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
    private static List<StatementSyntax> GenerateStatements(ContractModel model, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration, SyntaxTriviaList tabStatementTrivia, SyntaxTriviaList tabStatementExtraLineEndTrivia)
    {
        List<StatementSyntax> Statements = [];

        GetParameterReplacementTable(model, isDebugGeneration, out Dictionary<string, string> AliasNameReplacementTable, out bool IsContainingRequire);
        GetCallAndReturnStatements(model,
                                   methodDeclaration,
                                   tabStatementTrivia,
                                   tabStatementExtraLineEndTrivia,
                                   AliasNameReplacementTable,
                                   IsContainingRequire,
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
        aliasNameReplacementTable = [];
        isContainingRequire = false;

        foreach (AttributeModel Item in model.Attributes)
        {
            List<AttributeArgumentModel> Arguments = Item.Arguments;

            if (Item.Name == nameof(RequireNotNullAttribute))
            {
                // Valid RequireNotNull attribute always has arguments.
                Contract.Assert(Arguments.Count > 0);

                if (Arguments.Any(argument => argument.Name != string.Empty))
                {
                    Contract.Assert(Arguments[0].Name == string.Empty);
                    string ParameterName = Arguments[0].Value;
                    string OriginalParameterName = ParameterName;

                    GetModifiedIdentifiers(Arguments, ref ParameterName, out string AliasName);

                    aliasNameReplacementTable.Add(OriginalParameterName, AliasName);
                }
                else
                {
                    foreach (AttributeArgumentModel Argument in Item.Arguments)
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
                                                   out StatementSyntax callStatement,
                                                   out StatementSyntax? returnStatement)
    {
        if (IsCommandMethod(methodDeclaration, model.IsAsync))
        {
            callStatement = GenerateCommandStatement(model.ShortName, methodDeclaration.ParameterList, aliasNameReplacementTable, model.IsAsync);
            returnStatement = null;
        }
        else
        {
            callStatement = GenerateMethodQueryStatement(model.ShortName, methodDeclaration.ParameterList, aliasNameReplacementTable, model.IsAsync);
            returnStatement = GenerateReturnStatement();
        }

        callStatement = isContainingRequire
            ? callStatement.WithLeadingTrivia(tabStatementExtraLineEndTrivia)
            : callStatement.WithLeadingTrivia(tabStatementTrivia);
    }

    private static bool IsCommandMethod(MethodDeclarationSyntax methodDeclaration, bool isAsync) => (isAsync && IsTaskType(methodDeclaration.ReturnType)) || (!isAsync && IsVoidType(methodDeclaration.ReturnType));

    private static bool IsTaskType(TypeSyntax returnType)
    {
        string? ReturnIdentifierWithNamespace = null;
        NameSyntax? Name = returnType as NameSyntax;

        while (Name is QualifiedNameSyntax QualifiedName)
        {
            ReturnIdentifierWithNamespace = ReturnIdentifierWithNamespace is null
                ? $"{QualifiedName.Right}"
                : $"{QualifiedName.Right}.{ReturnIdentifierWithNamespace}";

            Name = QualifiedName.Left;
        }

        if (Name is IdentifierNameSyntax IdentifierName)
        {
            ReturnIdentifierWithNamespace = ReturnIdentifierWithNamespace is null
                ? IdentifierName.Identifier.Text
                : $"{IdentifierName.Identifier.Text}.{ReturnIdentifierWithNamespace}";
        }

        return ReturnIdentifierWithNamespace is "Task" or "System.Threading.Tasks.Task";
    }

    private static bool IsVoidType(TypeSyntax returnType) => returnType is PredefinedTypeSyntax PredefinedType && PredefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword);

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

        List<StatementSyntax> AttributeStatements = GenerateMethodAttributeStatements(attributeModel, methodDeclaration, isDebugGeneration);

        foreach (StatementSyntax Statement in AttributeStatements)
            if (FirstEnsure)
            {
                FirstEnsure = false;
                statements.Add(Statement.WithLeadingTrivia(tabStatementExtraLineEndTrivia));
            }
            else
            {
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

        List<ArgumentSyntax> Arguments = [];
        foreach (ParameterSyntax Parameter in parameterList.Parameters)
        {
            bool IsRef = false;
            bool IsOut = false;

            foreach (SyntaxToken Modifier in Parameter.Modifiers)
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

            ArgumentSyntax Argument =
                IsRef
                ? SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.RefKeyword), ParameterIdentifier.WithLeadingTrivia(SyntaxFactory.Space))
                : IsOut
                  ? SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), ParameterIdentifier.WithLeadingTrivia(SyntaxFactory.Space))
                  : SyntaxFactory.Argument(ParameterIdentifier);

            if (Arguments.Count > 0)
                Argument = Argument.WithLeadingTrivia(SyntaxFactory.Space);

            Arguments.Add(Argument);
        }

        ArgumentListSyntax ArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(Arguments));
        ExpressionSyntax CallExpression = SyntaxFactory.InvocationExpression(Invocation, ArgumentList);

        if (isAsync)
            CallExpression = SyntaxFactory.AwaitExpression(CallExpression.WithLeadingTrivia(SyntaxFactory.Space));

        ExpressionStatementSyntax ExpressionStatement = SyntaxFactory.ExpressionStatement(CallExpression);

        return ExpressionStatement;
    }

    private static LocalDeclarationStatementSyntax GenerateMethodQueryStatement(string methodName,
                                                                                ParameterListSyntax parameterList,
                                                                                Dictionary<string, string> aliasNameReplacementTable,
                                                                                bool isAsync)
    {
        string VerifiedSuffix = Settings.VerifiedSuffix;
        ExpressionSyntax Invocation = SyntaxFactory.IdentifierName(methodName + VerifiedSuffix);

        List<ArgumentSyntax> Arguments = [];
        foreach (ParameterSyntax Parameter in parameterList.Parameters)
        {
            bool IsRef = false;
            bool IsOut = false;

            foreach (SyntaxToken Modifier in Parameter.Modifiers)
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

            ArgumentSyntax Argument =
                IsRef
                ? SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.RefKeyword), ParameterIdentifier.WithLeadingTrivia(SyntaxFactory.Space))
                : IsOut
                  ? SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), ParameterIdentifier.WithLeadingTrivia(SyntaxFactory.Space))
                  : SyntaxFactory.Argument(ParameterIdentifier);

            if (Arguments.Count > 0)
                Argument = Argument.WithLeadingTrivia(SyntaxFactory.Space);

            Arguments.Add(Argument);
        }

        ArgumentListSyntax ArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(Arguments));
        ExpressionSyntax CallExpression = SyntaxFactory.InvocationExpression(Invocation, ArgumentList).WithLeadingTrivia(SyntaxFactory.Space);

        if (isAsync)
            CallExpression = SyntaxFactory.AwaitExpression(CallExpression).WithLeadingTrivia(SyntaxFactory.Space);

        IdentifierNameSyntax VarIdentifier = SyntaxFactory.IdentifierName("var");
        SyntaxToken ResultIdentifier = SyntaxFactory.Identifier(Settings.ResultIdentifier);
        EqualsValueClauseSyntax Initializer = SyntaxFactory.EqualsValueClause(CallExpression).WithLeadingTrivia(SyntaxFactory.Space);
        VariableDeclaratorSyntax VariableDeclarator = SyntaxFactory.VariableDeclarator(ResultIdentifier, null, Initializer).WithLeadingTrivia(SyntaxFactory.Space);
        VariableDeclarationSyntax Declaration = SyntaxFactory.VariableDeclaration(VarIdentifier, SyntaxFactory.SeparatedList([VariableDeclarator]));
        LocalDeclarationStatementSyntax LocalDeclarationStatement = SyntaxFactory.LocalDeclarationStatement(Declaration);

        return LocalDeclarationStatement;
    }

    private static ReturnStatementSyntax GenerateReturnStatement()
    {
        IdentifierNameSyntax ResultIdentifier = SyntaxFactory.IdentifierName(Settings.ResultIdentifier).WithLeadingTrivia(SyntaxFactory.Space);
        ReturnStatementSyntax ReturnStatement = SyntaxFactory.ReturnStatement(ResultIdentifier);

        return ReturnStatement;
    }

    private static List<StatementSyntax> GenerateMethodAttributeStatements(AttributeModel attributeModel, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration)
    {
        Dictionary<string, Func<List<AttributeArgumentModel>, MethodDeclarationSyntax, bool, List<StatementSyntax>>> GeneratorTable = new()
        {
            { nameof(RequireNotNullAttribute), GenerateRequireNotNullStatement },
            { nameof(RequireAttribute), GenerateRequireStatement },
            { nameof(EnsureAttribute), GenerateEnsureStatement },
        };

        Contract.Assert(GeneratorTable.ContainsKey(attributeModel.Name));
        return GeneratorTable[attributeModel.Name](attributeModel.Arguments, methodDeclaration, isDebugGeneration);
    }

    private static List<StatementSyntax> GenerateRequireNotNullStatement(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration)
    {
        return attributeArguments.Any(argument => argument.Name != string.Empty)
            ? GenerateRequireNotNullStatementWithAlias(attributeArguments, methodDeclaration)
            : GenerateMultipleRequireNotNullStatement(attributeArguments, methodDeclaration);
    }

    private static List<StatementSyntax> GenerateRequireNotNullStatementWithAlias(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration)
    {
        Contract.Assert(attributeArguments.Count > 0);
        Contract.Assert(attributeArguments[0].Name == string.Empty);
        string ParameterName = attributeArguments[0].Value;

        bool IsParameterTypeValid = GetParameterType(ParameterName, methodDeclaration, out TypeSyntax Type);
        Contract.Assert(IsParameterTypeValid);

        GetModifiedIdentifiers(attributeArguments, ref ParameterName, out string AliasName);

        ExpressionStatementSyntax ExpressionStatement = GenerateOneRequireNotNullStatement(ParameterName, Type, AliasName);

        return [ExpressionStatement];
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
        List<StatementSyntax> Statements = [];

        foreach (AttributeArgumentModel argument in attributeArguments)
        {
            string ParameterName = argument.Value;
            string AliasName = ToIdentifierLocalName(ParameterName);

            bool IsParameterTypeValid = GetParameterType(ParameterName, methodDeclaration, out TypeSyntax Type);
            Contract.Assert(IsParameterTypeValid);

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

        IdentifierNameSyntax InputName = SyntaxFactory.IdentifierName(parameterName);
        ArgumentSyntax InputArgument = SyntaxFactory.Argument(InputName);

        SyntaxToken VariableName = SyntaxFactory.Identifier(aliasName);
        VariableDesignationSyntax VariableDesignation = SyntaxFactory.SingleVariableDesignation(VariableName);
        DeclarationExpressionSyntax DeclarationExpression = SyntaxFactory.DeclarationExpression(type, VariableDesignation.WithLeadingTrivia(SyntaxFactory.Space));
        ArgumentSyntax OutputArgument = SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), DeclarationExpression.WithLeadingTrivia(SyntaxFactory.Space));
        OutputArgument = OutputArgument.WithLeadingTrivia(SyntaxFactory.Space);

        List<ArgumentSyntax> Arguments = [InputArgument, OutputArgument];
        ArgumentListSyntax ArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(Arguments));

        ExpressionSyntax CallExpression = SyntaxFactory.InvocationExpression(MemberAccessExpression, ArgumentList);
        ExpressionStatementSyntax ExpressionStatement = SyntaxFactory.ExpressionStatement(CallExpression);

        return ExpressionStatement;
    }

    private static List<StatementSyntax> GenerateRequireStatement(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration) => GenerateRequireStatement(attributeArguments, isDebugGeneration);

    private static List<StatementSyntax> GenerateRequireStatement(List<AttributeArgumentModel> attributeArguments, bool isDebugGeneration) => GenerateRequireOrEnsureStatement(attributeArguments, isDebugGeneration, "Require");

    private static List<StatementSyntax> GenerateEnsureStatement(List<AttributeArgumentModel> attributeArguments, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration) => GenerateEnsureStatement(attributeArguments, isDebugGeneration);

    private static List<StatementSyntax> GenerateEnsureStatement(List<AttributeArgumentModel> attributeArguments, bool isDebugGeneration) => GenerateRequireOrEnsureStatement(attributeArguments, isDebugGeneration, "Ensure");

    private static List<StatementSyntax> GenerateRequireOrEnsureStatement(List<AttributeArgumentModel> attributeArguments, bool isDebugGeneration, string contractMethodName)
    {
        return attributeArguments.Any(argument => argument.Name != string.Empty)
            ? GenerateRequireOrEnsureStatementWithDebugOnly(attributeArguments, isDebugGeneration, contractMethodName)
            : GenerateMultipleRequireOrEnsureStatement(attributeArguments, contractMethodName);
    }

    private static List<StatementSyntax> GenerateRequireOrEnsureStatementWithDebugOnly(List<AttributeArgumentModel> attributeArguments, bool isDebugGeneration, string contractMethodName)
    {
        // This is the result of TransformRequireOrEnsureAttributeWithDebugOnly().
        Contract.Assert(attributeArguments.Count == 2);

        if (attributeArguments[1].Value == "false" || isDebugGeneration)
        {
            List<AttributeArgumentModel> SingleAttributeArgument = [attributeArguments[0]];
            return GenerateMultipleRequireOrEnsureStatement(SingleAttributeArgument, contractMethodName);
        }
        else
        {
            return [];
        }
    }

    private static List<StatementSyntax> GenerateMultipleRequireOrEnsureStatement(List<AttributeArgumentModel> attributeArguments, string contractMethodName)
    {
        List<StatementSyntax> Statements = [];

        foreach (AttributeArgumentModel AttributeArgument in attributeArguments)
        {
            ExpressionSyntax ContractName = SyntaxFactory.IdentifierName(ContractClassName);
            SimpleNameSyntax ContractMethodSimpleName = SyntaxFactory.IdentifierName(contractMethodName);
            MemberAccessExpressionSyntax MemberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ContractName, ContractMethodSimpleName);

            IdentifierNameSyntax InputName = SyntaxFactory.IdentifierName(AttributeArgument.Value);
            ArgumentSyntax InputArgument = SyntaxFactory.Argument(InputName);
            List<ArgumentSyntax> Arguments = [InputArgument];
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
        return LongName[..^nameof(Attribute).Length];
    }

    private static string ToIdentifierLocalName(string text)
    {
        Contract.Assert(text.Length > 0);

        char FirstLetter = text[0];
        string OtherLetters = text[1..];

        return char.IsLower(FirstLetter) ? $"{char.ToUpper(FirstLetter, CultureInfo.InvariantCulture)}{OtherLetters}" : $"_{text}";
    }
}
