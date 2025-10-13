using System.Data;

namespace AquilaSolutions.LdesServer.Fragmentation;

/// <summary>
/// A common interface for fragmentation workers (i.e. bucketizers and paginators)
/// </summary>
public interface IFragmentationWorker<in TConfiguration> where TConfiguration : IFragmentationWorkerConfiguration
{
    /// <summary>
    /// Gets the worker identification
    /// </summary>
    string WorkerId { get; }

    /// <summary>
    /// Processes a fragmentation task using the given connection
    /// </summary>
    /// <param name="connection">The database connection</param>
    /// <param name="configuration">The worker configuration</param>
    /// <returns>True if any processing done, false otherwise</returns>
    Task<bool> ProcessAsync(IDbConnection connection, TConfiguration configuration);
}
