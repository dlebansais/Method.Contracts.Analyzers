//HintName: Program_HelloFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.3.5")]
    public static string HelloFrom(string text)
    {
        var Result = HelloFromVerified(text);

        Contract.Ensure(Result.Length > text.Length);

        return Result;
    }
}