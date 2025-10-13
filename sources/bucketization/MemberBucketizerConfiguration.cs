using AquilaSolutions.LdesServer.Fragmentation;

namespace AquilaSolutions.LdesServer.Bucketization;

public class MemberBucketizerConfiguration : IFragmentationWorkerConfiguration
{
    /// <summary>
    /// Gets or sets the delay between two loops of member bucketization, or NULL for a one-off bucketization.
    /// </summary>
    public short? LoopDelay { get; set; }
    
    /// <summary>
    /// Gets or sets the number of members to bucketize at a time.
    /// The bucketization process continues while there are members to bucketize, and
    /// no other bucketization process is running on the same collection. 
    /// </summary>
    public short MemberBatchSize { get; set; } = 3000;
}