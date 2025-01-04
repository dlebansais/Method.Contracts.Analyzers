namespace Contracts.Analyzers.Test;

extern alias Helper;
extern alias Analyzers;

using NUnit.Framework;
using ContractGenerator = Analyzers::Contracts.Analyzers.ContractGenerator;
using GeneratorSettingsEntry = Helper::Contracts.Analyzers.Helper.GeneratorSettingsEntry;
using GeneratorHelper = Helper::Contracts.Analyzers.Helper.GeneratorHelper;

[TestFixture]
internal class TestSettings
{
    [Test]
    public void TestAsString()
    {
        const string TestValue = "test";
        string Value;

        GeneratorSettingsEntry Entry = new(BuildKey: ContractGenerator.ResultIdentifierKey, DefaultValue: ContractGenerator.DefaultResultIdentifier);

        Value = Entry.StringValueOrDefault(null, out bool IsNullDefault);
        Assert.That(Value, Is.EqualTo(ContractGenerator.DefaultResultIdentifier));
        Assert.That(IsNullDefault, Is.True);

        Value = Entry.StringValueOrDefault(string.Empty, out bool IsEmptyDefault);
        Assert.That(Value, Is.EqualTo(ContractGenerator.DefaultResultIdentifier));
        Assert.That(IsEmptyDefault, Is.True);

        Value = Entry.StringValueOrDefault(TestValue, out bool IsValueDefault);
        Assert.That(Value, Is.EqualTo(TestValue));
        Assert.That(IsValueDefault, Is.False);
    }

    [Test]
    public void TestAsInt()
    {
        const string InvalidIntTestValue = "test";
        const int ValidIntTestValue = 1;
        int Value;

        GeneratorSettingsEntry Entry = new(BuildKey: ContractGenerator.TabLengthKey, DefaultValue: $"{ContractGenerator.DefaultTabLength}");

        Value = Entry.IntValueOrDefault(null, out bool IsNullDefault);
        Assert.That(Value, Is.EqualTo(ContractGenerator.DefaultTabLength));
        Assert.That(IsNullDefault, Is.True);

        Value = Entry.IntValueOrDefault(string.Empty, out bool IsEmptyDefault);
        Assert.That(Value, Is.EqualTo(ContractGenerator.DefaultTabLength));
        Assert.That(IsEmptyDefault, Is.True);

        Value = Entry.IntValueOrDefault(InvalidIntTestValue, out bool IsInvalidDefault);
        Assert.That(Value, Is.EqualTo(ContractGenerator.DefaultTabLength));
        Assert.That(IsInvalidDefault, Is.True);

        Value = Entry.IntValueOrDefault($"{ValidIntTestValue}", out bool IsValidDefault);
        Assert.That(Value, Is.EqualTo(ValidIntTestValue));
        Assert.That(IsValidDefault, Is.False);
    }

    [Test]
    public void TestPrefixAndSuffix()
    {
        const string Prefix = " prefix ";
        const string Suffix = " suffix ";
        const string Text = " test ";

        string Empty = GeneratorHelper.AddPrefixAndSuffixIfNotEmpty(string.Empty, Prefix, Suffix);
        Assert.That(Empty, Is.Empty);

        string NotEmpty = GeneratorHelper.AddPrefixAndSuffixIfNotEmpty(Text, Prefix, Suffix);
        Assert.That(NotEmpty, Is.EqualTo($"{Prefix}{Text}{Suffix}"));
    }
}
