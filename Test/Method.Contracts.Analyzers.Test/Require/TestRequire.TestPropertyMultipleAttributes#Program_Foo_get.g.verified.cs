//HintName: Program_Foo_get.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","2.0.1.36")]
    public static string Foo
    {
        get => FooVerified;
        set
        {
            var Value = Contract.AssertNotNull(value);
            Contract.Require(Value.Length > 0);
            Contract.Require(Value.Length > 1);

            FooVerified = Value;
        }
    }
}