//HintName: Program_HelloFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.3.5")]
    public static async Task<string> HelloFrom(string text)
    {
        Contract.RequireNotNull(text, out string Text);

        var Result = await HelloFromVerified(Text);

        return Result;
    }
}