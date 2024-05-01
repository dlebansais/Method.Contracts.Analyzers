//HintName: Program_HelloFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.0.1")]
    public static void HelloFrom(string text, out string textPlus)
    {
        HelloFromVerified(text, out textPlus);

        Contract.Ensure(textPlus.Length > text.Length);
    }
}