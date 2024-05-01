namespace Contracts.Analyzers.Demo;

using System;
using System.Text.RegularExpressions;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        /*
        Console.WriteLine("Started...");
        HelloFrom("Hello, World", out string Text);
        Console.WriteLine(Text);

        Regex x = AbcOrDefGeneratedRegex();
        Console.WriteLine($"{x}");
        */
    }

    [RequireNotNull("text")]
    [Require("text.Length > 0")]
    [Ensure("textPlus.Length == text.Length + 1")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + "!";
    }
}
