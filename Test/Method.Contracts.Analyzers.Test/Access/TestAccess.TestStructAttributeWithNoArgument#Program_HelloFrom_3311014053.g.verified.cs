﻿//HintName: Program_HelloFrom_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial struct Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.4.8.16")]
    public static string HelloFrom(string text)
    {
        var Result = HelloFromVerified(text);

        return Result;
    }
}