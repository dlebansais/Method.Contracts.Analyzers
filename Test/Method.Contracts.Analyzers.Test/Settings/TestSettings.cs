namespace Contracts.Analyzers.Test;

extern alias Helper;
extern alias Analyzers;

using NUnit.Framework;
using ContractGenerator = Analyzers::Contracts.Analyzers.ContractGenerator;
using GeneratorSettingsEntry = Helper::Contracts.Analyzers.Helper.GeneratorSettingsEntry;

[TestFixture]
public class TestSettings
{
    [Test]
    public void TestAsString()
    {
        const string TestValue = "test";
        string Value;
        bool IsDefault;

        GeneratorSettingsEntry Entry = new(EditorConfigKey: ContractGenerator.ResultIdentifierKey, DefaultValue: ContractGenerator.DefaultResultIdentifier);

        Value = Entry.StringValueOrDefault(null, out IsDefault);
        Assert.That(Value, Is.EqualTo(ContractGenerator.DefaultResultIdentifier));
        Assert.That(IsDefault, Is.True);

        Value = Entry.StringValueOrDefault(string.Empty, out IsDefault);
        Assert.That(Value, Is.EqualTo(ContractGenerator.DefaultResultIdentifier));
        Assert.That(IsDefault, Is.True);

        Value = Entry.StringValueOrDefault(TestValue, out IsDefault);
        Assert.That(Value, Is.EqualTo(TestValue));
        Assert.That(IsDefault, Is.False);
    }

    [Test]
    public void TestAsInt()
    {
        const string InvalidIntTestValue = "test";
        const int ValidIntTestValue = 1;
        int Value;
        bool IsDefault;

        GeneratorSettingsEntry Entry = new(EditorConfigKey: ContractGenerator.TabLengthKey, DefaultValue: $"{ContractGenerator.DefaultTabLength}");

        Value = Entry.IntValueOrDefault(null, out IsDefault);
        Assert.That(Value, Is.EqualTo(ContractGenerator.DefaultTabLength));
        Assert.That(IsDefault, Is.True);

        Value = Entry.IntValueOrDefault(string.Empty, out IsDefault);
        Assert.That(Value, Is.EqualTo(ContractGenerator.DefaultTabLength));
        Assert.That(IsDefault, Is.True);

        Value = Entry.IntValueOrDefault(InvalidIntTestValue, out IsDefault);
        Assert.That(Value, Is.EqualTo(ContractGenerator.DefaultTabLength));
        Assert.That(IsDefault, Is.True);

        Value = Entry.IntValueOrDefault($"{ValidIntTestValue}", out IsDefault);
        Assert.That(Value, Is.EqualTo(ValidIntTestValue));
        Assert.That(IsDefault, Is.False);
    }
}
