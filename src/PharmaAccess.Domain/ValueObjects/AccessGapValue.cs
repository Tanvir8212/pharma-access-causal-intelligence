namespace PharmaAccess.Domain.ValueObjects;

public readonly record struct AccessGapValue
{
    public AccessGapValue(double value)
    {
        if (!double.IsFinite(value) || value is < -100d or > 100d)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Access Gap must be finite and between -100 and 100 inclusive.");
        }

        Value = value;
    }

    public double Value { get; }
    public static implicit operator double(AccessGapValue value) => value.Value;
}
