namespace AquilaSolutions.LdesServer.Core.Models;

public class Page
{
    public required string Name { get; set; }
    public required bool Root { get; set; }
    public required bool Open { get; set; }
    public required short Assigned  { get; set; }
    public DateTime UpdatedAt  { get; set; }
}