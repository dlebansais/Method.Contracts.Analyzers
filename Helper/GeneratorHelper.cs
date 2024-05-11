namespace Contracts.Analyzers.Helper;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Helper class for the code generator.
/// </summary>
internal static class GeneratorHelper
{
    /// <summary>
    /// Returns <paramref name="text"/> prefixed with <paramref name="prefix"/> and followed with <paramref name="suffix"/> if not empty; otherwise just return <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The string to compare to the empty string.</param>
    /// <param name="prefix">The prefix.</param>
    /// <param name="suffix">The suffix.</param>
    public static string AddPrefixAndSuffixIfNotEmpty(string text, string prefix, string suffix)
    {
        return text == string.Empty ? string.Empty : $"{prefix}{text}{suffix}";
    }

    private const string UsingDirectivePrefix = "using ";

    /// <summary>
    /// Sorts using directives.
    /// </summary>
    /// <param name="usings">The using directives to sort.</param>
    /// <returns>The sorted directives.</returns>
    public static string SortUsings(string usings)
    {
        if (usings == string.Empty)
            return string.Empty;

        List<string> Namespaces = new();
        string[] Lines = usings.Split('\n');

        foreach (string Line in Lines)
            if (IsUsingDirective(Line, out string Directive))
                Namespaces.Add(Directive);

        Namespaces.Sort(SortWithSystemFirst);
        Namespaces = Namespaces.Distinct().ToList();

        string Result = string.Empty;

        foreach (string DirectiveNamespace in Namespaces)
            Result += $"\n{UsingDirectivePrefix}{DirectiveNamespace};";

        return Result + "\n";
    }

    private static bool IsUsingDirective(string line, out string directiveNamespace)
    {
        string TrimmedLine = line.Trim(' ').Trim('\n').Trim('\r');

        if (StringStartsWith(TrimmedLine, UsingDirectivePrefix) && StringEndsWith(TrimmedLine, ";"))
        {
            string RawNamespace = TrimmedLine.Substring(UsingDirectivePrefix.Length, TrimmedLine.Length - UsingDirectivePrefix.Length - 1);
            string[] Names = RawNamespace.Split('.');

            List<string> TrimmedNames = new();
            foreach (string Name in Names)
                TrimmedNames.Add(Name.Trim());

            directiveNamespace = string.Join(".", TrimmedNames);
            return true;
        }

        directiveNamespace = string.Empty;
        return false;
    }

    private static int SortWithSystemFirst(string line1, string line2)
    {
        if (IsSystemUsing(line1) && !IsSystemUsing(line2))
            return -1;
        else if (!IsSystemUsing(line1) && IsSystemUsing(line2))
            return 1;
        else
#if NETFRAMEWORK
            return string.Compare(line1, line2);
#else
            return string.Compare(line1, line2, StringComparison.Ordinal);
#endif
    }

    private static bool IsSystemUsing(string usingNamespace)
    {
        return usingNamespace == "System" || StringStartsWith(usingNamespace, "System.");
    }

    /// <summary>
    /// Returns whether the string <paramref name="s"/> starts with the prefix <paramref name="prefix"/>, performing a <see cref="StringComparison.Ordinal"/> comparison.
    /// </summary>
    /// <param name="s">The string.</param>
    /// <param name="prefix">The prefix.</param>
    public static bool StringStartsWith(string s, string prefix)
    {
        return s.StartsWith(prefix, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns whether the string <paramref name="s"/> ends with the suffix <paramref name="suffix"/>, performing a <see cref="StringComparison.Ordinal"/> comparison.
    /// </summary>
    /// <param name="s">The string.</param>
    /// <param name="suffix">The suffix.</param>
    public static bool StringEndsWith(string s, string suffix)
    {
        return s.EndsWith(suffix, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns a hash code for a string that is stable from one execution to the other.
    /// </summary>
    /// <param name="s">The string to get the hash code of.</param>
    public static int GetStableHashCode(string s)
    {
        unchecked
        {
            int hash1 = 5381;
            int hash2 = hash1;

            for (int i = 0; i < s.Length && s[i] != '\0'; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ s[i];
                if (i == s.Length - 1 || s[i + 1] == '\0')
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ s[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }

    /// <summary>
    /// Gets all supported attributes of a method.
    /// </summary>
    /// <param name="methodDeclaration">The method.</param>
    /// <param name="supportedAttributeNames">The list of supported attributes.</param>
    public static List<AttributeSyntax> GetMethodSupportedAttributes(MethodDeclarationSyntax methodDeclaration, List<string> supportedAttributeNames)
    {
        List<AttributeSyntax> Result = new();

        for (int IndexList = 0; IndexList < methodDeclaration.AttributeLists.Count; IndexList++)
        {
            AttributeListSyntax AttributeList = methodDeclaration.AttributeLists[IndexList];

            for (int Index = 0; Index < AttributeList.Attributes.Count; Index++)
            {
                AttributeSyntax Attribute = AttributeList.Attributes[Index];
                string AttributeName = ToAttributeName(Attribute);

                if (supportedAttributeNames.Contains(AttributeName))
                {
                    Result.Add(Attribute);
                }
            }
        }

        return Result;
    }

    /// <summary>
    /// Returns the full name of an attribute.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    public static string ToAttributeName(AttributeSyntax attribute)
    {
        return $"{attribute.Name.GetText()}{nameof(Attribute)}";
    }
}