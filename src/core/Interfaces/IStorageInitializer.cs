namespace LdesServer.Core.Interfaces;

public interface IStorageInitializer
{
    /// <summary>
    /// Prepares the storage for the LDES server by running database migrations.
    /// In addition, for development only, it ensures that the database exists, creating it if necessary.
    /// </summary>
    /// <param name="isDevelopment">True if running in development, false otherwise</param>
    /// <returns>True if successfully initialized, false otherwise</returns>
    Task<bool> InitializeAsync(bool isDevelopment);
}