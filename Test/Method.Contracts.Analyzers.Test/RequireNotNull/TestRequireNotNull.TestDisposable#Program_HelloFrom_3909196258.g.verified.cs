//HintName: Program_HelloFrom_3909196258.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using System.IO;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","3.0.7.55")]
    public static void HelloFrom(Stream stream)
    {
        Contract.RequireNotNull(stream, out Stream Stream);

        HelloFromVerified(Stream);
    }
}