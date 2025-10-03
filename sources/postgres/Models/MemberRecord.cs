using AquilaSolutions.LdesServer.Core.Models;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Models;

internal class MemberRecord : Member
{
    public required long Mid { get; set; }
    public required short Cid { get; set; }
}