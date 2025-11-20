namespace LdesServer.Core.Models;

public class ViewStatistics
{
    /// <summary>
    /// The collection name
    /// </summary>
    public required string Collection { get; set; }
    
    /// <summary>
    ///  The view name
    /// </summary>
    public required string View { get; set; }
    
    /// <summary>
    /// The number of bucketized members
    /// </summary>
    public required long Bucketized { get; set; }
    
    /// <summary>
    /// The number of paginated members
    /// </summary>
    public required long Paginated { get; set; }
}