namespace AquilaSolutions.LdesServer.Storage.Postgres.Models;

internal class BucketizationStatistics
{
    public required short Vid {get; set;}

    /// <summary>
    /// The last transaction that has been bucketized  
    /// </summary>
    public long LastTxn { get; set; }
    
    /// <summary>
    /// The total amount of bucketized members for the view
    /// </summary>
    public long Total { get; set; }
}