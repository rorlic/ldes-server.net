using AquilaSolutions.LdesServer.Core.Models;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Models;

internal class ViewRecord : View
{
    public required short Cid {get; set;}
    public required short Vid {get; set;}
    public BucketizationStatistics? Statistics { get; set; }
}