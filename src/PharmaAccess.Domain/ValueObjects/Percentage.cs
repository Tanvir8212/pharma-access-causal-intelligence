namespace PharmaAccess.Domain.ValueObjects;

public readonly record struct Percentage
{
    public Percentage(double value)
    {
        if (!double.IsFinite(value) || value is < 0d or > 100d)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be finite and between 0 and 100 inclusive.");
        }

        Value = value;
    }

    public double Value { get; }
    public static implicit operator double(Percentage value) => value.Value;
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
