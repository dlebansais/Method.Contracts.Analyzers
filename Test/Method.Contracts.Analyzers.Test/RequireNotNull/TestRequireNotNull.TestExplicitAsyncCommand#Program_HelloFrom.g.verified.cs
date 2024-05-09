//HintName: Program_HelloFrom.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.4.6")]
    public static async Task HelloFrom(string text, out string textPlus)
    {
        Contract.RequireNotNull(text, out string Text);

        await HelloFromVerified(Text, out textPlus);
    }
}