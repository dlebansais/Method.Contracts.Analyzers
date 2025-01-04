namespace Contracts.Analyzers.Helper;

/// <summary>
/// Represents a string that keeps track of its assigned state.
/// </summary>
internal record AssignTrackingString
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssignTrackingString"/> class.
    /// </summary>
    public AssignTrackingString()
    {
        Value = string.Empty;
        IsSet = false;

        Contract.Assert(IsConsistent);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssignTrackingString"/> class.
    /// </summary>
    /// <param name="value">The string value.</param>
    public AssignTrackingString(string value)
    {
        Value = value;
        IsSet = true;

        Contract.Assert(IsConsistent);
    }

    public static implicit operator string(AssignTrackingString s)
    {
        return s.Value;
    }

    public static explicit operator AssignTrackingString(string s)
    {
        return new AssignTrackingString(s);
    }

    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the string has been assigned.
    /// </summary>
    public bool IsSet { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the string is consistent.
    /// </summary>
    public bool IsConsistent => IsSet == (Value != string.Empty);
}
