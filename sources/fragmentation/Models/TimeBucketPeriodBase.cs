namespace AquilaSolutions.LdesServer.Fragmentation.Models;

public abstract class TimeBucketPeriodBase : ITimeBucketPeriod
{
    public abstract TimeBucket CalculateBucket(DateTimeOffset ts);

    public static TimeBucketPeriodBase From(string period)
    {
        if (period.StartsWith("PT"))
        {
            var value = int.Parse(period.Substring(2, period.Length - 3));
            var unit = period.Last();
            return unit switch
            {
                'H' => new SimpleHoursPeriod(value),
                'M' => new SimpleMinutesPeriod(value),
                'S' => new SimpleSecondsPeriod(value),
                _ => throw new Exception($"Invalid time period: {period}")
            };
        }
        else
        {
            var value = int.Parse(period.Substring(1, period.Length - 2));
            var unit = period.Last();
            return unit switch
            {
                'Y' => new SimpleYearsPeriod(value),
                'M' => new SimpleMonthsPeriod(value),
                'D' => new SimpleDaysPeriod(value),
                _ => throw new Exception($"Invalid date period: {period}")
            };
        }
    }
}