//HintName: Program_HelloFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.4.6")]
    public static void HelloFrom(string text, out string textPlus)
    {
        Contract.Require(text.Length > 0);

        HelloFromVerified(text, out textPlus);
    }
}