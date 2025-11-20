namespace LdesServer.Fragmentation.Models;

public interface ITimeBucketPeriod
{
    TimeBucket CalculateBucket(DateTimeOffset ts);
}