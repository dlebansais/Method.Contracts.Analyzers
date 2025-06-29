//HintName: Program_Foo_get.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","2.0.3.39")]
    public static string? Foo
    {
        get => FooVerified;
        set
        {
            Contract.Require(value is null || value.Length > 0);

            FooVerified = value;
        }
    }
}