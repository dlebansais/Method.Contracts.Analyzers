//HintName: Program_HelloFrom.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.5.7")]
    public static string HelloFrom(string text, out string copy)
    {
        var Result = HelloFromVerified(text, out copy);

        return Result;
    }
}