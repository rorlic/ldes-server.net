using System.Reflection;
using AquilaSolutions.LdesServer.Core.Interfaces;
using DbUp;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Initializer;

public class StorageInitializer(NpgsqlDataSource dataSource, ILogger<StorageInitializer> logger)
    : IStorageInitializer
{
    private string ConnectionString { get; } = dataSource.ConnectionString;
    private ILogger<StorageInitializer> Logger { get; } = logger;

    private bool UpgradeDatabase()
    {
        var result = DeployChanges.To
            .PostgresqlDatabase(ConnectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build()
            .PerformUpgrade();

        var done = result.Successful;
        if (done) return true;

        Logger.LogError(result.Error, "An error occured while upgrading database.");
        return false;
    }

    public Task<bool> InitializeAsync(bool isDevelopment)
    {
        if (isDevelopment)
        {
            EnsureDatabase.For.PostgresqlDatabase(ConnectionString);
        }

        return Task.FromResult(UpgradeDatabase());
    }
}