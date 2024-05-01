//HintName: Program_HelloFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.0.1")]
    public static void HelloFrom(string text1, string text2, out string textPlus)
    {
        Contract.Require(text1.Length > 0);
        Contract.Require(text2.Length > 0);

        HelloFromVerified(text1, text2, out textPlus);
    }
}