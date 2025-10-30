using AquilaSolutions.LdesServer.Core.Models;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Models;

internal class CollectionStatisticsRecord : CollectionStatistics
{
    public required short Cid {get; set;}
}