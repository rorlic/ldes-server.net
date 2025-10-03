namespace AquilaSolutions.LdesServer.Storage.Postgres.Models;

internal class PaginationStatistics
{
    public required short Vid {get; set;}
    public required short Bid {get; set;}

    /// <summary>
    /// The last member id that has been pagination for the bucket  
    /// </summary>
    public long LastMid { get; set; }
    
    /// <summary>
    /// The total amount of paginated members for the bucket
    /// </summary>
    public long Total { get; set; }
}