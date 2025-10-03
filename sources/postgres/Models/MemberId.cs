using AquilaSolutions.LdesServer.Core.Interfaces;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Models;

internal class MemberId(long id) : IMember
{
    public long Id { get; } = id;
}