//HintName: Program_HelloFrom_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    /// <summary>
    /// Test doc.
    /// </summary>
    /// <param name="text">Test parameter.</param>
    /// <returns>Test value.</returns>
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.7.2.23")]
    public static string HelloFrom(string text)
    {
        var Result = HelloFromVerified(text);

        return Result;
    }
}