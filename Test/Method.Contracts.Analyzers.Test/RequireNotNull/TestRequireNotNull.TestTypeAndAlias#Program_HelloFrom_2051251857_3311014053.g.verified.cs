﻿//HintName: Program_HelloFrom_2051251857_3311014053.g.cs
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
    /// <param name="text">Test parameter 1.</param>
    /// <param name="textPlus">Test parameter 2, a copy of <paramref name="text"/>.</param>
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.4.4.13")]
    public static void HelloFrom(string text, out string textPlus)
    {
        Contract.RequireNotNull(text, out object Foo);

        HelloFromVerified(Foo, out textPlus);
    }
}