//HintName: Program_Foo_get.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","2.0.4.40")]
    public static string Foo
    {
        get
        {
            var Result = FooVerified;

            Contract.Ensure(Result.Length > 0);
            Contract.Ensure(Result.Length > 1);

            return Result;
        }
        set
        {
            var Value = Contract.AssertNotNull(value);

            FooVerified = Value;
        }
    }
}