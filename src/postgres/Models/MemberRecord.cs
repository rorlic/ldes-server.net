using LdesServer.Core.Models;

namespace LdesServer.Storage.Postgres.Models;

internal class MemberRecord : Member
{
    public required long Mid { get; set; }
    public required short Cid { get; set; }
}