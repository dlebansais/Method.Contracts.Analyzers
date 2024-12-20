//HintName: Program_Foo_get.g.cs
#nullable enable

namespace Contracts.TestSuite;

using System;
using System.CodeDom.Compiler;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.9.5.33")]
    public static int Foo
    {
        get => FooVerified;
        set
        {
            Contract.Require(value > 0);

            FooVerified = value;
        }
    }
}