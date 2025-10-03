using AquilaSolutions.LdesServer.Core.Interfaces;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Models;

internal class TransactionId(long id) : IMemberSet
{
    public long Id { get; } = id;
}