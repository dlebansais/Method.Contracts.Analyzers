namespace Contracts.Analyzers;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Contracts.Analyzers.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Represents a code generator.
/// </summary>
public partial class ContractGenerator
{
    private static void UpdateWithGeneratedMethodDeclaration(GeneratorAttributeSyntaxContext context, ref ContractModel model)
    {
        SyntaxNode TargetNode = context.TargetNode;
        if (TargetNode is not MethodDeclarationSyntax MethodDeclaration)
            return;

        bool IsDebugGeneration = MethodDeclaration.SyntaxTree.Options.PreprocessorSymbolNames.Contains("DEBUG");

        string Tab = new(' ', Math.Max(Settings.TabLength, 1));
        SyntaxTriviaList LeadingTrivia = GetLeadingTriviaWithLineEnd(Tab);
        SyntaxTriviaList LeadingTriviaWithoutLineEnd = GetLeadingTriviaWithoutLineEnd(Tab);
        SyntaxTriviaList? TrailingTrivia = GetModifiersTrailingTrivia(MethodDeclaration);
        bool SimplifyReturnTypeLeadingTrivia = MethodDeclaration.Modifiers.Count == 0;

        SyntaxList<AttributeListSyntax> CodeAttributes = GenerateCodeAttributes();
        MethodDeclaration = MethodDeclaration.WithAttributeLists(CodeAttributes);

        SyntaxToken ShortIdentifier = SyntaxFactory.Identifier(model.ShortName);
        MethodDeclaration = MethodDeclaration.WithIdentifier(ShortIdentifier);

        SyntaxTokenList Modifiers = GenerateContractModifiers(ref model, MethodDeclaration, LeadingTrivia, TrailingTrivia);
        MethodDeclaration = MethodDeclaration.WithModifiers(Modifiers);

        BlockSyntax MethodBody = GenerateBody(model, MethodDeclaration, IsDebugGeneration, LeadingTrivia, LeadingTriviaWithoutLineEnd, Tab);
        MethodDeclaration = MethodDeclaration.WithExpressionBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None)).WithBody(MethodBody);

        if (HasUpdatedParameterList(model, MethodDeclaration, out ParameterListSyntax ParameterList))
            MethodDeclaration = MethodDeclaration.WithParameterList(ParameterList);

        if (model.IsAsync && IsTaskType(MethodDeclaration.ReturnType))
            MethodDeclaration = MethodDeclaration.WithReturnType(SyntaxFactory.IdentifierName("Task").WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "))));
        else if (SimplifyReturnTypeLeadingTrivia) // This case applies to methods with zero modifier that become public.
            MethodDeclaration = MethodDeclaration.WithReturnType(MethodDeclaration.ReturnType.WithLeadingTrivia(SyntaxFactory.Space));

        MethodDeclaration = MethodDeclaration.WithLeadingTrivia(LeadingTriviaWithoutLineEnd);

        model = model with { GeneratedMethodDeclaration = MethodDeclaration.ToFullString() };
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

    private static SyntaxTokenList GenerateContractModifiers(ref ContractModel model, MemberDeclarationSyntax memberDeclaration, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia)
    {
        List<SyntaxToken> ModifierTokens = [];

        ModifierTokens = model.Attributes.Find(m => m.Name == nameof(AccessAttribute)) is AttributeModel AccessAttributeModel
            ? GenerateContractExplicitModifiers(ref model, AccessAttributeModel, leadingTrivia, trailingTrivia)
            : GenerateContractDefaultModifiers(ref model, memberDeclaration, leadingTrivia, trailingTrivia);

        return SyntaxFactory.TokenList(ModifierTokens);
    }

    private static List<SyntaxToken> GenerateContractExplicitModifiers(ref ContractModel model, AttributeModel accessAttributeModel, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia)
    {
        List<SyntaxToken> ModifierTokens = [];

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
                model = model with { IsAsync = true };
        }

        return ModifierTokens;
    }

    private static List<SyntaxToken> GenerateContractDefaultModifiers(ref ContractModel model, MemberDeclarationSyntax memberDeclaration, SyntaxTriviaList leadingTrivia, SyntaxTriviaList? trailingTrivia)
    {
        List<SyntaxToken> ModifierTokens = [];

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
                    model = model with { IsAsync = true };
            }
        }

        int LastItemIndex = ModifierTokens.Count - 1;
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
        {
            GetAttributeHasTypeOrName(Attribute, out AssignTrackingString ParameterName, out AssignTrackingString Type, out AssignTrackingString Name);

            if (ParameterName.Value == parameter.Identifier.Text)
            {
                if (Type.IsSet)
                {
                    TypeSyntax UpatedType = SyntaxFactory.IdentifierName(Type.Value).WithTrailingTrivia(SyntaxFactory.Space);
                    updatedParameter = updatedParameter.WithType(UpatedType);
                }

                if (Name.IsSet)
                {
                    SyntaxToken UpdatedIdentifier = SyntaxFactory.Identifier(Name.Value);
                    updatedParameter = updatedParameter.WithIdentifier(UpdatedIdentifier);
                }
            }
        }

        return updatedParameter != parameter;
    }

    private static void GetAttributeHasTypeOrName(AttributeModel attribute, out AssignTrackingString parameterName, out AssignTrackingString type, out AssignTrackingString name)
    {
        parameterName = new();
        type = new();
        name = new();

        if (AttributeHasType(attribute, out AssignTrackingString TypeParameterName, out AssignTrackingString ParsedType))
        {
            parameterName = TypeParameterName;
            type = ParsedType;
        }

        if (AttributeHasName(attribute, out AssignTrackingString NameParameterName, out AssignTrackingString ParseName))
        {
            parameterName = NameParameterName;
            name = ParseName;
        }
    }

    private static bool AttributeHasType(AttributeModel attribute, out AssignTrackingString parameterName, out AssignTrackingString type)
    {
        parameterName = new AssignTrackingString();
        type = new AssignTrackingString();

        foreach (AttributeArgumentModel AttributeArgument in attribute.Arguments)
        {
            if (AttributeArgument.Name == string.Empty)
                parameterName = (AssignTrackingString)AttributeArgument.Value;
            if (AttributeArgument.Name == nameof(RequireNotNullAttribute.Type))
                type = (AssignTrackingString)AttributeArgument.Value;
        }

        // Valid attribute for RequireNotNull always have a parameter name.
        Contract.Assert(parameterName.IsSet);

        return type.IsSet;
    }

    private static bool AttributeHasName(AttributeModel attribute, out AssignTrackingString parameterName, out AssignTrackingString name)
    {
        parameterName = new AssignTrackingString();
        name = new AssignTrackingString();

        foreach (AttributeArgumentModel AttributeArgument in attribute.Arguments)
        {
            if (AttributeArgument.Name == string.Empty)
                parameterName = (AssignTrackingString)AttributeArgument.Value;
            if (AttributeArgument.Name == nameof(RequireNotNullAttribute.Name))
                name = (AssignTrackingString)AttributeArgument.Value;
        }

        // Valid attribute for RequireNotNull always have a parameter name.
        Contract.Assert(parameterName.IsSet);

        return name.IsSet;
    }

    private static BlockSyntax GenerateBody(ContractModel model, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration, SyntaxTriviaList tabTrivia, SyntaxTriviaList tabTriviaWithoutLineEnd, string tab)
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

        List<StatementSyntax> Statements = GenerateStatements(model, methodDeclaration, isDebugGeneration, TabStatementTrivia, TabStatementExtraLineEndTrivia);

        return SyntaxFactory.Block(OpenBraceToken, SyntaxFactory.List(Statements), CloseBraceToken);
    }
}
