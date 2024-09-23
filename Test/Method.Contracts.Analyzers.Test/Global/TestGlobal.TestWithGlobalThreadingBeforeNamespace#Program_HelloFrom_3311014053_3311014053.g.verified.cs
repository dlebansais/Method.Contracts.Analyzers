//HintName: Program_HelloFrom_3311014053_3311014053.g.cs
#nullable enable

using global::System.CodeDom.Compiler;
using global::System.Threading.Tasks;
using Contracts;

namespace Contracts.TestSuite;

partial class Program
{
    [GeneratedCodeAttribute("Method.Contracts.Analyzers","1.6.1.20")]
    public static void HelloFrom(string text, out string textPlus)
    {
        HelloFromVerified(text, out textPlus);
    }
}