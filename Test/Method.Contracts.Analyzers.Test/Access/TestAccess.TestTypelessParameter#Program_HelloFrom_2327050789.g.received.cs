﻿//HintName: Program_HelloFrom_2327050789.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.4.8.16")]
    public static string HelloFrom(dynamic text)
    {
        var Result = HelloFromVerified(text);

        return Result;
    }
}