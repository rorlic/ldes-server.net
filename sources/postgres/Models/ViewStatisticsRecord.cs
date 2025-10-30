using AquilaSolutions.LdesServer.Core.Models;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Models;

internal class ViewStatisticsRecord : ViewStatistics
{
    public required short Cid {get; set;}
    public required short Vid {get; set;}
}