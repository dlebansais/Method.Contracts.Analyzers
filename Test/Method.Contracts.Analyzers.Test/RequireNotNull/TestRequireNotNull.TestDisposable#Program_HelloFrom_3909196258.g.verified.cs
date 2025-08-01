﻿//HintName: Program_HelloFrom_3909196258.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using System.IO;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","2.0.4.40")]
    public static void HelloFrom(Stream stream)
    {
        Contract.RequireNotNull(stream, out Stream Stream);

        HelloFromVerified(Stream);
    }
}