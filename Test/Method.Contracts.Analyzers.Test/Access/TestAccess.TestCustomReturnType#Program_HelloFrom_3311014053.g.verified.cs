//HintName: Program_HelloFrom_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","2.0.5.41")]
    public static Program HelloFrom(string text)
    {
        var Result = HelloFromVerified(text);

        return Result;
    }
}