using AquilaSolutions.LdesServer.Core.Models;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Models;

internal class BucketRecord : Bucket
{
    public required long Bid {get; set;}
    public required short Vid {get; set;}
    public PaginationStatistics? Statistics { get; set; }
}