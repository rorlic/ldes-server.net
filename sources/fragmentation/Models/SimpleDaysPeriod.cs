namespace AquilaSolutions.LdesServer.Fragmentation.Models;

public class SimpleDaysPeriod : TimeBucketPeriodBase
{
    private readonly int _days;

    public SimpleDaysPeriod(int days)
    {
        if (days is < 1 or > 27) throw new ArgumentOutOfRangeException(nameof(days));
        _days = days;
    }

    public override TimeBucket CalculateBucket(DateTimeOffset ts)
    {
        var day = ts.Day - (ts.Day - 1) % _days;
        var time = new TimeOnly(0, 0, 0);
        var from = new DateTime(new DateOnly(ts.Year, ts.Month, day), time, DateTimeKind.Utc);
        var to = from.AddDays(_days);
        return new TimeBucket(
            from.ToString(TimeBucket.Format),
            to.ToString(TimeBucket.Format),
            $"[{from.Year:0000}-{from.Month:00}-{from.Day:00} .. {to.Year:0000}-{to.Month:00}-{to.Day:00}]"
        );
    }
}