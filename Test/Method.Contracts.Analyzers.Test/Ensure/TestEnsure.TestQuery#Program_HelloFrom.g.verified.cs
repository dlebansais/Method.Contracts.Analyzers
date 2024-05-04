//HintName: Program_HelloFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.0.1")]
    public static string HelloFrom(string text)
    {
        var Result = HelloFromVerified(text);

        Contract.Ensure(Result.Length > text.Length);

        return Result;
    }
}