//HintName: Program_ByeByeFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.1.3")]
    public static void ByeByeFrom(string text, out string textPlus)
    {
        ByeByeFromVerified(text, out textPlus);
    }
}