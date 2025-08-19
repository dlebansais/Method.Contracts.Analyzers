namespace Contracts.Analyzers.Demo;

using System;
using Contracts;

/// <summary>
/// Represents the demo program.
/// </summary>
internal partial class Program
{
    /// <summary>
    /// The main entry point.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Started...");
        string Text = HelloFrom("Hello, World");
        Console.WriteLine(Text);
    }

    /// <summary>
    /// Test doc.
    /// </summary>
    /// <param name="text">Test parameter, a copy of <paramref name="text"/>.</param>
    /// <returns>Test value.</returns>
    [RequireNotNull("text")]
    [Require("text.Length > 0")]
    [Ensure("DemoResult.Length == text.Length + 1")]
    private static string HelloFromDemoVerified(string text) => text + "!";
}
