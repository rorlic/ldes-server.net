using System.Data;
using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Models.Configuration;
using AquilaSolutions.LdesServer.Core.Namespaces;
using AquilaSolutions.LdesServer.Serving.Extensions;
using Microsoft.Extensions.DependencyInjection;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Serving.Services;

public class NodeService(
    ServingConfiguration servingConfiguration,
    IServiceProvider serviceProvider,
    ICollectionRepository collectionRepository,
    IViewRepository viewRepository,
    IPageRepository pageRepository)
{
    private IGraph CreateDefaultGraph()
    {
        var configuration = serviceProvider.GetRequiredService<LdesServerConfiguration>();
        var g = new Graph().WithBaseUri(configuration.GetBaseUri()).WithStandardPrefixes();
        return g;
    }

    private static IUriNode AddEventStream(IGraph g, IGraph collectionDefinition)
    {
        g.NamespaceMap.Import(collectionDefinition.NamespaceMap);
        g.WithTriples(collectionDefinition.Triples);
        
        var eventStream = g.GetSubjectByPredicateObject(QNames.rdf.type, QNames.ldes.EventStream).AsUriNode();
        var timestampPath = g.FindOneByQNamePredicate(QNames.ldes.timestampPath)?.Object?.AsUriNode();
        if (timestampPath is null)
            g.Assert(new Triple(eventStream, g.CreateUriNode(QNames.ldes.timestampPath), g.CreateUriNode(QNames.prov.generatedAtTime)));
        
        var versionOfPath = g.FindOneByQNamePredicate(QNames.ldes.versionOfPath)?.Object?.AsUriNode();
        if (versionOfPath is null)
            g.Assert(new Triple(eventStream, g.CreateUriNode(QNames.ldes.versionOfPath), g.CreateUriNode(QNames.dct.isVersionOf)));

        return eventStream;
    }

    public async Task<LdesNode?> GetEventStreamAsync(string collectionName)
    {
        using var connection = serviceProvider.GetRequiredService<IDbConnection>();
        using var transaction = connection.BeginTransaction();

        var collection = await collectionRepository
            .GetCollectionAsync(transaction, collectionName)
            .ConfigureAwait(false);
        if (collection is null) return null;

        var g = CreateDefaultGraph();
        var collectionDefinition = collection.ParseDefinition(g.BaseUri);
        var eventStream = AddEventStream(g, collectionDefinition);

        var views = (await viewRepository.GetCollectionViewsAsync(transaction, collection)).ToArray();
        var viewNodes = views
            .Select(x => g.CreateUriNode(new Uri($"{collectionName}/{x.Name}", UriKind.Relative)))
            .ToArray();
        g.WithViewReferences(eventStream, viewNodes);

        return CreateLdesNode(g, [], true, DateTime.UtcNow);
    }

    public async Task<LdesNode?> GetViewAsync(string collectionName, string viewName)
    {
        using var connection = serviceProvider.GetRequiredService<IDbConnection>();
        using var transaction = connection.BeginTransaction();

        var collection = await collectionRepository
            .GetCollectionAsync(transaction, collectionName)
            .ConfigureAwait(false);
        if (collection is null) return null;

        var view = await viewRepository
            .GetCollectionViewAsync(transaction, collection, viewName)
            .ConfigureAwait(false);
        if (view is null) return null;

        var page = await pageRepository.GetDefaultBucketRootPageAsync(transaction, view);
        var g = CreateDefaultGraph();

        // Add node (page)
        var pagePrefix = $"{collectionName}/{viewName}/";
        var pageNode = g.CreateUriNode(new Uri($"{collectionName}/{viewName}", UriKind.Relative));
        var relations = await pageRepository.GetPageRelationsAsync(transaction, page).ConfigureAwait(false);
        g.WithNode(pageNode).WithNodeRelations(pageNode, pagePrefix, relations);
        if (viewName == View.DefaultName) g.WithEventSource(pageNode);

        // add event stream (collection)
        var collectionDefinition = collection.ParseDefinition(g.BaseUri);
        g.WithViewReference(AddEventStream(g, collectionDefinition), pageNode);

        // Add members as-is and let the LDES node format itself, e.g. for TREE profile specification (https://treecg.github.io/specification/profile) 
        var members = await pageRepository.GetPageMembersAsync(transaction, page).ConfigureAwait(false);

        return CreateLdesNode(g, members, page.Open, page.UpdatedAt);
    }

    public async Task<LdesNode?> GetPageAsync(string collectionName, string viewName, string pageName)
    {
        using var connection = serviceProvider.GetRequiredService<IDbConnection>();
        using var transaction = connection.BeginTransaction();

        var collection = await collectionRepository.GetCollectionAsync(transaction, collectionName)
            .ConfigureAwait(false);
        if (collection is null) return null;

        var page = await pageRepository.GetPageAsync(transaction, collection, viewName, pageName).ConfigureAwait(false);
        if (page is null) return null;

        var g = CreateDefaultGraph();

        // Add node (page)
        var pagePrefix = $"{collectionName}/{viewName}/";
        var pageNode = g.CreateUriNode(new Uri($"{pagePrefix}{pageName}", UriKind.Relative));
        var relations = await pageRepository.GetPageRelationsAsync(transaction, page).ConfigureAwait(false);
        g.WithNode(pageNode).WithNodeRelations(pageNode, pagePrefix, relations);

        // add event stream (collection)
        var collectionDefinition = collection.ParseDefinition(g.BaseUri);
        g.WithViewReference(AddEventStream(g, collectionDefinition), pageNode);

        // Add members as-is and let the LDES node format itself, e.g. for TREE profile specification (https://treecg.github.io/specification/profile) 
        var members = await pageRepository.GetPageMembersAsync(transaction, page).ConfigureAwait(false);

        return CreateLdesNode(g, members, page.Open, page.UpdatedAt);
    }

    private LdesNode CreateLdesNode(IGraph g, IEnumerable<Member> members, bool open, DateTime updatedAt)
    {
        var maxAge = open ? servingConfiguration.MaxAge : servingConfiguration.MaxAgeImmutable;
        return new LdesNode(g.WithoutCustomDefinition(), members,
            new LdesNode.Info(updatedAt, TimeSpan.FromSeconds(maxAge), open));
    }
}