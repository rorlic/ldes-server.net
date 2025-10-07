using System.Data;
using AquilaSolutions.LdesServer.Administration.Interfaces;
using AquilaSolutions.LdesServer.Administration.Validators;
using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Namespaces;
using FluentValidation.Results;
using VDS.RDF;
using VDS.RDF.Writing;
using StringWriter = VDS.RDF.Writing.StringWriter;

namespace AquilaSolutions.LdesServer.Administration.Services;

public class ViewService(
    IDbConnection connection,
    ICollectionRepository collectionRepository,
    IViewRepository viewRepository) : IViewService
{
    private static ViewValidator CreateViewValidator { get; } = new();

    private static readonly ValidationFailure CannotStoreView =
        new(string.Empty, "Cannot store view (see logging).");

    private static readonly ValidationFailure CannotUseDefaultViewName =
        new(string.Empty, $"You cannot name a view '{View.DefaultName}' as this is reserved for the default view name.");

    public async Task<ValidationResult?> CreateViewAsync(IGraph definition, string collectionName)
    {
        using var transaction = connection.BeginTransaction();

        var collection = await collectionRepository
            .GetCollectionAsync(transaction, collectionName)
            .ConfigureAwait(false);
        if (collection is null) return null;

        var validationResult = await CreateViewValidator
            .ValidateAsync(definition)
            .ConfigureAwait(false);
        if (!validationResult.IsValid) return validationResult;

        var viewTriple =
            definition.GetTriplesWithPredicateObject(definition.CreateUriNode(QNames.rdf.type),
                definition.CreateUriNode(QNames.tree.Node)).SingleOrDefault()?.Subject ??
            definition.GetTriplesWithPredicate(definition.CreateUriNode(QNames.tree.viewDescription)).SingleOrDefault()
                ?.Subject ??
            definition.Nodes.OfType<IUriNode>().SingleOrDefault() ??
            throw new ArgumentException("Cannot determine view name from view definition.");

        var viewIri = viewTriple.ToString();
        var viewName = viewIri[(viewIri.LastIndexOf('/') + 1)..];

        if (viewName == View.DefaultName) 
            return new ValidationResult([CannotUseDefaultViewName]);

        var rdfWriter = new CompressingTurtleWriter();
        var viewDefinition = StringWriter.Write(definition, rdfWriter);

        var view = await viewRepository
            .CreateViewAsync(transaction, collection, viewName, viewDefinition)
            .ConfigureAwait(false);
        var created = view is not null;
        if (created) transaction.Commit();
        return new ValidationResult(created ? Array.Empty<ValidationFailure>() : [CannotStoreView]);
    }

    public async Task<bool> DeleteViewAsync(string collectionName, string viewName)
    {
        using var transaction = connection.BeginTransaction();

        var collection = await collectionRepository
            .GetCollectionAsync(transaction, collectionName)
            .ConfigureAwait(false);
        if (collection is null) return false;

        var deleted = await viewRepository
            .DeleteViewAsync(transaction, collection, viewName)
            .ConfigureAwait(false);
        if (deleted) transaction.Commit();
        return deleted;
    }

    public async Task<IGraph?> GetViewAsync(string collectionName, string viewName)
    {
        using var transaction = connection.BeginTransaction();

        var collection = await collectionRepository
            .GetCollectionAsync(transaction, collectionName)
            .ConfigureAwait(false);
        if (collection is null) return null;

        var view = await viewRepository
            .GetCollectionViewAsync(transaction, collection, viewName)
            .ConfigureAwait(false);
        return view?.ParseDefinition().WithServerPrefixes();
    }

    public async Task<ITripleStore?> GetViewsAsync(string collectionName)
    {
        using var transaction = connection.BeginTransaction();

        var collection = await collectionRepository
            .GetCollectionAsync(transaction, collectionName)
            .ConfigureAwait(false);
        if (collection is null) return null;

        var views = await viewRepository
            .GetCollectionViewsAsync(transaction, collection)
            .ConfigureAwait(false);
        var store = new SimpleTripleStore();
        views.ToList().ForEach(x =>
        {
            var definition = x.ParseDefinition();
            var viewUri = definition.GetSubjectByPredicateObject(
                definition.CreateUriNode("rdf:type"),
                definition.CreateUriNode("tree:Node"));
            var g = new Graph(viewUri as IUriNode).WithStandardPrefixes().WithServerPrefixes();
            g.Assert(definition.Triples);
            store.Add(g);
        });
        return store;
    }
}