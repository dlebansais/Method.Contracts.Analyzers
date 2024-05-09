//HintName: Program_HelloFrom.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.4.6")]
    public static string HelloFrom(string Text)
    {
        var Result = HelloFromVerified(Text);

        return Result;
    }
}