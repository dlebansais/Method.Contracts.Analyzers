//HintName: Program_ByeByeFrom_3311014053_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","3.0.2.50")]
    public static void ByeByeFrom(string text, out string textPlus)
    {
        ByeByeFromVerified(text, out textPlus);
    }
}