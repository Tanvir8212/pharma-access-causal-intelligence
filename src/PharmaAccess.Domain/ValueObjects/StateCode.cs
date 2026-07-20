using PharmaAccess.Domain.Common;

namespace PharmaAccess.Domain.ValueObjects;

public readonly record struct StateCode
{
    public StateCode(string value)
    {
        var normalized = DomainGuard.RequiredText(value, 2, nameof(value)).ToUpperInvariant();
        if (normalized.Length != 2 || !normalized.All(char.IsAsciiLetter))
        {
            throw new ArgumentException("State code must contain exactly two ASCII letters.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }
    public override string ToString() => Value;
    public static implicit operator string(StateCode value) => value.Value;
}
