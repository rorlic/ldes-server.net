using AquilaSolutions.LdesServer.Core.Models;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Models;

internal class BucketRecord : Bucket
{
    public required long Bid {get; set;}
    public required short Vid {get; set;}
    
    /// <summary>
    /// The last member id that is ready for pagination for the bucket (limited to a batch)
    /// </summary>
    public long? LastMid { get; set; }
}