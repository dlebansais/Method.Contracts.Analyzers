//HintName: Program_HelloFrom_3311014053_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.4.0.10")]
    public static string HelloFrom(string text, out string copy)
    {
        var Result = HelloFromVerified(text, out copy);

        return Result;
    }
}