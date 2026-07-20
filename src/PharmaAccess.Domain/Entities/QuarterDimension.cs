using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Domain.Entities;

public sealed class QuarterDimension
{
    private QuarterDimension() { }

    public QuarterDimension(CalendarQuarter quarter)
    {
        CalendarYear = quarter.CalendarYear;
        QuarterNumber = quarter.QuarterNumber;
        QuarterStartDate = quarter.StartDate;
        QuarterEndDate = quarter.EndDate;
        DisplayCode = quarter.ToString();
    }

    public int QuarterId { get; private set; }
    public int CalendarYear { get; private set; }
    public int QuarterNumber { get; private set; }
    public DateOnly QuarterStartDate { get; private set; }
    public DateOnly QuarterEndDate { get; private set; }
    public string DisplayCode { get; private set; } = null!;
    public CalendarQuarter ToValueObject() => new(CalendarYear, QuarterNumber);
}
