namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Represents a code generator.
/// </summary>
public partial class ContractGenerator
{
    private static void UpdateWithGeneratedPropertyDeclaration(GeneratorAttributeSyntaxContext context, ref ContractModel model)
    {
        SyntaxNode TargetNode = context.TargetNode;
        if (TargetNode is not PropertyDeclarationSyntax PropertyDeclaration)
            return;

        bool IsDebugGeneration = PropertyDeclaration.SyntaxTree.Options.PreprocessorSymbolNames.Contains("DEBUG");
        bool IsNotNullPropertyType = CheckNotNullPropertyType(model, context, PropertyDeclaration);

        string Tab = new(' ', Math.Max(Settings.TabLength, 1));
        SyntaxTriviaList LeadingTrivia = GetLeadingTriviaWithLineEnd(Tab);
        SyntaxTriviaList LeadingTriviaWithoutLineEnd = GetLeadingTriviaWithoutLineEnd(Tab);
        SyntaxTriviaList? TrailingTrivia = GetModifiersTrailingTrivia(PropertyDeclaration);
        bool SimplifyReturnTypeLeadingTrivia = PropertyDeclaration.Modifiers.Count == 0;

        SyntaxList<AttributeListSyntax> CodeAttributes = GenerateCodeAttributes();
        PropertyDeclaration = PropertyDeclaration.WithAttributeLists(CodeAttributes);

        SyntaxToken ShortIdentifier = SyntaxFactory.Identifier(model.ShortName);
        PropertyDeclaration = PropertyDeclaration.WithIdentifier(ShortIdentifier);

        SyntaxTokenList Modifiers = GenerateContractModifiers(ref model, PropertyDeclaration, LeadingTrivia, TrailingTrivia);
        PropertyDeclaration = PropertyDeclaration.WithModifiers(Modifiers);

        AccessorListSyntax PropertyAccessorList = GenerateAccessorList(model, IsDebugGeneration, IsNotNullPropertyType, LeadingTrivia, LeadingTriviaWithoutLineEnd, Tab);
        PropertyDeclaration = PropertyDeclaration.WithAccessorList(PropertyAccessorList);

        if (SimplifyReturnTypeLeadingTrivia) // This case applies to properties with zero modifier that become public.
            PropertyDeclaration = PropertyDeclaration.WithType(PropertyDeclaration.Type.WithLeadingTrivia(SyntaxFactory.Space));

        PropertyDeclaration = PropertyDeclaration.WithLeadingTrivia(LeadingTriviaWithoutLineEnd);

        model = model with { GeneratedPropertyDeclaration = PropertyDeclaration.ToFullString() };
    }

    private static bool CheckNotNullPropertyType(ContractModel model, GeneratorAttributeSyntaxContext context, PropertyDeclarationSyntax propertyDeclaration)
    {
        // Ignore private properties, the author that want null checking can make it explicit.
        if (IsPropertyPrivate(model))
            return false;

        Location ReturnLocation = propertyDeclaration.Type.GetLocation();
        int ReturnPosition = ReturnLocation.SourceSpan.Start;
        NullableContext NullableContext = context.SemanticModel.GetNullableContext(ReturnPosition);

        bool IsAnnotationUsed = false;
        if (NullableContext.HasFlag(NullableContext.AnnotationsEnabled))
            IsAnnotationUsed = true;
        if (NullableContext.HasFlag(NullableContext.AnnotationsContextInherited))
            IsAnnotationUsed = true;

        // If nullable is not enabled, null is always a possible value.
        if (!IsAnnotationUsed)
            return false;

        // If the type is not a reference type, value cannot be null. If we don't known, play safe and don't check for null.
        TypeInfo PropertyTypeInfo = context.SemanticModel.GetTypeInfo(propertyDeclaration.Type);

        bool IsReferenceType = false;
        if (PropertyTypeInfo.Type is IArrayTypeSymbol)
            IsReferenceType = true;
        if (PropertyTypeInfo.Type is INamedTypeSymbol PropertyTypeSymbol && !PropertyTypeSymbol.IsValueType)
            IsReferenceType = true;

        if (!IsReferenceType)
            return false;

        // If the type is directly nullable, null is always a possible value.
        bool IsAnnotated = propertyDeclaration.Type is NullableTypeSyntax;
        return !IsAnnotated;
    }

    private static bool IsPropertyPrivate(ContractModel model)
    {
        // No 'Access' attribute means the attribute default, which is public.
        if (model.Attributes.FirstOrDefault(attributeModel => attributeModel.Name == nameof(AccessAttribute)) is not AttributeModel AttributeModel)
            return false;

        foreach (AttributeArgumentModel ArgumentModel in AttributeModel.Arguments)
            if (ArgumentModel.Value is "public" or "protected" or "internal")
                return false;

        // No access modifier means the C# default, which is private.
        return true;
    }

    private static AccessorListSyntax GenerateAccessorList(ContractModel model, bool isDebugGeneration, bool isNotNullPropertyType, SyntaxTriviaList tabTrivia, SyntaxTriviaList tabTriviaWithoutLineEnd, string tab)
    {
        Debug.Assert(tabTriviaWithoutLineEnd.Count > 0);

        SyntaxToken OpenBraceToken = SyntaxFactory.Token(SyntaxKind.OpenBraceToken);
        OpenBraceToken = OpenBraceToken.WithLeadingTrivia(tabTrivia);

        List<SyntaxTrivia> TrivialList = [.. tabTrivia, SyntaxFactory.Whitespace(tab)];
        SyntaxTriviaList TabAccessorsTrivia = SyntaxFactory.TriviaList(TrivialList);

        List<SyntaxTrivia> TrivialListExtraLineEnd = new(TrivialList);
        TrivialListExtraLineEnd.Insert(0, SyntaxFactory.EndOfLine("\n"));
        TrivialListExtraLineEnd.Add(SyntaxFactory.Whitespace(tab));
        SyntaxTriviaList TabStatementExtraLineEndTrivia = SyntaxFactory.TriviaList(TrivialListExtraLineEnd);

        SyntaxToken CloseBraceToken = SyntaxFactory.Token(SyntaxKind.CloseBraceToken);
        CloseBraceToken = CloseBraceToken.WithLeadingTrivia(tabTrivia);

        List<SyntaxTrivia> AccessorsTrivialList = [.. TrivialList, SyntaxFactory.Whitespace(tab)];
        SyntaxTriviaList TabStatementTrivia = SyntaxFactory.TriviaList(AccessorsTrivialList);

        SyntaxToken AccessorOpenBraceToken = SyntaxFactory.Token(SyntaxKind.OpenBraceToken);
        AccessorOpenBraceToken = AccessorOpenBraceToken.WithLeadingTrivia(TabAccessorsTrivia);

        SyntaxToken AccessorCloseBraceToken = SyntaxFactory.Token(SyntaxKind.CloseBraceToken);
        AccessorCloseBraceToken = AccessorCloseBraceToken.WithLeadingTrivia(TabAccessorsTrivia);

        bool HasEnsure = model.Attributes.Any(attributeModel => attributeModel.Name == nameof(EnsureAttribute));
        bool HasRequire = model.Attributes.Any(attributeModel => attributeModel.Name == nameof(RequireAttribute));
        AccessorDeclarationSyntax Getter;
        AccessorDeclarationSyntax Setter;

        if (HasEnsure)
        {
            List<StatementSyntax> GetterStatements = GeneratePropertyStatements(model, isDebugGeneration, false, isGetter: true, TabStatementTrivia, TabStatementExtraLineEndTrivia);
            BlockSyntax GetterBody = SyntaxFactory.Block(AccessorOpenBraceToken, SyntaxFactory.List(GetterStatements), AccessorCloseBraceToken);
            Getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, GetterBody);
        }
        else
        {
            string VerifiedSuffix = Settings.VerifiedSuffix;
            ExpressionSyntax Invocation = SyntaxFactory.IdentifierName(model.ShortName + VerifiedSuffix).WithLeadingTrivia(SyntaxFactory.Space);
            ArrowExpressionClauseSyntax ExpressionBody = SyntaxFactory.ArrowExpressionClause(Invocation).WithLeadingTrivia(SyntaxFactory.Space);
            Getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithExpressionBody(ExpressionBody).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        if (HasRequire || isNotNullPropertyType)
        {
            List<StatementSyntax> SetterStatements = GeneratePropertyStatements(model, isDebugGeneration, isNotNullPropertyType, isGetter: false, TabStatementTrivia, TabStatementExtraLineEndTrivia);
            BlockSyntax SetterBody = SyntaxFactory.Block(AccessorOpenBraceToken, SyntaxFactory.List(SetterStatements), AccessorCloseBraceToken);
            Setter = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, SetterBody);
        }
        else
        {
            AssignmentExpressionSyntax AssignmentExpression = GeneratePropertyAssignment(model.ShortName, "value");
            ArrowExpressionClauseSyntax ExpressionBody = SyntaxFactory.ArrowExpressionClause(AssignmentExpression).WithLeadingTrivia(SyntaxFactory.Space);
            Setter = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithExpressionBody(ExpressionBody).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        Getter = Getter.WithLeadingTrivia(TabAccessorsTrivia);
        Setter = Setter.WithLeadingTrivia(TabAccessorsTrivia);

        return SyntaxFactory.AccessorList(OpenBraceToken, [Getter, Setter], CloseBraceToken);
    }

    private static List<StatementSyntax> GeneratePropertyStatements(ContractModel model, bool isDebugGeneration, bool isNotNullPropertyType, bool isGetter, SyntaxTriviaList tabStatementTrivia, SyntaxTriviaList tabStatementExtraLineEndTrivia)
    {
        Contract.Assert(!isGetter || !isNotNullPropertyType);

        List<StatementSyntax> Statements = [];

        if (!isGetter && isNotNullPropertyType)
        {
            ExpressionSyntax ContractName = SyntaxFactory.IdentifierName(ContractClassName);
            SimpleNameSyntax AssertNotNull = SyntaxFactory.IdentifierName(nameof(Contract.AssertNotNull));
            MemberAccessExpressionSyntax MemberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ContractName, AssertNotNull);
            IdentifierNameSyntax InputName = SyntaxFactory.IdentifierName("value");
            ArgumentSyntax InputArgument = SyntaxFactory.Argument(InputName);
            List<ArgumentSyntax> Arguments = [InputArgument];
            ArgumentListSyntax ArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(Arguments));
            ExpressionSyntax GetValueExpression = SyntaxFactory.InvocationExpression(MemberAccessExpression, ArgumentList).WithLeadingTrivia(SyntaxFactory.Space);

            IdentifierNameSyntax VarIdentifier = SyntaxFactory.IdentifierName("var");
            SyntaxToken NotNullValueIdentifier = SyntaxFactory.Identifier(Settings.ValueIdentifier);
            EqualsValueClauseSyntax Initializer = SyntaxFactory.EqualsValueClause(GetValueExpression).WithLeadingTrivia(SyntaxFactory.Space);
            VariableDeclaratorSyntax VariableDeclarator = SyntaxFactory.VariableDeclarator(NotNullValueIdentifier, null, Initializer).WithLeadingTrivia(SyntaxFactory.Space);
            VariableDeclarationSyntax Declaration = SyntaxFactory.VariableDeclaration(VarIdentifier, SyntaxFactory.SeparatedList([VariableDeclarator]));
            LocalDeclarationStatementSyntax LocalDeclarationStatement = SyntaxFactory.LocalDeclarationStatement(Declaration);

            Statements.Add(LocalDeclarationStatement.WithLeadingTrivia(tabStatementTrivia));
        }

        GetPropertyAccessStatements(model,
                                    isNotNullPropertyType,
                                    tabStatementTrivia,
                                    tabStatementExtraLineEndTrivia,
                                    out StatementSyntax CallStatement,
                                    out StatementSyntax ReturnStatement,
                                    out StatementSyntax AssignStatement);

        foreach (AttributeModel AttributeModel in model.Attributes)
            if (AttributeModel.Name != nameof(AccessAttribute))
                AddAttributeStatements(isDebugGeneration, isGetter, tabStatementTrivia, tabStatementExtraLineEndTrivia, Statements, AttributeModel);

        if (isGetter)
        {
            Statements.Insert(0, CallStatement);
            Statements.Add(ReturnStatement.WithLeadingTrivia(tabStatementExtraLineEndTrivia));
        }
        else
        {
            Statements.Add(AssignStatement.WithLeadingTrivia(tabStatementExtraLineEndTrivia));
        }

        return Statements;
    }

    private static void GetPropertyAccessStatements(ContractModel model,
                                                    bool isNotNullPropertyType,
                                                    SyntaxTriviaList tabStatement,
                                                    SyntaxTriviaList tabStatementExtraLineEndTrivia,
                                                    out StatementSyntax callStatement,
                                                    out StatementSyntax returnStatement,
                                                    out StatementSyntax assignStatement)
    {
        callStatement = GeneratePropertyQueryStatement(model.ShortName, tabStatement);
        returnStatement = GenerateReturnStatement();
        assignStatement = GeneratePropertyAssignmentStatement(model.ShortName, isNotNullPropertyType);
        assignStatement = assignStatement.WithLeadingTrivia(tabStatementExtraLineEndTrivia);
    }

    private static LocalDeclarationStatementSyntax GeneratePropertyQueryStatement(string propertyName, SyntaxTriviaList leadingTrivia)
    {
        string VerifiedSuffix = Settings.VerifiedSuffix;
        ExpressionSyntax Invocation = SyntaxFactory.IdentifierName(propertyName + VerifiedSuffix).WithLeadingTrivia(SyntaxFactory.Space);
        IdentifierNameSyntax VarIdentifier = SyntaxFactory.IdentifierName("var");
        SyntaxToken ResultIdentifier = SyntaxFactory.Identifier(Settings.ResultIdentifier);
        EqualsValueClauseSyntax Initializer = SyntaxFactory.EqualsValueClause(Invocation).WithLeadingTrivia(SyntaxFactory.Space);
        VariableDeclaratorSyntax VariableDeclarator = SyntaxFactory.VariableDeclarator(ResultIdentifier, null, Initializer).WithLeadingTrivia(SyntaxFactory.Space);
        VariableDeclarationSyntax Declaration = SyntaxFactory.VariableDeclaration(VarIdentifier, SyntaxFactory.SeparatedList([VariableDeclarator]));
        LocalDeclarationStatementSyntax LocalDeclarationStatement = SyntaxFactory.LocalDeclarationStatement(Declaration).WithLeadingTrivia(leadingTrivia);

        return LocalDeclarationStatement;
    }

    private static ExpressionStatementSyntax GeneratePropertyAssignmentStatement(string propertyName, bool isNotNullPropertyType)
    {
        AssignmentExpressionSyntax AssignmentExpression = GeneratePropertyAssignment(propertyName, isNotNullPropertyType ? Settings.ValueIdentifier : "value");
        ExpressionStatementSyntax AssignmentStatement = SyntaxFactory.ExpressionStatement(AssignmentExpression);

        return AssignmentStatement;
    }

    private static AssignmentExpressionSyntax GeneratePropertyAssignment(string propertyName, string valueName)
    {
        string VerifiedSuffix = Settings.VerifiedSuffix;
        ExpressionSyntax PropertyName = SyntaxFactory.IdentifierName(propertyName + VerifiedSuffix).WithLeadingTrivia(SyntaxFactory.Space);
        IdentifierNameSyntax ValueName = SyntaxFactory.IdentifierName(valueName).WithLeadingTrivia(SyntaxFactory.Space);
        SyntaxToken EqualToken = SyntaxFactory.Token(SyntaxKind.EqualsToken);
        EqualToken = EqualToken.WithLeadingTrivia(SyntaxFactory.Space);
        AssignmentExpressionSyntax AssignmentExpression = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, PropertyName, EqualToken, ValueName);

        return AssignmentExpression;
    }

    private static void AddAttributeStatements(bool isDebugGeneration,
                                               bool isGetter,
                                               SyntaxTriviaList tabStatementTrivia,
                                               SyntaxTriviaList tabStatementExtraLineEndTrivia,
                                               List<StatementSyntax> statements,
                                               AttributeModel attributeModel)
    {
        List<StatementSyntax> AttributeStatements = GeneratePropertyAttributeStatements(attributeModel, isDebugGeneration, isGetter);

        foreach (StatementSyntax Statement in AttributeStatements)
        {
            if (isGetter && statements.Count == 0)
                statements.Add(Statement.WithLeadingTrivia(tabStatementExtraLineEndTrivia));
            else
                statements.Add(Statement.WithLeadingTrivia(tabStatementTrivia));
        }
    }

    private static List<StatementSyntax> GeneratePropertyAttributeStatements(AttributeModel attributeModel, bool isDebugGeneration, bool isGetter)
    {
        return attributeModel.Name is nameof(EnsureAttribute) && isGetter
            ? GenerateEnsureStatement(attributeModel.Arguments, isDebugGeneration)
            : attributeModel.Name is nameof(RequireAttribute) && !isGetter
                ? GenerateRequireStatement(attributeModel.Arguments, isDebugGeneration)
                : [];
    }
}
