//HintName: Program_HelloFrom_3311014053_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using System.Threading.Tasks;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.4.5.14")]
    public static async Task HelloFrom(string text, out string textPlus)
    {
        Contract.RequireNotNull(text, out string Text);

        await HelloFromVerified(Text, out textPlus);
    }
}