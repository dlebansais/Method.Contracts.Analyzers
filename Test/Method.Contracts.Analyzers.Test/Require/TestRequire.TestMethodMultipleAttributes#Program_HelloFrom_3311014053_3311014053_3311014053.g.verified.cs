//HintName: Program_HelloFrom_3311014053_3311014053_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.9.1.29")]
    public static void HelloFrom(string text1, string text2, out string textPlus)
    {
        Contract.Require(text1.Length > 0);
        Contract.Require(text2.Length > 0);

        HelloFromVerified(text1, text2, out textPlus);
    }
}