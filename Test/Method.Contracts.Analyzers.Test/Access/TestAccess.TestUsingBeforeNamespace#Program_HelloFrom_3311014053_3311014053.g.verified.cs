//HintName: Program_HelloFrom_3311014053_3311014053.g.cs
#nullable enable

using System;
using Contracts;

namespace Contracts.TestSuite;

using System.CodeDom.Compiler;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","2.0.4.40")]
    public static void HelloFrom(string text, out string textPlus)
    {
        HelloFromVerified(text, out textPlus);
    }
}