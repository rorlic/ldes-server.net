namespace LdesServer.Fragmentation.Models;

public class SimpleYearsPeriod(int years) : TimeBucketPeriodBase
{
    public override TimeBucket CalculateBucket(DateTimeOffset ts)
    {
        var year = ts.Year - ts.Year % years;
        return new TimeBucket(
            $"{year:0000}-01-01T00:00:00Z",
            $"{year + years:0000}-01-01T00:00:00Z",
            $"[{year:0000} .. {year + years:0000}]"
        );
    }
}
