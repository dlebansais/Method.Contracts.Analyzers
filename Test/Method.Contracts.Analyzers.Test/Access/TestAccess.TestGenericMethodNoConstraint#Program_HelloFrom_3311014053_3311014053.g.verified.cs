//HintName: Program_HelloFrom_3311014053_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.7.2.23")]
    public static void HelloFrom<T>(string text, out string textPlus)
    {
        HelloFromVerified(text, out textPlus);
    }
}