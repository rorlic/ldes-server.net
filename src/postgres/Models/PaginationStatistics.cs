namespace LdesServer.Storage.Postgres.Models;

internal class PaginationStatistics
{
    public required short Vid {get; set;}
    
    /// <summary>
    /// The total amount of paginated members for the view
    /// </summary>
    public long Total { get; set; }
    
    /// <summary>
     /// The first member id that is ready for pagination for the view  
     /// </summary>
     public long FirstMid { get; set; }
}