namespace Contracts.Analyzers.Test;

extern alias Helper;
extern alias Analyzers;

using NUnit.Framework;
using AssignTrackingString = Helper::Contracts.Analyzers.Helper.AssignTrackingString;

[TestFixture]
internal class TestAssignTrackingString
{
    [Test]
    public void TestEmpty()
    {
        AssignTrackingString Empty = new();

        Assert.That(Empty.Value, Is.Empty);
        Assert.That(Empty.IsSet, Is.False);

        AssignTrackingString OtherEmpty = Empty with { };

        Assert.That(OtherEmpty.Value, Is.Empty);
        Assert.That(OtherEmpty.IsSet, Is.False);
    }

    [Test]
    public void TestAssignment()
    {
        AssignTrackingString NotEmpty = (AssignTrackingString)"Test";

        Assert.That(NotEmpty.Value, Is.Not.Empty);
        Assert.That(NotEmpty.IsSet, Is.True);

        AssignTrackingString OtherNotEmpty = NotEmpty with { };

        Assert.That(OtherNotEmpty.Value, Is.Not.Empty);
        Assert.That(OtherNotEmpty.IsSet, Is.True);
    }
}
