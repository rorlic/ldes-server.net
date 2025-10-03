using FluentValidation.Results;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Administration.Interfaces;

public interface ICollectionService
{
    /// <summary>
    /// Creates a new collection with the given definition, unless it already exists and after validating the definition.
    /// In addition, it creates an event source for the collection, unless configured to not do so.
    /// </summary>
    /// <param name="definition">The collection definition</param>
    /// <returns>The validation result, containing 0 or more validation failures</returns>
    Task<ValidationResult> CreateCollectionAsync(IGraph definition);
    
    /// <summary>
    /// Deletes the collection with the given name.
    /// </summary>
    /// <param name="collectionName">The collection name</param>
    /// <returns>True if deleted, false otherwise</returns>
    Task<bool> DeleteCollectionAsync(string collectionName);
    
    /// <summary>
    /// Retrieves the collection with the given name.
    /// </summary>
    /// <param name="collectionName">The collection name</param>
    /// <returns>The collection definition or null</returns>
    Task<IGraph?> GetCollectionAsync(string collectionName);
    
    /// <summary>
    /// Retrieves all collections.
    /// </summary>
    /// <returns>A store containing each collection definition in a named graph</returns>
    Task<ITripleStore> GetCollectionsAsync();
}