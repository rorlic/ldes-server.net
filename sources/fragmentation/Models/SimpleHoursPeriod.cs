namespace AquilaSolutions.LdesServer.Fragmentation.Models;

public class SimpleHoursPeriod : TimeBucketPeriodBase
{
    private readonly int _hours;

    public SimpleHoursPeriod(int hours)
    {
        if (hours is < 1 or > 23) throw new ArgumentOutOfRangeException(nameof(hours));
        _hours = hours;
    }

    public override TimeBucket CalculateBucket(DateTimeOffset ts)
    {
        var hour = ts.Hour - ts.Hour % _hours;
        var date = new DateOnly(ts.Year, ts.Month, ts.Day);
        var time = new TimeOnly(hour, 0, 0);
        var from = new DateTime(date, time, DateTimeKind.Utc);
        var to = from.AddHours(_hours);
        var fromString = from.ToString(TimeBucket.Format);
        var toString = to.ToString(TimeBucket.Format);
        return new TimeBucket(fromString, toString, $"[{fromString} .. {toString}]");
    }
}