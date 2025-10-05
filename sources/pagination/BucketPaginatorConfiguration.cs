namespace AquilaSolutions.LdesServer.Pagination;

public class BucketPaginatorConfiguration
{
    public short LoopDelay { get; set; } = 3000;
    public short MemberBatchSize { get; set; } = 5000;
    public short DefaultPageSize { get; set; } = 250;
}