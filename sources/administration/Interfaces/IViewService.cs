using FluentValidation.Results;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Administration.Interfaces;

public interface IViewService
{
    /// <summary>
    /// Creates a new view for the given collection, unless it already exists and after validating the definition.
    /// In addition, it creates a default bucket and a root page for the view.
    /// </summary>
    /// <param name="data">The view definition</param>
    /// <param name="collectionName">The collection name</param>
    /// <returns>The validation result, containing 0 or more validation failures</returns>
    Task<ValidationResult?> CreateViewAsync(IGraph data, string collectionName);
    
    /// <summary>
    /// Deletes the view with the given name for the given collection.
    /// </summary>
    /// <param name="collectionName">The collection name</param>
    /// <param name="viewName">The view name</param>
    /// <returns>True if deleted, false otherwise</returns>
    Task<bool> DeleteViewAsync(string collectionName, string viewName);

    /// <summary>
    /// Retrieves the view with the given name for the given collection.
    /// </summary>
    /// <param name="collectionName">The collection name</param>
    /// <param name="viewName">The view name</param>
    /// <returns>The view definition or null</returns>
    Task<IGraph?> GetViewAsync(string collectionName, string viewName);
    
    /// <summary>
    /// Retrieves all views for the given collection.
    /// </summary>
    /// <param name="collectionName">The collection name</param>
    /// <returns>A store containing each view definition in a named graph</returns>
    Task<ITripleStore?> GetViewsAsync(string collectionName);
}