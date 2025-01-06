namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Contracts.Analyzers.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Represents a code generator.
/// </summary>
public partial class ContractGenerator
{
    private static ContractModel TransformContractAttributes(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        SyntaxNode TargetNode = context.TargetNode;
        MemberDeclarationSyntax MemberDeclaration = Contract.AssertOfType<MemberDeclarationSyntax>(TargetNode);

        ContractModel Model = GetModelWithoutContract(context, MemberDeclaration);
        Model = Model with { Attributes = GetModelContract(MemberDeclaration) };
        UpdateWithDocumentation(MemberDeclaration, ref Model);
        UpdateWithGeneratedMethodDeclaration(context, ref Model);
        UpdateWithGeneratedPropertyDeclaration(context, ref Model);
        UpdateUsings(context, ref Model);

        return Model;
    }

    private static ContractModel GetModelWithoutContract(GeneratorAttributeSyntaxContext context, MemberDeclarationSyntax memberDeclaration)
    {
        INamedTypeSymbol ContainingClass = Contract.AssertNotNull(context.TargetSymbol.ContainingType);
        INamespaceSymbol ContainingNamespace = Contract.AssertNotNull(ContainingClass.ContainingNamespace);

        string Namespace = ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
        string ClassName = ContainingClass.Name;
        string? DeclarationTokens = null;
        string? FullClassName = null;

        if (memberDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>() is ClassDeclarationSyntax ClassDeclaration)
        {
            DeclarationTokens = "class";
            FullClassName = ClassName;
            TypeParameterListSyntax? TypeParameterList = ClassDeclaration.TypeParameterList;

            if (TypeParameterList is not null)
            {
                FullClassName += TypeParameterList.ToString();

                string ConstraintClauses = ClassDeclaration.ConstraintClauses.ToString();
                if (ConstraintClauses != string.Empty)
                    FullClassName += " " + ConstraintClauses;
            }
        }

        if (memberDeclaration.FirstAncestorOrSelf<StructDeclarationSyntax>() is StructDeclarationSyntax StructDeclaration)
        {
            DeclarationTokens = "struct";
            FullClassName = ClassName;
            TypeParameterListSyntax? TypeParameterList = StructDeclaration.TypeParameterList;

            if (TypeParameterList is not null)
            {
                FullClassName += TypeParameterList.ToString();

                string ConstraintClauses = StructDeclaration.ConstraintClauses.ToString();
                if (ConstraintClauses != string.Empty)
                    FullClassName += " " + ConstraintClauses;
            }
        }

        if (memberDeclaration.FirstAncestorOrSelf<RecordDeclarationSyntax>() is RecordDeclarationSyntax RecordDeclaration)
        {
            DeclarationTokens = RecordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? "record struct" : "record";
            FullClassName = ClassName;
            TypeParameterListSyntax? TypeParameterList = RecordDeclaration.TypeParameterList;

            if (TypeParameterList is not null)
            {
                FullClassName += TypeParameterList.ToString();

                string ConstraintClauses = RecordDeclaration.ConstraintClauses.ToString();
                if (ConstraintClauses != string.Empty)
                    FullClassName += " " + ConstraintClauses;
            }
        }

        string SymbolName = context.TargetSymbol.Name;
        string VerifiedSuffix = Settings.VerifiedSuffix;

        Contract.Assert(GeneratorHelper.StringEndsWith(SymbolName, VerifiedSuffix));
        Contract.Assert(SymbolName.Length > VerifiedSuffix.Length);
        string ShortName = SymbolName[..^VerifiedSuffix.Length];

        return new ContractModel(
            Namespace: Namespace,
            UsingsBeforeNamespace: string.Empty,
            UsingsAfterNamespace: string.Empty,
            ClassName: ClassName,
            DeclarationTokens: Contract.AssertNotNull(DeclarationTokens),
            FullClassName: Contract.AssertNotNull(FullClassName),
            ShortName: ShortName,
            UniqueOverloadIdentifier: GetUniqueOverloadIdentifier(memberDeclaration),
            Documentation: string.Empty,
            Attributes: [],
            GeneratedMethodDeclaration: string.Empty,
            GeneratedPropertyDeclaration: string.Empty,
            IsAsync: false);
    }

    private static string GetUniqueOverloadIdentifier(MemberDeclarationSyntax memberDeclaration) => memberDeclaration is MethodDeclarationSyntax MethodDeclaration ? GetUniqueOverloadIdentifier(MethodDeclaration) : "_get";

    private static string GetUniqueOverloadIdentifier(MethodDeclarationSyntax methodDeclaration)
    {
        ParameterListSyntax ParameterList = methodDeclaration.ParameterList;
        string Result = string.Empty;

        for (int i = 0; i < ParameterList.Parameters.Count; i++)
        {
            ParameterSyntax Parameter = ParameterList.Parameters[i];

            // Empirically, there is always a type even if the parameter is empty.
            TypeSyntax Type = Contract.AssertNotNull(Parameter.Type);

            string TypeAsString = Type.ToString();
            uint HashCode = unchecked((uint)GeneratorHelper.GetStableHashCode(TypeAsString));
            Result += $"_{HashCode}";
        }

        return Result;
    }

    private static void UpdateWithDocumentation(MemberDeclarationSyntax memberDeclaration, ref ContractModel model)
    {
        if (memberDeclaration.HasLeadingTrivia)
        {
            SyntaxTriviaList LeadingTrivia = memberDeclaration.GetLeadingTrivia();

            List<SyntaxTrivia> SupportedTrivias = [];
            foreach (SyntaxTrivia trivia in LeadingTrivia)
                if (IsSupportedTrivia(trivia))
                    SupportedTrivias.Add(trivia);

            // Trim consecutive end of lines until there is only at most one at the beginning.
            bool HadEndOfLine = false;
            while (HasStartingEndOfLineTrivias(SupportedTrivias))
            {
                int PreviousRemaining = SupportedTrivias.Count;

                HadEndOfLine = true;
                SupportedTrivias.RemoveAt(0);

                // Ensures that this while loop is not infinite.
                int Remaining = SupportedTrivias.Count;
                Contract.Assert(Remaining + 1 == PreviousRemaining);
            }

            if (HadEndOfLine)
            {
                // Trim whitespace trivias at start.
                while (IsFirstTriviaWhitespace(SupportedTrivias))
                {
                    int PreviousRemaining = SupportedTrivias.Count;

                    SupportedTrivias.RemoveAt(0);

                    // Ensures that this while loop is not infinite.
                    int Remaining = SupportedTrivias.Count;
                    Contract.Assert(Remaining + 1 == PreviousRemaining);
                }
            }

            // Remove successive whitespace trivias.
            int i = 0;
            while (i + 1 < SupportedTrivias.Count)
            {
                int PreviousRemaining = SupportedTrivias.Count - i;

                if (SupportedTrivias[i].IsKind(SyntaxKind.WhitespaceTrivia) && SupportedTrivias[i + 1].IsKind(SyntaxKind.WhitespaceTrivia))
                    SupportedTrivias.RemoveAt(i);
                else
                    i++;

                int Remaining = SupportedTrivias.Count - i;

                // Ensures that this while loop is not infinite.
                Contract.Assert(Remaining + 1 == PreviousRemaining);
            }

            LeadingTrivia = SyntaxFactory.TriviaList(SupportedTrivias);
            if (LeadingTrivia.Any(SyntaxKind.SingleLineDocumentationCommentTrivia))
                model = model with { Documentation = LeadingTrivia.ToFullString().Trim('\r').Trim('\n').TrimEnd(' ') };
        }

        Dictionary<string, string> ParameterNameReplacementTable = memberDeclaration is MethodDeclarationSyntax MethodDeclaration
            ? GetParameterNameReplacementTable(model, MethodDeclaration)
            : [];

        foreach (KeyValuePair<string, string> Entry in ParameterNameReplacementTable)
        {
            string OldParameterName = $"<param name=\"{Entry.Key}\">";
            string NewParameterName = $"<param name=\"{Entry.Value}\">";
            model = model with { Documentation = AnalyzerTools.Replace(model.Documentation, OldParameterName, NewParameterName) };

            string OldParameterRef = $"<paramref name=\"{Entry.Key}\"/>";
            string NewParameterRef = $"<paramref name=\"{Entry.Value}\"/>";
            model = model with { Documentation = AnalyzerTools.Replace(model.Documentation, OldParameterRef, NewParameterRef) };
        }
    }

    private static bool IsSupportedTrivia(SyntaxTrivia trivia)
    {
        return trivia.IsKind(SyntaxKind.EndOfLineTrivia) ||
               trivia.IsKind(SyntaxKind.WhitespaceTrivia) ||
               trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
               trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
    }

    private static bool IsFirstTriviaWhitespace(List<SyntaxTrivia> trivias)
    {
        // If we reach this method there is at least one end of line, therefore at least one trivia.
        Contract.Assert(trivias.Count > 0);

        SyntaxTrivia FirstTrivia = trivias[0];

        return FirstTrivia.IsKind(SyntaxKind.WhitespaceTrivia);
    }

    private static bool HasStartingEndOfLineTrivias(List<SyntaxTrivia> trivias)
    {
        int Count = 0;

        for (int i = 0; i < trivias.Count; i++)
        {
            SyntaxTrivia Trivia = trivias[i];

            if (Trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                Count++;

                if (Count > 1)
                    return true;
            }
            else if (!Trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                return false;
            }
        }

        return false;
    }

    private static Dictionary<string, string> GetParameterNameReplacementTable(ContractModel model, MethodDeclarationSyntax methodDeclaration)
    {
        Dictionary<string, string> Result = [];

        ParameterListSyntax ParameterList = methodDeclaration.ParameterList;
        SeparatedSyntaxList<ParameterSyntax> Parameters = ParameterList.Parameters;

        foreach (ParameterSyntax Parameter in Parameters)
            if (IsParameterNameReplaced(model, Parameter, out AssignTrackingString ReplacementName))
                Result.Add(Parameter.Identifier.Text, ReplacementName.Value);

        return Result;
    }

    private static bool IsParameterNameReplaced(ContractModel model, ParameterSyntax parameter, out AssignTrackingString replacementName)
    {
        replacementName = new AssignTrackingString();

        foreach (AttributeModel Attribute in model.Attributes)
            if (AttributeHasName(Attribute, out AssignTrackingString ParameterName, out replacementName) && ParameterName.Value == parameter.Identifier.Text && replacementName.IsSet)
                return true;

        return false;
    }

    private static List<AttributeModel> GetModelContract(MemberDeclarationSyntax memberDeclaration)
    {
        List<AttributeModel> Result = [];
        List<AttributeSyntax> MemberAttributes = GeneratorHelper.GetMemberSupportedAttributes(context: null, memberDeclaration, SupportedAttributeTypes);

        Dictionary<string, Func<MemberDeclarationSyntax, IReadOnlyList<AttributeArgumentSyntax>, List<AttributeArgumentModel>>> AttributeTransformTable = new()
        {
            { nameof(AccessAttribute), TransformAccessAttribute },
            { nameof(RequireNotNullAttribute), TransformRequireNotNullAttribute },
            { nameof(RequireAttribute), TransformRequireAttribute },
            { nameof(EnsureAttribute), TransformEnsureAttribute },
        };

        foreach (AttributeSyntax Attribute in MemberAttributes)
            if (Attribute.ArgumentList is AttributeArgumentListSyntax AttributeArgumentList)
            {
                string AttributeName = GeneratorHelper.ToAttributeName(Attribute);
                IReadOnlyList<AttributeArgumentSyntax> AttributeArguments = AttributeArgumentList.Arguments;

                Contract.Assert(AttributeTransformTable.ContainsKey(AttributeName));
                Func<MemberDeclarationSyntax, IReadOnlyList<AttributeArgumentSyntax>, List<AttributeArgumentModel>> AttributeTransform = AttributeTransformTable[AttributeName];
                List<AttributeArgumentModel> Arguments = AttributeTransform(memberDeclaration, AttributeArguments);

                AttributeModel Model = new(AttributeName, Arguments);

                Result.Add(Model);
            }

        return Result;
    }

    private static List<AttributeArgumentModel> TransformAccessAttribute(MemberDeclarationSyntax memberDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments) => TransformStringOnlyAttribute(attributeArguments);

    private static List<AttributeArgumentModel> TransformRequireNotNullAttribute(MemberDeclarationSyntax memberDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        Contract.Assert(memberDeclaration is MethodDeclarationSyntax);
        MethodDeclarationSyntax MethodDeclarationSyntax = (MethodDeclarationSyntax)memberDeclaration;

        return IsRequireNotNullAttributeWithAliasTypeOrName(attributeArguments)
            ? TransformRequireNotNullAttributeWithAlias(MethodDeclarationSyntax, attributeArguments)
            : TransformRequireNotNullAttributeNoAlias(attributeArguments);
    }

    private static List<AttributeArgumentModel> TransformRequireNotNullAttributeWithAlias(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        // This was verified in IsRequireNotNullAttributeWithAlias().
        Contract.Assert(attributeArguments.Count > 0);
        AttributeArgumentSyntax FirstAttributeArgument = attributeArguments[0];

        // This was verified in IsValidRequireNotNullAttributeWithAlias().
        Contract.Assert(FirstAttributeArgument.NameEquals is null);

        bool IsValidParameterName = IsStringOrNameofAttributeArgument(FirstAttributeArgument, out string ParameterName);

        // This was verified in IsValidRequireNotNullAttributeWithAlias().
        Contract.Assert(IsValidParameterName);

        bool IsValidParameterType = GetParameterType(ParameterName, methodDeclaration, out _);

        // This was verified in IsValidRequireNotNullAttributeWithAlias().
        Contract.Assert(IsValidParameterType);

        AssignTrackingString Type = new();
        AssignTrackingString Name = new();
        AssignTrackingString AliasName = new();

        for (int i = 1; i < attributeArguments.Count; i++)
        {
            bool IsValidAttributeArgument = IsValidArgumentWithAliasTypeOrName(attributeArguments[i], ref Type, ref Name, ref AliasName);

            // This was verified in IsValidRequireNotNullAttributeWithAlias().
            Contract.Assert(IsValidAttributeArgument);
        }

        // This was verified in IsValidRequireNotNullAttributeWithAlias().
        Contract.Assert(Type.IsSet || Name.IsSet || AliasName.IsSet);

        List<AttributeArgumentModel> Result = [new AttributeArgumentModel(Name: string.Empty, Value: ParameterName)];

        if (Type.IsSet)
            Result.Add(new AttributeArgumentModel(Name: nameof(RequireNotNullAttribute.Type), Value: Type));

        if (Name.IsSet)
            Result.Add(new AttributeArgumentModel(Name: nameof(RequireNotNullAttribute.Name), Value: Name));

        if (AliasName.IsSet)
            Result.Add(new AttributeArgumentModel(Name: nameof(RequireNotNullAttribute.AliasName), Value: AliasName));

        Contract.Assert(Result.Count > 1);

        return Result;
    }

    private static List<AttributeArgumentModel> TransformRequireNotNullAttributeNoAlias(IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        List<AttributeArgumentModel> Result = [];

        foreach (AttributeArgumentSyntax AttributeArgument in attributeArguments)
        {
            // This was verified in IsValidRequireNotNullAttributeWithAlias().
            Contract.Assert(AttributeArgument.NameEquals is null);

            bool IsValidParameterName = IsStringOrNameofAttributeArgument(AttributeArgument, out string ParameterName);

            // This was verified in IsValidRequireNotNullAttributeWithAlias().
            Contract.Assert(IsValidParameterName);

            Result.Add(new AttributeArgumentModel(Name: string.Empty, Value: ParameterName));
        }

        return Result;
    }

    private static List<AttributeArgumentModel> TransformRequireAttribute(MemberDeclarationSyntax memberDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments) => TransformRequireOrEnsureAttribute(attributeArguments);

    private static List<AttributeArgumentModel> TransformEnsureAttribute(MemberDeclarationSyntax memberDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments) => TransformRequireOrEnsureAttribute(attributeArguments);

    private static List<AttributeArgumentModel> TransformRequireOrEnsureAttribute(IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        return IsRequireOrEnsureAttributeWithDebugOnly(attributeArguments)
            ? TransformRequireOrEnsureAttributeWithDebugOnly(attributeArguments)
            : TransformStringOnlyAttribute(attributeArguments);
    }

    private static List<AttributeArgumentModel> TransformRequireOrEnsureAttributeWithDebugOnly(IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        // This was verified in IsRequireOrEnsureAttributeWithDebugOnly().
        Contract.Assert(attributeArguments.Count > 0);
        AttributeArgumentSyntax FirstAttributeArgument = attributeArguments[0];

        // This was verified in IsValidRequireOrEnsureAttributeWithDebugOnly().
        Contract.Assert(FirstAttributeArgument.NameEquals is null);

        bool IsValidParameterName = IsStringAttributeArgument(FirstAttributeArgument, out string Expression);

        // This was verified in IsValidRequireOrEnsureAttributeWithDebugOnly().
        Contract.Assert(IsValidParameterName);

        bool? IsDebugOnly = null;

        for (int i = 1; i < attributeArguments.Count; i++)
        {
            bool IsValidAttributeArgument = IsValidDebugOnlyArgument(attributeArguments[i], ref IsDebugOnly);

            // This was verified in IsValidRequireOrEnsureAttributeWithDebugOnly().
            Contract.Assert(IsValidAttributeArgument);
        }

        // This was verified in IsValidRequireNotNullAttributeWithAlias().
        Contract.Assert(IsDebugOnly.HasValue);

        List<AttributeArgumentModel> Result =
        [
            new AttributeArgumentModel(Name: string.Empty, Value: Expression),
            new AttributeArgumentModel(Name: nameof(RequireAttribute.DebugOnly), Value: IsDebugOnly == true ? "true" : "false"),
        ];

        return Result;
    }

    private static List<AttributeArgumentModel> TransformStringOnlyAttribute(IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        bool IsValid = IsValidStringOnlyAttribute(attributeArguments, out Collection<string> ArgumentValues, out _);
        Contract.Assert(IsValid);

        List<AttributeArgumentModel> Result = [];
        foreach (string ArgumentValue in ArgumentValues)
            Result.Add(new AttributeArgumentModel(Name: string.Empty, Value: ArgumentValue));

        return Result;
    }

    private static void UpdateUsings(GeneratorAttributeSyntaxContext context, ref ContractModel model)
    {
        // We know it's not null from KeepNodeForPipeline().
        BaseNamespaceDeclarationSyntax BaseNamespaceDeclaration = Contract.AssertNotNull(context.TargetNode.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>());

        if (BaseNamespaceDeclaration.Usings.Count > 0)
        {
            string UsingString = BaseNamespaceDeclaration.Usings.ToFullString();
            Contract.Assert(UsingString.Length > 0);

            model = model with { UsingsAfterNamespace = UsingString };
        }

        if (BaseNamespaceDeclaration.Parent is CompilationUnitSyntax CompilationUnit)
            model = model with { UsingsBeforeNamespace = CompilationUnit.Usings.ToFullString() };

        AddMissingUsingsAndSort(ref model);
    }

    private static void AddMissingUsingsAndSort(ref ContractModel model)
    {
        model = model with { UsingsBeforeNamespace = GeneratorHelper.SortUsings(model.UsingsBeforeNamespace) };
        model = model with { UsingsAfterNamespace = GeneratorHelper.SortUsings(model.UsingsAfterNamespace) };
        bool UseGlobal = GeneratorHelper.HasGlobalSystem(model.UsingsAfterNamespace);

        AddMissingUsing(ref model, "Contracts", isGlobal: false);
        AddMissingUsing(ref model, "System.CodeDom.Compiler", isGlobal: UseGlobal);

        if (model.IsAsync)
            AddMissingUsing(ref model, "System.Threading.Tasks", isGlobal: UseGlobal);

        model = model with { UsingsAfterNamespace = GeneratorHelper.SortUsings(model.UsingsAfterNamespace) };
    }

    private static void AddMissingUsing(ref ContractModel model, string usingDirective, bool isGlobal)
    {
        string GlobalDirective = $"using global::{usingDirective};\n";
        string NonGlobalDirective = $"using {usingDirective};\n";
        bool IsDirectiveBeforeNamespace;
        bool IsDirectiveAfterNamespace;

#if NETSTANDARD2_1_OR_GREATER
        IsDirectiveBeforeNamespace = model.UsingsBeforeNamespace.Contains(GlobalDirective, StringComparison.Ordinal) || model.UsingsBeforeNamespace.Contains(NonGlobalDirective, StringComparison.Ordinal);
        IsDirectiveAfterNamespace = model.UsingsAfterNamespace.Contains(GlobalDirective, StringComparison.Ordinal) || model.UsingsAfterNamespace.Contains(NonGlobalDirective, StringComparison.Ordinal);
#else
        IsDirectiveBeforeNamespace = model.UsingsBeforeNamespace.Contains(GlobalDirective) || model.UsingsBeforeNamespace.Contains(NonGlobalDirective);
        IsDirectiveAfterNamespace = model.UsingsAfterNamespace.Contains(GlobalDirective) || model.UsingsAfterNamespace.Contains(NonGlobalDirective);
#endif

        if (!IsDirectiveBeforeNamespace && !IsDirectiveAfterNamespace)
        {
            string LineToAdd = isGlobal ? GlobalDirective : NonGlobalDirective;
            Contract.Assert(!model.UsingsAfterNamespace.Contains(LineToAdd));

            model = model with { UsingsAfterNamespace = model.UsingsAfterNamespace + LineToAdd };
        }
    }
}
