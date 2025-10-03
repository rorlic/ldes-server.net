namespace AquilaSolutions.LdesServer.Fragmentation.Models;

public class SimpleMonthsPeriod : TimeBucketPeriodBase
{
    private readonly int _months;

    public SimpleMonthsPeriod(int months)
    {
        if (months is < 1 or > 11) throw new ArgumentOutOfRangeException(nameof(months));
        _months = months;
    }

    public override TimeBucket CalculateBucket(DateTimeOffset ts)
    {
        var month = ts.Month - (ts.Month - 1) % _months;
        var time = new TimeOnly(0, 0, 0);
        var from = new DateTime(new DateOnly(ts.Year, month, 1), time, DateTimeKind.Utc);
        var to = from.AddMonths(_months);
        return new TimeBucket(
            from.ToString(TimeBucket.Format),
            to.ToString(TimeBucket.Format),
            $"[{from.Year:0000}-{from.Month:00} .. {to.Year:0000}-{to.Month:00}]"
        );
    }
}