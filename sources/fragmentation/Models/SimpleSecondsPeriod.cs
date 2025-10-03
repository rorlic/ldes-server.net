namespace AquilaSolutions.LdesServer.Fragmentation.Models;

public class SimpleSecondsPeriod : TimeBucketPeriodBase
{
    private readonly int _seconds;

    public SimpleSecondsPeriod(int seconds)
    {
        if (seconds is < 1 or > 59) throw new ArgumentOutOfRangeException(nameof(seconds));
        _seconds = seconds;
    }

    public override TimeBucket CalculateBucket(DateTimeOffset ts)
    {
        var second = ts.Second - ts.Second % _seconds;
        var date = new DateOnly(ts.Year, ts.Month, ts.Day);
        var time = new TimeOnly(ts.Hour, ts.Minute, second);
        var from = new DateTime(date, time, DateTimeKind.Utc);
        var to = from.AddSeconds(_seconds);
        var fromString = from.ToString(TimeBucket.Format);
        var toString = to.ToString(TimeBucket.Format);
        return new TimeBucket(fromString, toString, $"[{fromString} .. {toString}]");
    }
}