﻿//HintName: Program_HelloFrom.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.3.5.7")]
    public async static Task<string> HelloFrom(string text)
    {
        Contract.RequireNotNull(text, out string Text);

        var Result = await HelloFromVerified(Text);

        return Result;
    }
}