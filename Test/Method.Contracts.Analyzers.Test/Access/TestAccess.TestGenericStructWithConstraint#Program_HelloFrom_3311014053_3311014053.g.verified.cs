﻿//HintName: Program_HelloFrom_3311014053_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial struct Program<T> where T : class, Exception
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.4.7.16")]
    public static void HelloFrom(string text, out string textPlus)
    {
        HelloFromVerified(text, out textPlus);
    }
}