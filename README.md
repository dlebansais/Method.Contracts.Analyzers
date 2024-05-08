# Method.Contracts.Analyzers

A code generator companion for [Method.Contracts](https://github.com/dlebansais/Method.Contracts).

[![Build status](https://ci.appveyor.com/api/projects/status/ex06fppm0o8d8fh1?svg=true)](https://ci.appveyor.com/project/dlebansais/method-contracts) [![CodeFactor](https://www.codefactor.io/repository/github/dlebansais/method.contracts/badge)](https://www.codefactor.io/repository/github/dlebansais/method.contracts) [![NuGet](https://img.shields.io/nuget/v/Method.Contracts.svg)](https://www.nuget.org/packages/Method.Contracts)

This assembly applies to projects using **C# 8 or higher** and with **Nullable** enabled.

## Usage

Add the assembly from the latest release as a dependency of your project. The `Contracts` namespace then becomes available.

````csharp
using Contracts;
````

## Summary

This code generators can be used to decorate methods with contracts. The general principle is to declare private method `FooVerified` with arguments, and attributes specifying the contract around these arguments. The genertor will then add a public method `Foo` with the same arguments, and code to check these contracts, then call `FooVerified`.

For instance, consider:

````csharp
using Contracts;

[Access("public")]
[RequireNotNull("text")]
private bool TryParseFooVerified(string text, out Foo parsedFoo)
{
    if (text.Length > 0)
    {
	// ...
    }
````

This will generate the following code:

````csharp
public bool TryParseFoo(string text, out Foo parsedFoo)
{
    Contract.RequireNotNull(text, out string Text);

    return TryParseFooVerified(Text, out Foo parsedFoo);
}
````

### RequireNotNull attribute

Specifies that one or more arguments must not be null.

Usage:

````csharp
using Contracts;

[RequireNotNull(text1, text2)]
private void FooVerified(string text1, string text2)
{
  // ...
}
````

There can be multiple occurences of the `RequireNotNull` attribute for the same method. The generator will add a single call to `Contract.RequireNotNull` for each argument specified by their name, and use automatic type resolution to handle their type.

The generator can handle the special case of `IDisposable` arguments.

### Require attribute

Specifies that one or more conditions must be true upon entering the method. Conditions can freely mix arguments and other variables. They must provided to the attribute has a string arguments.

Usage:

````csharp
using Contracts;

public string Bar { get; set; }

[Require("text.Length > Bar.Length")]
private void FooVerified(string text)
{
  // ...
}
````

There can be multiple occurences of the `Require` attribute for the same method. The generator will add a single call to `Contract.Require` for each expression.

If there is an error in the expression (for example, a syntax error), this can only be caught in the generated code.

### Ensure attribute

Specifies that one or more conditions are guaranteed to be true on method exit. Conditions can freely mix arguments (`in`, `out` or `ref`) and other variables. They must provided to the attribute has a string arguments.

Usage:

````csharp
using Contracts;

public string Bar { get; set; }

[Ensure("Result.Length > Bar.Length")]
private string FooVerified()
{
  return Bar + "!";
}
````

There can be multiple occurences of the `Ensure` attribute for the same method. The generator will add a single call to `Contract.Ensure` for each expression.

If there is an error in the expression (for example, a syntax error), this can only be caught in the generated code.

You can use the special name `Result` in expression that test the returned value.

### Access attribute

Indicates that the generated method access has one or more specifiers. These can be `public`, `internal` and so on. If multiple specifiers are needed, such as `protected internal`, provide each of them as separate argument.

````csharp
using Contracts;

[Access("protected", "internal")]
private void FooVerified()
{
  // ...
}
````

This will generate the following code:

````csharp
protected internal void Foo()
{
  FooVerified();
}
````

Note that `public` is the default access when the `Access` attribute is not present. If you need `private` you must specify it explicitely.

If there is no `Access` attribute and the method is `static` or `async`, the generated code is also `static` (or `async`, respectively). Otherwise, you have to specify it explicitely.

## Configuration

You can configure the generator with the following settings:

+ `SuffixVerifier`: specifies which suffix a method should have to support contract attributes. The default value is `Verified` (see `TryParseFooVerified` above in sample code).
+ `TabLength`: the number of whitespace for a tab in generated code. The default value is 4.
+ `ReturnIdentifier`: the name of the identifier that can be used in `Ensure` expressions to indicate the value returned by the method. The default is `Result`.
+ `DisabledWarnings`: a comma-separated list of warnings to disable in the generated code with `#pragma warning disable`.

To change a setting, modify the `.csproj` file of your project as follow (*Demo* is just an example):

````xml
    <PropertyGroup>
        <ContractSourceGenerator_VerifiedSuffix>DemoVerified</ContractSourceGenerator_VerifiedSuffix>
        <ContractSourceGenerator_TabLength>8</ContractSourceGenerator_TabLength>
        <ContractSourceGenerator_ResultIdentifier>DemoResult</ContractSourceGenerator_ResultIdentifier>
    </PropertyGroup>

    <ItemGroup>
        <CompilerVisibleProperty Include="ContractSourceGenerator_VerifiedSuffix;ContractSourceGenerator_TabLength;ContractSourceGenerator_ResultIdentifier" />
    </ItemGroup>
````

You don't have to specify all values if you're changing just one setting. Note that empty strings for `SuffixVerifier` and `ReturnIdentifier` are ignored, as well `TabLength` if not a strictly positive integer value.
