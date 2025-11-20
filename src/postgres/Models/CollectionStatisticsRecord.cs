using LdesServer.Core.Models;

namespace LdesServer.Storage.Postgres.Models;

internal class CollectionStatisticsRecord : CollectionStatistics
{
    public required short Cid {get; set;}
}