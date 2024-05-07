//HintName: Program_HelloFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.2.4")]
    public static void HelloFrom(string text1, string text2, out string textPlus)
    {
        HelloFromVerified(text1, text2, out textPlus);

        Contract.Ensure(textPlus.Length > text1.Length);
        Contract.Ensure(textPlus.Length > text2.Length);
    }
}