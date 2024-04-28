namespace Contracts.Analyzers.Demo;

using System;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Started...");
        HelloFrom("Hello, World", out string Text);
        Console.WriteLine(Text);
    }

    [Access("public", "static")]
    [RequireNotNull("text")]
    [Require("text.Length > 0")]
    [Ensure("textPlus.Length == text.Length + 1")]
    static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + "!";
    }
}
