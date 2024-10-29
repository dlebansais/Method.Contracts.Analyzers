//HintName: Program_HelloFrom_3311014053_3311014053.g.cs
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
    /// <param name="newText">Test parameter 1.</param>
    /// <param name="textPlus">Test parameter 2, a copy of <paramref name="newText"/>.</param>
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.7.0.21")]
    public static void HelloFrom(string newText, out string textPlus)
    {
        Contract.RequireNotNull(newText, out string NewText);

        HelloFromVerified(NewText, out textPlus);
    }
}