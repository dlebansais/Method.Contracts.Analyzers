﻿//HintName: Program_HelloFrom_3311014053_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","2.0.1.36")]
    public static void HelloFrom(string text, out string textPlus)
    {
        Contract.Require(text.Length > 0);

        HelloFromVerified(text, out textPlus);
    }
}