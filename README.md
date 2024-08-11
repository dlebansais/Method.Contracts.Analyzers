# Method.Contracts.Analyzers

A code generator companion for [Method.Contracts](https://github.com/dlebansais/Method.Contracts).

[![Build status](https://ci.appveyor.com/api/projects/status/hh3eks1b9v66mx2f?svg=true)](https://ci.appveyor.com/project/dlebansais/method-contracts-analyzers) [![CodeFactor](https://www.codefactor.io/repository/github/dlebansais/method.contracts.analyzers/badge)](https://www.codefactor.io/repository/github/dlebansais/method.contracts.analyzers) [![NuGet](https://img.shields.io/nuget/v/Method.Contracts.Analyzers.svg)](https://www.nuget.org/packages/Method.Contracts.Analyzers)

This assembly applies to projects using **C# 8 or higher** and with **Nullable** enabled.

## Usage

Add the assembly from the latest release as a dependency of your project. The `Contracts` namespace then becomes available.

````csharp
using Contracts;
````

## Summary

This code generators can be used to decorate methods with contracts. The general principle is to declare private method `FooVerified` with arguments, and attributes specifying the contract around these arguments. The generator will then add a public method `Foo` with the same arguments, and code to check these contracts, then call `FooVerified`.

For instance, consider:

````csharp
using Contracts;

[Access("public")]
[RequireNotNull(nameof(text))]
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

    var Result = TryParseFooVerified(Text, out parsedFoo);

    return Result;
}
````

## RequireNotNull attribute

Specifies that one or more arguments must not be null.

Usage:

````csharp
using Contracts;

[RequireNotNull("text1", "text2")]
private void FooVerified(string text1, string text2)
{
  // ...
}
````

There can be multiple occurrences of the `RequireNotNull` attribute for the same method. The generator will add a single call to `Contract.RequireNotNull` for each argument specified by their name, and use automatic type resolution to handle their type.

The generator can handle the special case of `IDisposable` arguments.

The `nameof` syntax can be used for parameter names:
````csharp
[RequireNotNull(nameof(text1), nameof(text2))]
````

### Alias name

The default strategy for naming the parameter alias is as follow:

+ If the parameter begins with a lowercase letter, use the same name but begining with an uppercase letter. For instance, `text` is changed to `Text`.
+ Otherwise, add an underscore prefix. For instance, `Text` is changed to `_Text` and `_text` is changed to `__text`.

You can specify your own aliasing with the `AliasName` attribute option:

````csharp
[RequireNotNull(nameof(text), AliasName = "Text")] // This is the same as using the default alias.
[RequireNotNull(nameof(text), AliasName = "_textFoo")]
````

Note that in this case only one parameter name is allowed. To alias multiple parameters, use multiple `RequireNotNull` attributes.

### Type and name of the generate code

You can also specify a subtype for the parameter. For example, WPF converters must implement the `IValueConverter` interface, and specifically the following method:

````csharp
object Convert(object value, Type targetType, object parameter, CultureInfo culture);
````

When `value` can only be of a specific type (such as `string`, `IList`...), there must be a cast within the verified method:

````csharp
[RequireNotNull(nameof(value))]
[Require("Value is IList)]
private static object ConvertVerified(object value, Type targetType, object parameter, CultureInfo culture)
{
    IList Items = (IList)parameter;
    //...
}
````

You can specify the expected subtype with the `Type` and `Name` attribute parameters, and the code above can then be simplified as follow:

````csharp
[RequireNotNull(nameof(items), Type = "object", Name = "value")]
private static object ConvertVerified(IList items, Type targetType, object parameter, CultureInfo culture)
{
    //...
}
````

Similarly to `AliasName`, `Type` and `Name` can only be used if there is exactly one attribute parameter.

## Require attribute

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

There can be multiple occurrences of the `Require` attribute for the same method. The generator will add a single call to `Contract.Require` for each expression.

If there is an error in the expression (for example, a syntax error), this can only be caught in the generated code.

### Debug only

The optional `DebugOnly` argument can be set to `true` to generate code that compiles only if `DEBUG` is set.

## Ensure attribute

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

There can be multiple occurrences of the `Ensure` attribute for the same method. The generator will add a single call to `Contract.Ensure` for each expression.

If there is an error in the expression (for example, a syntax error), this can only be caught in the generated code.

You can use the special name `Result` in expression that test the returned value.

### Debug only

The optional `DebugOnly` argument can be set to `true` to generate code that compiles only if `DEBUG` is set.

## Access attribute

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

Note that `public` is the default access when the `Access` attribute is not present. If you need `private` you must specify it explicitly.

If there is no `Access` attribute and the method is `static` or `async`, the generated code is also `static` (or `async`, respectively). Otherwise, you have to specify it explicitly.

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

You don't have to specify all values if you're changing just one setting. Note that empty strings for `SuffixVerifier` and `ReturnIdentifier` are ignored, as well as `TabLength` if it's not a strictly positive integer value.
