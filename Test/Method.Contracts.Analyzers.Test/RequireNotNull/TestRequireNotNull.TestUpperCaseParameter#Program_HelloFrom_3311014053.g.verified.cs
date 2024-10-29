﻿//HintName: Program_HelloFrom_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.7.0.21")]
    public static string HelloFrom(string Text)
    {
        Contract.RequireNotNull(Text, out string _Text);

        var Result = HelloFromVerified(_Text);

        return Result;
    }
}