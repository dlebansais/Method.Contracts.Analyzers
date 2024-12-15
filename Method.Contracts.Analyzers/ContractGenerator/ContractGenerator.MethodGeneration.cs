namespace Contracts.Analyzers;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        if (TargetNode is not MethodDeclarationSyntax MethodDeclaration)
        {
            isAsync = false;
            return string.Empty;
        }

        bool IsDebugGeneration = MethodDeclaration.SyntaxTree.Options.PreprocessorSymbolNames.Contains("DEBUG");

        string Tab = new(' ', Math.Max(Settings.TabLength, 1));
        SyntaxTriviaList LeadingTrivia = GetLeadingTriviaWithLineEnd(Tab);
        SyntaxTriviaList LeadingTriviaWithoutLineEnd = GetLeadingTriviaWithoutLineEnd(Tab);
        SyntaxTriviaList? TrailingTrivia = GetModifiersTrailingTrivia(MethodDeclaration);
        bool SimplifyReturnTypeLeadingTrivia = MethodDeclaration.Modifiers.Count == 0 && MethodDeclaration.ReturnType.HasLeadingTrivia;

        SyntaxList<AttributeListSyntax> CodeAttributes = GenerateCodeAttributes();
        MethodDeclaration = MethodDeclaration.WithAttributeLists(CodeAttributes);

        SyntaxToken ShortIdentifier = SyntaxFactory.Identifier(model.ShortName);
        MethodDeclaration = MethodDeclaration.WithIdentifier(ShortIdentifier);

        SyntaxTokenList Modifiers = GenerateContractModifiers(model, MethodDeclaration, LeadingTrivia, TrailingTrivia, out isAsync);
        MethodDeclaration = MethodDeclaration.WithModifiers(Modifiers);

        BlockSyntax MethodBody = GenerateBody(model, MethodDeclaration, IsDebugGeneration, LeadingTrivia, LeadingTriviaWithoutLineEnd, isAsync, Tab);
        MethodDeclaration = MethodDeclaration.WithExpressionBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None)).WithBody(MethodBody);

        if (HasUpdatedParameterList(model, MethodDeclaration, out ParameterListSyntax ParameterList))
            MethodDeclaration = MethodDeclaration.WithParameterList(ParameterList);

        if (isAsync && IsTaskType(MethodDeclaration.ReturnType))
            MethodDeclaration = MethodDeclaration.WithReturnType(SyntaxFactory.IdentifierName("Task").WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "))));
        else if (SimplifyReturnTypeLeadingTrivia) // This case applies to methods with zero modifier that become public.
            MethodDeclaration = MethodDeclaration.WithReturnType(MethodDeclaration.ReturnType.WithLeadingTrivia(WhitespaceTrivia));

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

        AttributeArgumentListSyntax ArgumentList = SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList([ToolNameAttributeArgument, ToolVersionAttributeArgument]));
        AttributeSyntax Attribute = SyntaxFactory.Attribute(AttributeName, ArgumentList);
        AttributeListSyntax AttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList([Attribute]));
        SyntaxList<AttributeListSyntax> Attributes = SyntaxFactory.List([AttributeList]);

        return Attributes;
    }

    private static string GetToolName()
    {
        AssemblyName ExecutingAssemblyName = Assembly.GetExecutingAssembly().GetName();
        return ExecutingAssemblyName.Name.ToString();
    }

    private static string GetToolVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    private static SyntaxTokenList GenerateContractModifiers(ContractModel model, MemberDeclarationSyntax memberDeclaration, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia, out bool isAsync)
    {
        List<SyntaxToken> ModifierTokens = [];

        ModifierTokens = model.Attributes.Find(m => m.Name == nameof(AccessAttribute)) is AttributeModel AccessAttributeModel
            ? GenerateContractExplicitModifiers(AccessAttributeModel, leadingTrivia, trailingTrivia, out isAsync)
            : GenerateContractDefaultModifiers(memberDeclaration, leadingTrivia, trailingTrivia, out isAsync);

        return SyntaxFactory.TokenList(ModifierTokens);
    }

    private static List<SyntaxToken> GenerateContractExplicitModifiers(AttributeModel accessAttributeModel, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia, out bool isAsync)
    {
        List<SyntaxToken> ModifierTokens = [];
        isAsync = false;

        for (int i = 0; i < accessAttributeModel.Arguments.Count; i++)
        {
            AttributeArgumentModel ArgumentModel = accessAttributeModel.Arguments[i];
            string ArgumentValue = ArgumentModel.Value;
            SyntaxToken ModifierToken = SyntaxFactory.Identifier(ArgumentValue);

            ModifierToken = i == 0 ? ModifierToken.WithLeadingTrivia(leadingTrivia) : ModifierToken.WithLeadingTrivia(SyntaxFactory.Space);

            if (i + 1 == accessAttributeModel.Arguments.Count)
                ModifierToken = ModifierToken.WithTrailingTrivia(trailingTrivia);

            ModifierTokens.Add(ModifierToken);

            if (ArgumentValue is "async")
                isAsync = true;
        }

        return ModifierTokens;
    }

    private static List<SyntaxToken> GenerateContractDefaultModifiers(MemberDeclarationSyntax memberDeclaration, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia, out bool isAsync)
    {
        List<SyntaxToken> ModifierTokens = [];
        isAsync = false;

        SyntaxToken PublicModifierToken = SyntaxFactory.Identifier("public");
        PublicModifierToken = PublicModifierToken.WithLeadingTrivia(leadingTrivia);
        ModifierTokens.Add(PublicModifierToken);

        // If the method is static and/or async, add the same static modifier to the generated code.
        foreach (SyntaxToken Modifier in memberDeclaration.Modifiers)
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

        int LastItemIndex = memberDeclaration.Modifiers.Count - 1;
        ModifierTokens[LastItemIndex] = ModifierTokens[LastItemIndex].WithTrailingTrivia(trailingTrivia);

        return ModifierTokens;
    }

    private static SyntaxTriviaList GetLeadingTriviaWithLineEnd(string tab)
    {
        List<SyntaxTrivia> Trivias =
        [
            SyntaxFactory.EndOfLine("\n"),
            SyntaxFactory.Whitespace(tab),
        ];

        return SyntaxFactory.TriviaList(Trivias);
    }

    private static SyntaxTriviaList GetLeadingTriviaWithoutLineEnd(string tab)
    {
        List<SyntaxTrivia> Trivias =
        [
            SyntaxFactory.Whitespace(tab),
        ];

        return SyntaxFactory.TriviaList(Trivias);
    }

    private static SyntaxTriviaList? GetModifiersTrailingTrivia(MemberDeclarationSyntax memberDeclaration) => memberDeclaration.Modifiers.Count > 0 ? memberDeclaration.Modifiers.Last().TrailingTrivia : null;

    private static bool HasUpdatedParameterList(ContractModel model, MethodDeclarationSyntax methodDeclaration, out ParameterListSyntax updatedParameterList)
    {
        ParameterListSyntax ParameterList = methodDeclaration.ParameterList;
        updatedParameterList = ParameterList;

        SeparatedSyntaxList<ParameterSyntax> Parameters = ParameterList.Parameters;
        SeparatedSyntaxList<ParameterSyntax> UpdatedParameters = Parameters;

        foreach (ParameterSyntax Parameter in UpdatedParameters)
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

        // Valid attribute for RequireNotNull always have a parameter name.
        Contract.Assert(parameterName != string.Empty);

        return type != string.Empty || name != string.Empty;
    }

    private static BlockSyntax GenerateBody(ContractModel model, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration, SyntaxTriviaList tabTrivia, SyntaxTriviaList tabTriviaWithoutLineEnd, bool isAsync, string tab)
    {
        bool IsExpressionBody = methodDeclaration.ExpressionBody is not null;
        SyntaxToken OpenBraceToken = SyntaxFactory.Token(SyntaxKind.OpenBraceToken);
        OpenBraceToken = OpenBraceToken.WithLeadingTrivia(IsExpressionBody ? tabTrivia : tabTriviaWithoutLineEnd);

        List<SyntaxTrivia> TrivialList = [.. tabTrivia, SyntaxFactory.Whitespace(tab)];
        SyntaxTriviaList TabStatementTrivia = SyntaxFactory.TriviaList(TrivialList);

        List<SyntaxTrivia> TrivialListExtraLineEnd = new(tabTrivia);
        TrivialListExtraLineEnd.Insert(0, SyntaxFactory.EndOfLine("\n"));
        TrivialListExtraLineEnd.Add(SyntaxFactory.Whitespace(tab));
        SyntaxTriviaList TabStatementExtraLineEndTrivia = SyntaxFactory.TriviaList(TrivialListExtraLineEnd);

        SyntaxToken CloseBraceToken = SyntaxFactory.Token(SyntaxKind.CloseBraceToken);
        CloseBraceToken = CloseBraceToken.WithLeadingTrivia(tabTrivia);

        List<StatementSyntax> Statements = GenerateStatements(model, methodDeclaration, isDebugGeneration, TabStatementTrivia, TabStatementExtraLineEndTrivia, isAsync);

        return SyntaxFactory.Block(OpenBraceToken, SyntaxFactory.List(Statements), CloseBraceToken);
    }

    private static SyntaxTriviaList WhitespaceTrivia { get; } = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
}
