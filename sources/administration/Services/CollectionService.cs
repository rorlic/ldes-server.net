using System.Data;
using AquilaSolutions.LdesServer.Administration.Interfaces;
using AquilaSolutions.LdesServer.Administration.Validators;
using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Models.Configuration;
using AquilaSolutions.LdesServer.Core.Namespaces;
using FluentValidation.Results;
using VDS.RDF;
using VDS.RDF.Writing;
using StringWriter = VDS.RDF.Writing.StringWriter;

namespace AquilaSolutions.LdesServer.Administration.Services;

public class CollectionService(
    LdesServerConfiguration configuration,
    IDbConnection connection,
    ICollectionRepository collectionRepository,
    IViewRepository viewRepository) : ICollectionService
{
    private static CollectionValidator CreateCollectionValidator { get; } = new();

    private static readonly ValidationFailure CannotStoreCollection =
        new(string.Empty, "Cannot store collection (see logging).");

    public async Task<ValidationResult> CreateCollectionAsync(IGraph definition)
    {
        var validationResult = await CreateCollectionValidator
            .ValidateAsync(definition)
            .ConfigureAwait(false);
        if (!validationResult.IsValid) return validationResult;

        // Note that the ArgumentException currently cannot be thrown
        // as the validation would fail above if no type EventStream is provided
        var collectionIri =
            definition.GetTriplesWithPredicateObject(definition.CreateUriNode(QNames.rdf.type),
                definition.CreateUriNode(QNames.ldes.EventStream)).SingleOrDefault()?.Subject.ToString() ??
            throw new ArgumentException("Cannot determine collection name from collection definition.");

        var defaultViewDefinition = $"@prefix tree: <https://w3id.org/tree#>.\n\n<{collectionIri}/> a tree:Node .";
        var rdfWriter = new CompressingTurtleWriter();
        var collectionDefinition = StringWriter.Write(definition, rdfWriter);
        var collectionName = collectionIri[(collectionIri.LastIndexOf('/') + 1)..];

        using var transaction = connection.BeginTransaction();
        var collection = await collectionRepository
            .CreateCollectionAsync(transaction, collectionName, collectionDefinition)
            .ConfigureAwait(false);
        var created = collection is not null;

        if (configuration.CreateEventSource && created)
        {
            var view = await viewRepository
                    .CreateViewAsync(transaction, collection!, View.DefaultName, defaultViewDefinition)
                    .ConfigureAwait(false);
            created &= view is not null;            
        }

        if (created) transaction.Commit();
        return new ValidationResult(created ? Array.Empty<ValidationFailure>() : [CannotStoreCollection]);
    }

    public async Task<bool> DeleteCollectionAsync(string collectionName)
    {
        using var transaction = connection.BeginTransaction();
        var deleted = await collectionRepository
            .DeleteCollectionAsync(transaction, collectionName)
            .ConfigureAwait(false);
        if (deleted) transaction.Commit();
        return deleted;
    }

    public async Task<IGraph?> GetCollectionAsync(string collectionName)
    {
        using var transaction = connection.BeginTransaction();
        var collection = await collectionRepository
            .GetCollectionAsync(transaction, collectionName)
            .ConfigureAwait(false);
        return collection?.ParseDefinition(configuration.GetBaseUri()).WithServerPrefixes();
    }

    public async Task<ITripleStore> GetCollectionsAsync()
    {
        using var transaction = connection.BeginTransaction();
        var collections = await collectionRepository
            .GetCollectionsAsync(transaction)
            .ConfigureAwait(false);
        var store = new SimpleTripleStore();
        collections.ToList().ForEach(x =>
        {
            var definition = x.ParseDefinition(configuration.GetBaseUri());
            var collectionUri = definition.GetSubjectByPredicateObject(
                definition.CreateUriNode("rdf:type"),
                definition.CreateUriNode("ldes:EventStream"));
            var g = new Graph(collectionUri as IUriNode)
                .WithBaseUri(definition.BaseUri).WithStandardPrefixes().WithServerPrefixes();
            g.NamespaceMap.Import(definition.NamespaceMap);
            g.Assert(definition.Triples);
            store.Add(g);
        });
        return store;
    }
}