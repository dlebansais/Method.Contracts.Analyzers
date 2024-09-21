//HintName: Program_HelloFrom_3311014053_3311014053_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.6.0.19")]
    public static void HelloFrom(string text1, string text2, out string textPlus)
    {
        Contract.RequireNotNull(text1, out string Text1);
        Contract.RequireNotNull(text2, out string Text2);

        HelloFromVerified(Text1, Text2, out textPlus);
    }
}