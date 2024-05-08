﻿//HintName: Program_HelloFrom.g.cs
namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.4.6")]
    public static void HelloFrom(string text1, string text2, out string textPlus)
    {
        Contract.RequireNotNull(text1, out string Text1);
        Contract.RequireNotNull(text2, out string Text2);

        HelloFromVerified(Text1, Text2, out textPlus);
    }
}