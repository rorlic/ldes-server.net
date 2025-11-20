using LdesServer.Core.Interfaces;

namespace LdesServer.Storage.Postgres.Models;

internal class MemberId(long id) : IMember
{
    public long Id { get; } = id;
}