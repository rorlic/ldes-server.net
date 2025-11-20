using LdesServer.Core.Models;

namespace LdesServer.Storage.Postgres.Models;

internal class PageRecord : Page
{
    public required long Pid {get; set;}
    public required long Bid {get; set;}
    public required long Vid {get; set;}
}