//HintName: Program_Foo_get.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","3.0.2.50")]
    public static int[] Foo
    {
        get => FooVerified;
        set
        {
            var Value = Contract.AssertNotNull(value);
            Contract.Require(value > 0);

            FooVerified = Value;
        }
    }
}