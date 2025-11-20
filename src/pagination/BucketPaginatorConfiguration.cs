using LdesServer.Fragmentation;

namespace LdesServer.Pagination;

public class BucketPaginatorConfiguration : IFragmentationWorkerConfiguration
{
    public short? LoopDelay { get; set; }
    public short MemberBatchSize { get; set; } = 5000;
    public short DefaultPageSize { get; set; } = 250;
}