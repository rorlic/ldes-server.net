using LdesServer.Core.Interfaces;

namespace LdesServer.Storage.Postgres.Models;

internal class TransactionId(long id) : IMemberSet
{
    public long Id { get; } = id;
}