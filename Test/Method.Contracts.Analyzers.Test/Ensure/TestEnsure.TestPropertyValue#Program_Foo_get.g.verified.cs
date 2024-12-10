//HintName: Program_Foo_get.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.9.0.27")]
    public static int Foo
    {
        get
        {
            var Result = FooVerified;

            Contract.Ensure(Result > 0);

            return Result;
        }
        set => FooVerified = value;
    }
}