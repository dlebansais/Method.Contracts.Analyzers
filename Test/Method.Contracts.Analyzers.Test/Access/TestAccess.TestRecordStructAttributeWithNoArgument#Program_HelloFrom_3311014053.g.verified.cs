//HintName: Program_HelloFrom_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial record struct Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","2.0.2.38")]
    public static string HelloFrom(string text)
    {
        var Result = HelloFromVerified(text);

        return Result;
    }
}