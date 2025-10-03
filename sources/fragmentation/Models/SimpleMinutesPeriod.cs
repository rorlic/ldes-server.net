namespace AquilaSolutions.LdesServer.Fragmentation.Models;

public class SimpleMinutesPeriod : TimeBucketPeriodBase
{
    private readonly int _minutes;

    public SimpleMinutesPeriod(int minutes)
    {
        if (minutes is < 1 or > 59) throw new ArgumentOutOfRangeException(nameof(minutes));
        _minutes = minutes;
    }

    public override TimeBucket CalculateBucket(DateTimeOffset ts)
    {
        var minute = ts.Minute - ts.Minute % _minutes;
        var date = new DateOnly(ts.Year, ts.Month, ts.Day);
        var time = new TimeOnly(ts.Hour, minute, 0);
        var from = new DateTime(date, time, DateTimeKind.Utc);
        var to = from.AddMinutes(_minutes);
        var fromString = from.ToString(TimeBucket.Format);
        var toString = to.ToString(TimeBucket.Format);
        return new TimeBucket(fromString, toString, $"[{fromString} .. {toString}]");
    }
}