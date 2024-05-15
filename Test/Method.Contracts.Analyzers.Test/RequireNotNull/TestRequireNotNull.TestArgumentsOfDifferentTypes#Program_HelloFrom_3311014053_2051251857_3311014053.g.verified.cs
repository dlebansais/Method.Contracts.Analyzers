//HintName: Program_HelloFrom_3311014053_2051251857_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.4.2.11")]
    public static void HelloFrom(string text1, object text2, out string textPlus)
    {
        Contract.RequireNotNull(text1, out string Text1);
        Contract.RequireNotNull(text2, out object Text2);

        HelloFromVerified(Text1, Text2, out textPlus);
    }
}