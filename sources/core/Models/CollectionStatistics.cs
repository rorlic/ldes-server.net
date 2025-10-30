namespace AquilaSolutions.LdesServer.Core.Models;

public class CollectionStatistics
{
    /// <summary>
    /// The collection name
    /// </summary>
    public required string Collection { get; set; } 
    
    /// <summary>
    /// The number of ingested members for the collection
    /// </summary>
    public required long Ingested { get; set; }
}