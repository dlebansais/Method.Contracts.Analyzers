namespace Contracts.Analyzers.Demo;

using System;

internal partial class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Started...");
        HelloFrom();
    }

    [Access("public", "static")]
    static void HelloFromVerified()
    {
        Console.WriteLine("Hello, World!");
    }
}
