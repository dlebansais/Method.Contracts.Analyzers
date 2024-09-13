//HintName: Program_HelloFrom_3311014053_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.4.9.16")]
    public static string HelloFrom(string text, ref string copy)
    {
        var Result = HelloFromVerified(text, ref copy);

        return Result;
    }
}