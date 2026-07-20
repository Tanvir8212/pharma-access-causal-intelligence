using System.Globalization;

namespace PharmaAccess.Domain.ValueObjects;

public readonly record struct CalendarQuarter : IComparable<CalendarQuarter>
{
    public CalendarQuarter(int calendarYear, int quarterNumber)
    {
        if (calendarYear is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(calendarYear));
        }

        if (quarterNumber is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(quarterNumber));
        }

        CalendarYear = calendarYear;
        QuarterNumber = quarterNumber;
    }

    public int CalendarYear { get; }
    public int QuarterNumber { get; }
    public DateOnly StartDate => new(CalendarYear, ((QuarterNumber - 1) * 3) + 1, 1);
    public DateOnly EndDate => StartDate.AddMonths(3).AddDays(-1);

    public static CalendarQuarter FromDate(DateOnly date) => new(date.Year, ((date.Month - 1) / 3) + 1);

    public CalendarQuarter AddQuarters(int quarters)
    {
        var zeroBased = checked(((CalendarYear - 1) * 4) + QuarterNumber - 1 + quarters);
        if (zeroBased < 0 || zeroBased > (9999 * 4) - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quarters));
        }

        return new CalendarQuarter((zeroBased / 4) + 1, (zeroBased % 4) + 1);
    }

    public int DistanceTo(CalendarQuarter other) => checked(((other.CalendarYear - CalendarYear) * 4) + other.QuarterNumber - QuarterNumber);

    public int CompareTo(CalendarQuarter other)
    {
        var yearComparison = CalendarYear.CompareTo(other.CalendarYear);
        return yearComparison != 0 ? yearComparison : QuarterNumber.CompareTo(other.QuarterNumber);
    }

    public static CalendarQuarter Parse(string value)
    {
        if (!TryParse(value, out var quarter))
        {
            throw new FormatException("Quarter must use the controlled YYYY-QN format.");
        }

        return quarter;
    }

    public static bool TryParse(string? value, out CalendarQuarter quarter)
    {
        quarter = default;
        if (value is null || value.Length != 7 || value[4] != '-' || value[5] != 'Q')
        {
            return false;
        }

        if (!int.TryParse(value.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
            !int.TryParse(value.AsSpan(6, 1), NumberStyles.None, CultureInfo.InvariantCulture, out var number) ||
            year < 1 || number is < 1 or > 4)
        {
            return false;
        }

        quarter = new CalendarQuarter(year, number);
        return true;
    }

    public override string ToString() => $"{CalendarYear:D4}-Q{QuarterNumber}";

    public static bool operator <(CalendarQuarter left, CalendarQuarter right) => left.CompareTo(right) < 0;
    public static bool operator >(CalendarQuarter left, CalendarQuarter right) => left.CompareTo(right) > 0;
    public static bool operator <=(CalendarQuarter left, CalendarQuarter right) => left.CompareTo(right) <= 0;
    public static bool operator >=(CalendarQuarter left, CalendarQuarter right) => left.CompareTo(right) >= 0;
}
