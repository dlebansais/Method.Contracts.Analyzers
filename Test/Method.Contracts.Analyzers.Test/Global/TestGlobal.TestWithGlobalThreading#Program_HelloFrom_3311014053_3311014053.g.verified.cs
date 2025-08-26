//HintName: Program_HelloFrom_3311014053_3311014053.g.cs
#nullable enable

namespace Contracts.TestSuite;

using global::System.CodeDom.Compiler;
using global::System.Threading.Tasks;
using Contracts;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","2.0.6.42")]
    public static void HelloFrom(string text, out string textPlus)
    {
        HelloFromVerified(text, out textPlus);
    }
}