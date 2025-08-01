﻿//HintName: Program_HelloFrom_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using System.Threading.Tasks;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","2.0.4.40")]
    public async static Task<string> HelloFrom(string text)
    {
        Contract.RequireNotNull(text, out string Text);

        var Result = await HelloFromVerified(Text);

        return Result;
    }
}