//HintName: Program_HelloFrom_3311014053_3945725674_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.4.5.14")]
    public static void HelloFrom(string text1, object? text2, out string textPlus)
    {
        Contract.RequireNotNull(text1, out string Text1);

        HelloFromVerified(Text1, text2, out textPlus);
    }
}