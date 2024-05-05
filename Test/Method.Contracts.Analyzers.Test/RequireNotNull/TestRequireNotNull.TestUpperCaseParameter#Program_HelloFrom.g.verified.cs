//HintName: Program_HelloFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.1.1")]
    public static string HelloFrom(string Text)
    {
        Contract.RequireNotNull(Text, out string _Text);

        var Result = HelloFromVerified(_Text);

        return Result;
    }
}