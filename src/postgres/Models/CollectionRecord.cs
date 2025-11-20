using LdesServer.Core.Models;

namespace LdesServer.Storage.Postgres.Models;

internal class CollectionRecord : Collection
{
    public required short Cid {get; set;}
}