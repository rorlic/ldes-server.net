namespace LdesServer.Fragmentation.Models;

public class TimeBucketPath(IEnumerable<TimeBucket> buckets) : List<TimeBucket>(buckets.ToList());