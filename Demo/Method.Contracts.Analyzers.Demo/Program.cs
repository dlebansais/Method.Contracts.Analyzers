namespace Contracts.Analyzers.Demo;

using System;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Started...");
        string Text = HelloFrom("Hello, World");
        Console.WriteLine(Text);
    }

    [RequireNotNull("text")]
    [Require("text.Length > 0")]
    [Ensure("Result.Length == text.Length + 1")]
    private static string  HelloFromVerified(string text)
    {
        return text + "!";
    }
}
