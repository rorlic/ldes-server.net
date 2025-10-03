using AquilaSolutions.LdesServer.Core.Models;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Models;

internal class CollectionRecord : Collection
{
    public required short Cid {get; set;}
}