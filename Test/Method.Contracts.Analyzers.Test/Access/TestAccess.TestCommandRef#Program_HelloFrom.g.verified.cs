//HintName: Program_HelloFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.1.3")]
    public static void HelloFrom(ref string text)
    {
        HelloFromVerified(ref text);
    }
}