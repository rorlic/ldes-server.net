using AquilaSolutions.LdesServer.Administration.Interfaces;
using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Models.Configuration;
using AquilaSolutions.LdesServer.Core.Namespaces;
using Microsoft.Extensions.Logging;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace AquilaSolutions.LdesServer.Administration.Initializer;

public class DefinitionsInitializer(
    LdesServerConfiguration configuration,
    ICollectionService collectionService,
    IViewService viewService,
    ILogger<DefinitionsInitializer> logger
)
{
    private static readonly IDictionary<string, IStoreReader> QuadReaders = new Dictionary<string, IStoreReader>
    {
        { ".jsonld", new JsonLdParser() },
        { ".nq", new NQuadsParser() },
        { ".trig", new TriGParser() }
    };

    private static readonly IDictionary<string, IRdfReader> TripleReaders = new Dictionary<string, IRdfReader>
    {
        { ".nt", new NTriplesParser() },
        { ".ttl", new TurtleParser() }
    };

    private static readonly string[] AllExtensions = TripleReaders.Keys.Concat(QuadReaders.Keys).ToArray();

    private record LinkedDataFile(string Path, IGraph Graph);

    private enum KindType
    {
        Unknown,
        Collection,
        View
    }

    private abstract class Definition(string path, IGraph graph)
    {
        public string Path { get; } = path;
        public IGraph Graph { get; } = graph;
        public abstract KindType Kind { get; }
    }

    private class UnknownDefinition(string path, IGraph graph) : Definition(path, graph)
    {
        public override KindType Kind => KindType.Unknown;
    }

    private class CollectionDefinition(string path, IGraph graph, IUriNode collection) : Definition(path, graph)
    {
        public IUriNode Collection { get; } = collection;
        public override KindType Kind => KindType.Collection;
    }

    private class ViewDefinition(string path, IGraph graph, IUriNode collection, IUriNode view)
        : Definition(path, graph)
    {
        public IUriNode Collection { get; } = collection;
        public IUriNode View { get; } = view;
        public override KindType Kind => KindType.View;
    }

    public async Task Seed(IEnumerable<FileInfo> paths)
    {
        var baseUri = new Uri(configuration.BaseUri);
        var definitionsByKind = paths
            .Select(x =>
            {
                if (TripleReaders.TryGetValue(x.Extension, out var triplesReader))
                    return FromTriples(x, triplesReader, baseUri);

                if (QuadReaders.TryGetValue(x.Extension, out var quadsReader))
                    return FromQuads(x, quadsReader, baseUri);

                logger.LogInformation(
                    $"Cannot seed file '{x.FullName}' because of unknown extension, use one of the following extensions: {string.Join(",", AllExtensions)}");
                return null;
            })
            .Where(x => x != null)
            .Select(x => CreateDefinition(x!))
            .GroupBy(x => x.Kind)
            .OrderBy(x => x.Key)
            .ToList();

        var existingCollections = (await collectionService.GetCollectionsAsync().ConfigureAwait(false))
            .Graphs.Select(x => x.Name)
            .ToArray();

        foreach (var definitions in definitionsByKind)
        {
            switch (definitions.Key)
            {
                case KindType.Unknown:
                {
                    definitions.ToList().ForEach(x =>
                    {
                        x.Graph.Dispose();
                        logger.LogWarning(
                            $"Cannot seed file '{x.Path}' because it does not appear to contain a collection or view.");
                    });

                    break;
                }
                case KindType.Collection:
                {
                    foreach (var definition in definitions.Cast<CollectionDefinition>())
                    {
                        var graph = definition.Graph;
                        try
                        {
                            if (!existingCollections.Contains(definition.Collection))
                                await CreateCollectionAsync(graph, definition);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Cannot seed file '{definition.Path}' because of error: {ex.Message}");
                        }
                        finally
                        {
                            graph.Dispose();
                        }
                    }

                    break;
                }
                case KindType.View:
                {
                    foreach (var definition in definitions.Cast<ViewDefinition>())
                    {
                        var graph = definition.Graph;
                        var collectionName = definition.Collection.ToString().Split("/").Last();
                        var existingViews = 
                            (await viewService.GetViewsAsync(collectionName).ConfigureAwait(false) ?? new TripleStore())
                            .Graphs
                            .Select(x => x.Name)
                            .ToArray();
                        
                        try
                        {
                            if (!existingViews.Contains(definition.View))
                                await CreateViewAsync(graph, definition);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Cannot seed file '{definition.Path}' because of error: {ex.Message}");
                        }
                        finally
                        {
                            graph.Dispose();
                        }
                    }

                    break;
                }
                default: throw new ArgumentOutOfRangeException($"Unknown kind: {definitions.Key}");
            }
        }
    }

    private static LinkedDataFile FromQuads(FileInfo x, IStoreReader quadsReader, Uri baseUri)
    {
        using var reader = new StreamReader(x.OpenRead());
        var graph = new Graph().WithBaseUri(baseUri);
        var store = new SimpleTripleStore();
        store.Add(graph);
        quadsReader.Load(store, reader);
        
        if (store.Graphs.Count > 1)
            throw new Exception("No named graphs are allowed in collection and view definitions.");
        
        return new LinkedDataFile(x.FullName, graph);
    }

    private static LinkedDataFile FromTriples(FileInfo x, IRdfReader triplesReader, Uri baseUri)
    {
        using var reader = new StreamReader(x.OpenRead());
        var graph = new Graph().WithBaseUri(baseUri);
        triplesReader.Load(graph, reader);
        return new LinkedDataFile(x.FullName, graph);
    }

    private static Definition CreateDefinition(LinkedDataFile rdf)
    {
        var graph = rdf.Graph;
        var collectionTriple = graph.GetTriplesWithPredicateObject(
                graph.CreateUriNode(QNames.rdf.type), graph.CreateUriNode(QNames.ldes.EventStream))
            .SingleOrDefault();
        var collection = collectionTriple?.Subject.AsUriNode();
        if (collection != null)
            return new CollectionDefinition(rdf.Path, graph, collection);

        var viewTriple = graph.GetTriplesWithPredicateObject(
            graph.CreateUriNode(QNames.rdf.type), graph.CreateUriNode(QNames.tree.Node)).SingleOrDefault();

        if (viewTriple != null)
        {
            var view = viewTriple.Subject.AsUriNode();
            var viewUri = view.ToString();
            var viewName = viewUri.Split('/').Last();
            collection = graph.CreateUriNode(new Uri(viewUri.Replace(viewName, "").TrimEnd('/')));
            return new ViewDefinition(rdf.Path, graph, collection, view);
        }

        return new UnknownDefinition(rdf.Path, graph);
    }

    private async Task CreateCollectionAsync(IGraph graph, CollectionDefinition definition)
    {
        var result = await collectionService.CreateCollectionAsync(graph).ConfigureAwait(false);
        if (result.IsValid)
        {
            logger.LogInformation($"Created collection '{definition.Collection}'");
        }
        else
        {
            logger.LogWarning(
                $"Cannot create collection '{definition.Collection}' because of validation errors:");
            result.Errors.ForEach(x => logger.LogWarning(x.ErrorMessage));
        }
    }

    private async Task CreateViewAsync(IGraph graph, ViewDefinition definition)
    {
        var collectionName = definition.Collection.ToString().Split('/').Last();
        var result = await viewService
            .CreateViewAsync(graph, collectionName).ConfigureAwait(false);
        if (result is null)
        {
            logger.LogWarning(
                $"Cannot create a view '{definition.View}' for non-existing collection '{definition.Collection}'");
        }
        else if (result.IsValid)
        {
            logger.LogInformation($"Created view '{definition.View}' for collection '{definition.Collection}'");
        }
        else
        {
            logger.LogWarning(
                $"Cannot create view '{definition.View}' for collection '{definition.Collection}' because of validation errors:");
            result.Errors.ForEach(x => logger.LogWarning(x.ErrorMessage));
        }
    }
}