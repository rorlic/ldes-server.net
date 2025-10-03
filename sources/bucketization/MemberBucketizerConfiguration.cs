namespace AquilaSolutions.LdesServer.Bucketization;

public class MemberBucketizerConfiguration
{
    /// <summary>
    /// Gets or sets the delay between two loops of member bucketization.
    /// </summary>
    public short LoopDelay { get; set; } = 2000;
    
    /// <summary>
    /// Gets or sets the number of members to bucketize at a time.
    /// The bucketization process continues while there are members to bucketize, and
    /// no other bucketization process is running on the same collection. 
    /// </summary>
    public short MemberBatchSize { get; set; } = 3000;
}