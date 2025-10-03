using AquilaSolutions.LdesServer.Core.Models;

namespace AquilaSolutions.LdesServer.Core.Interfaces;

public interface ICollectionRepository
{
    /// <summary>
    /// Creates a new collection with the given name and definition
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="name">Collection name</param>
    /// <param name="definition">Collection definition</param>
    /// <returns>The newly created collection or null if the collection could not be created</returns>
    Task<Collection?> CreateCollectionAsync(IDbTransaction transaction, string name, string definition);

    /// <summary>
    /// Deletes the collection with the given name
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="name">Collection name</param>
    /// <returns>True if deleted, false otherwise</returns>
    Task<bool> DeleteCollectionAsync(IDbTransaction transaction, string name);

    /// <summary>
    /// Retrieves the set of collections 
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <returns>Set of collections</returns>
    Task<IEnumerable<Collection>> GetCollectionsAsync(IDbTransaction transaction);

    /// <summary>
    /// Retrieves a collection with the given collection name
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="name">Collection name</param>
    /// <returns>The collection with the given name or null</returns>
    Task<Collection?> GetCollectionAsync(IDbTransaction transaction, string name);
}