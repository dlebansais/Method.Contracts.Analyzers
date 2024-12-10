//HintName: Program_HelloFrom_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial record struct Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.9.0.27")]
    public static string HelloFrom(string text)
    {
        var Result = HelloFromVerified(text);

        return Result;
    }
}