namespace Contracts.Analyzers;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
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

        Debug.Assert(TargetNode is MethodDeclarationSyntax);
        MethodDeclarationSyntax MethodDeclaration = (MethodDeclarationSyntax)TargetNode;
        bool IsDebugGeneration = MethodDeclaration.SyntaxTree.Options.PreprocessorSymbolNames.Contains("DEBUG");

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

        BlockSyntax MethodBody = GenerateBody(model, MethodDeclaration, IsDebugGeneration, LeadingTrivia, LeadingTriviaWithoutLineEnd, isAsync, Tab);
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

    private static BlockSyntax GenerateBody(ContractModel model, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration, SyntaxTriviaList tabTrivia, SyntaxTriviaList tabTriviaWithoutLineEnd, bool isAsync, string tab)
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

        List<StatementSyntax> Statements = GenerateStatements(model, methodDeclaration, isDebugGeneration, TabStatementTrivia, TabStatementExtraLineEndTrivia, isAsync);

        return SyntaxFactory.Block(OpenBraceToken, SyntaxFactory.List(Statements), CloseBraceToken);
    }

    private static SyntaxTriviaList WhitespaceTrivia { get; } = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
}
