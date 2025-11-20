namespace LdesServer.Core.Models;

public class PageRelation
{
    public string? Link { get; set; } // page-name 
    public string? Type { get; set; } // tree:Relation or the name of the specialized subclass 
    public string? Path { get; set; } // tree:path => predicate path
    public string? Value { get; set; } // tree:value => literal or uri
}