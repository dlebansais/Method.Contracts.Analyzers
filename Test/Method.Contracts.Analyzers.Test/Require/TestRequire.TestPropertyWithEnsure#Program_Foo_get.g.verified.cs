//HintName: Program_Foo_get.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.8.0.26")]
    public static string Foo
    {
        get
        {
            var Result = FooVerified;

            Contract.Ensure(Result.Length > 0);

            return Result;
        }
        set
        {
            var Value = Contract.AssertNotNull(value);
            Contract.Require(Value.Length > 0);

            FooVerified = Value;
        }
    }
}