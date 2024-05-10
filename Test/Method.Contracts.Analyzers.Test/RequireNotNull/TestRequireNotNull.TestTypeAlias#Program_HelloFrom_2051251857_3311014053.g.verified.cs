//HintName: Program_HelloFrom_2051251857_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.6.9")]
    public static void HelloFrom(object text, out string textPlus)
    {
        Contract.RequireNotNull(text, out string Text);

        HelloFromVerified(Text, out textPlus);
    }
}