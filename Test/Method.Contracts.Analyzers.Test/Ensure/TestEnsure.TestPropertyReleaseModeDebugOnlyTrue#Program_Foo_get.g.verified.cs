//HintName: Program_Foo_get.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.9.3.31")]
    public static string Foo
    {
        get
        {
            var Result = FooVerified;

            return Result;
        }
        set
        {
            var Value = Contract.AssertNotNull(value);

            FooVerified = Value;
        }
    }
}