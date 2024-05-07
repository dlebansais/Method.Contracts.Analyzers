//HintName: Program_HelloFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.2.4")]
    public static string HelloFrom(string text, ref string copy)
    {
        var Result = HelloFromVerified(text, ref copy);

        return Result;
    }
}