using System.Data;
using System.Text;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Models.Configuration;
using AquilaSolutions.LdesServer.Fetching.Services;
using AquilaSolutions.LdesServer.Fetching.Test.Extensions;
using FluentAssertions;
using Moq;
using VDS.RDF;
using Xunit.Abstractions;

namespace AquilaSolutions.LdesServer.Fetching.Test.Services;

public class GetNodeTest(ITestOutputHelper helper)
{
    #region Helpers

    private const string BaseUri = "https://example.com/feed/";
    private const string CollectionName = "collection";
    private const string PageName = "07ec8b92-939a-437d-8aa6-254da35184cf";
    private const string PartialPageName = $"{CollectionName}/_/{PageName}";

    private static readonly IUriNode CollectionUri = new UriNode(new Uri($"{BaseUri}{CollectionName}"));
    private static readonly IUriNode PageUri = new UriNode(new Uri($"{BaseUri}{PartialPageName}"));

    private static readonly IUriNode IsA = new UriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
    private static readonly IUriNode EventStreamType = new UriNode(new Uri("https://w3id.org/ldes#EventStream"));
    private static readonly IUriNode EventSourceType = new UriNode(new Uri("https://w3id.org/ldes#EventSource"));
    private static readonly IUriNode TreeNodeType = new UriNode(new Uri("https://w3id.org/tree#Node"));
    private static readonly IUriNode TreeRelationType = new UriNode(new Uri("https://w3id.org/tree#Relation"));
    private static readonly IUriNode TreeViewPredicate = new UriNode(new Uri("https://w3id.org/tree#view"));
    private static readonly IUriNode TreeShapePredicate = new UriNode(new Uri("https://w3id.org/tree#shape"));
    private static readonly IUriNode NodeShapeType = new UriNode(new Uri("http://www.w3.org/ns/shacl#NodeShape"));

    private static readonly IUriNode TreeViewDescriptionPredicate =
        new UriNode(new Uri("https://w3id.org/tree#viewDescription"));

    private static readonly IUriNode TreeRelationPredicate = new UriNode(new Uri("https://w3id.org/tree#relation"));
    private static readonly IUriNode TreeNodePredicate = new UriNode(new Uri("https://w3id.org/tree#node"));

    private static readonly IUriNode LdesVersionOfPathPredicate =
        new UriNode(new Uri("https://w3id.org/ldes#versionOfPath"));

    private static readonly IUriNode LdesTimestampPathPredicate =
        new UriNode(new Uri("https://w3id.org/ldes#timestampPath"));

    private static readonly IUriNode DctIsVersionOf = new UriNode(new Uri("http://purl.org/dc/terms/isVersionOf"));

    private static readonly IUriNode ProvGeneratedAtTime =
        new UriNode(new Uri("http://www.w3.org/ns/prov#generatedAtTime"));

    private static INode GetSingleProperty(IGraph g, IRefNode subject, IUriNode predicate,
        Func<Triple, bool>? filter = null)
    {
        var triples = g.GetTriplesWithSubjectPredicate(subject, predicate).ToArray();
        if (filter != null)
        {
            triples = triples.Where(filter).ToArray();
        }

        triples.Should().HaveCount(1);
        return triples.First().Object;
    }

    private static IEnumerable<INode> GetProperty(IGraph g, IRefNode subject, IUriNode predicate,
        Func<Triple, bool>? filter = null)
    {
        var triples = g.GetTriplesWithSubjectPredicate(subject, predicate).ToArray();
        if (filter != null)
        {
            triples = triples.Where(filter).ToArray();
        }

        return triples.Select(x => x.Object);
    }

    private static NodeService CreateSystemUnderTest(Collection? collection = null,
        Action<Mock<IViewRepository>, IDbTransaction, Collection>? setupViewLookup = null,
        Action<Mock<IPageRepository>, IDbTransaction>? setupPageLookup = null)
    {
        collection ??= new Collection
            { Name = CollectionName, Definition = $"<{CollectionUri}> a <https://w3id.org/ldes#EventStream> ." };

        var transaction = new Mock<IDbTransaction>();
        var connection = new Mock<IDbConnection>();
        connection.Setup(x => x.BeginTransaction()).Returns(transaction.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IDbConnection))).Returns(connection.Object);
        serviceProvider.Setup(x => x.GetService(typeof(LdesServerConfiguration)))
            .Returns(new LdesServerConfiguration{BaseUri = BaseUri});

        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x =>
            x.GetCollectionAsync(transaction.Object, It.IsAny<string>())).ReturnsAsync(collection);

        var viewRepositoryMock = new Mock<IViewRepository>();
        setupViewLookup?.Invoke(viewRepositoryMock, transaction.Object, collection);

        var pageRepositoryMock = new Mock<IPageRepository>();
        var page = new Page { Name = PageName, Root = true, Open = false, Assigned = 250 };
        setupPageLookup?.Invoke(pageRepositoryMock, transaction.Object);
        pageRepositoryMock.Setup(x => x.GetPageAsync(transaction.Object, collection, View.DefaultName, PageName))
            .ReturnsAsync(page);

        return new NodeService(new FetchingConfiguration(), serviceProvider.Object, collectionRepositoryMock.Object,
            viewRepositoryMock.Object, pageRepositoryMock.Object);
    }

    #endregion

    [Fact]
    public async Task EventStreamContainsTheBaseUrl()
    {
        var sut = CreateSystemUnderTest();

        using var g = (await sut.GetEventStreamAsync(CollectionName))?.Graph;

        g.Should().NotBeNull();
        g.BaseUri.Should().Be(BaseUri);
    }

    [Fact]
    public async Task EventStreamContainsTheDefinition()
    {
        var sut = CreateSystemUnderTest();

        using var g = await sut.GetEventStreamAsync(CollectionName);

        g.Should().NotBeNull();
        // expected:
        //   <https://example.com/ldes/collection>
        //     a <https://w3id.org/ldes#EventStream> ;
        //     <https://w3id.org/ldes#versionOfPath> <http://purl.org/dc/terms/isVersionOf> ;
        //     <https://w3id.org/ldes#timestampPath> <http://www.w3.org/ns/prov#generatedAtTime> .
        g.Triples.Should().Contain([
            new Triple(CollectionUri, IsA, EventStreamType),
            new Triple(CollectionUri, LdesVersionOfPathPredicate, DctIsVersionOf),
            new Triple(CollectionUri, LdesTimestampPathPredicate, ProvGeneratedAtTime)
        ]);
    }

    [Fact]
    public async Task EventStreamContainsOtherViewDefinitions()
    {
        const string view1 = "view1";
        const string view2 = "view2";
        const string pageName = "07ec8b92-939a-437d-8aa6-254da35184cf";
        var defaultView = new View { Name = string.Empty };
        var sut = CreateSystemUnderTest(
            setupViewLookup: (m, t, c) =>
                m.Setup(x => x.GetCollectionViewsAsync(t, c)).ReturnsAsync(
                    [defaultView, new View { Name = view1 }, new View { Name = view2 }]),
            setupPageLookup: (m, t) =>
                m.Setup(x => x.GetDefaultBucketRootPageAsync(t, defaultView)).ReturnsAsync(
                    new Page { Name = $"{CollectionName}/{pageName}", Root = true, Open = true, Assigned = 0 })
        );

        using var g = (await sut.GetEventStreamAsync(CollectionName))?.Graph;

        g.Should().NotBeNull();
        // expected:
        //   @base <https://example.com/ldes/collection> . 
        //   <> a ldes:EventStream ; ldes:view </view1>, </view2> .

        var defaultViewNode = new UriNode(new Uri($"{BaseUri}/"));

        var otherViewNodes = GetProperty(g, CollectionUri, TreeViewPredicate, x => !x.Object.Equals(defaultViewNode));
        otherViewNodes.Should()
            .Contain(new[] { view1, view2 }.Select(x => new UriNode(new Uri($"{BaseUri}{CollectionName}/{x}"))));
    }

    [Fact]
    public async Task EventStreamContainsConfiguredPrefixes()
    {
        const string definition = $"""
                                   @prefix abc: <http://abc.org/> .
                                   @prefix def: <http://def.org#> .
                                   </{CollectionName}> a <https://w3id.org/ldes#EventStream> .
                                   """;
        var collection = new Collection { Name = CollectionName, Definition = definition };
        var sut = CreateSystemUnderTest(collection);

        var g = (await sut.GetEventStreamAsync(CollectionName))?.Graph;

        // expected:
        //  @prefix abc: <http://abc.org/> .
        //  @prefix def: <http://def.org#> .
        g.Should().NotBeNull();
        g.NamespaceMap.Prefixes.Should().Contain(["abc", "def"]);
        g.NamespaceMap.GetNamespaceUri("abc").Should().Be(new Uri("http://abc.org/"));
        g.NamespaceMap.GetNamespaceUri("def").Should().Be(new Uri("http://def.org#"));
    }

    [Fact]
    public async Task EventStreamContainsConfiguredShaclShapes()
    {
        const string definition = $"""
                                   @prefix tree: <https://w3id.org/tree#> .
                                   @prefix ldes: <https://w3id.org/ldes#> .
                                   @prefix sh:   <http://www.w3.org/ns/shacl#> .
                                   <{CollectionName}> a ldes:EventStream ; tree:shape [ a sh:NodeShape ] .
                                   """;
        var collection = new Collection { Name = CollectionName, Definition = definition };
        var sut = CreateSystemUnderTest(collection);

        var g = (await sut.GetEventStreamAsync(CollectionName))?.Graph;

        // expected:
        //  <{CollectionUri}> tree:shape _b1.
        //  _b1 a sh:NodeShape .
        g.Should().NotBeNull();
        
        var shaclShape = GetSingleProperty(g, CollectionUri, TreeShapePredicate);
        shaclShape.Should().BeOfType<BlankNode>();

        var shapeType = GetSingleProperty(g, (BlankNode)shaclShape, IsA);
        shapeType.Should().Be(NodeShapeType);
    }
    
    [Fact]
    public async Task EventStreamContainsRecommendedEventStreamPredicates()
    {
        const string versionOf = "example:versionOf";
        const string timestamp = "example:timestamp";
        const string definition = $"""
                                   @prefix ldes:   <https://w3id.org/ldes#> .
                                   @prefix example: <https://example.org/> .
                                   </{CollectionName}> a ldes:EventStream ;
                                                   ldes:versionOfPath {versionOf} ;
                                                   ldes:timestampPath {timestamp} .
                                   """;
        var collection = new Collection { Name = CollectionName, Definition = definition };
        var sut = CreateSystemUnderTest(collection);

        var g = (await sut.GetEventStreamAsync(CollectionName))?.Graph;

        g.Should().NotBeNull();

        var versionOfPredicate = g.GetTriplesWithPredicate(g.CreateUriNode("ldes:versionOfPath")).Single().Object;
        versionOfPredicate.Should().Be(g.CreateUriNode(versionOf));

        var timestampPredicate = g.GetTriplesWithPredicate(g.CreateUriNode("ldes:timestampPath")).Single().Object;
        timestampPredicate.Should().Be(g.CreateUriNode(timestamp));
    }


    [Fact]
    public async Task DefaultViewContainsEventSourceDefinition()
    {
        const string pageName = "07ec8b92-939a-437d-8aa6-254da35184cf";
        var defaultView = new View { Name = View.DefaultName };
        var sut = CreateSystemUnderTest(
            setupViewLookup: (m, t, c) =>
                m.Setup(x => x.GetCollectionViewAsync(t, c, defaultView.Name)).ReturnsAsync(defaultView),
            setupPageLookup: (m, t) =>
            {
                var page = new Page { Name = string.Empty, Root = true, Open = true, Assigned = 0 };
                m.Setup(x => x.GetDefaultBucketRootPageAsync(t, defaultView)).ReturnsAsync(page);
                m.Setup(x => x.GetPageRelationsAsync(t, page)).ReturnsAsync([new PageRelation { Link = pageName }]);
            });

        using var g = (await sut.GetViewAsync(CollectionName, View.DefaultName))!.Graph;

        g.Should().NotBeNull();
        // expected:
        //   @base <https://example.com/ldes/> . 
        //   </collection> a ldes:EventStream ; ldes:view </collection/_> .
        //
        //  </collection/_> a tree:Node ; tree:viewDescription [ a tree:EventSource ] .

        var defaultViewNode = new UriNode(new Uri($"{CollectionUri}/{defaultView.Name}"));

        var viewNode = GetSingleProperty(g, CollectionUri, TreeViewPredicate);
        viewNode.Should().BeOfType<UriNode>().And.Subject.As<UriNode>().Should().Be(defaultViewNode);

        var viewType = GetSingleProperty(g, (UriNode)viewNode, IsA);
        viewType.Should().Be(TreeNodeType);

        var viewDescription = GetSingleProperty(g, (UriNode)viewNode, TreeViewDescriptionPredicate);
        viewDescription.Should().BeOfType<BlankNode>();

        var eventSource = GetSingleProperty(g, (BlankNode)viewDescription, IsA);
        eventSource.Should().Be(EventSourceType);

        var relationNode = GetSingleProperty(g, (UriNode)viewNode, TreeRelationPredicate);
        relationNode.Should().BeOfType<BlankNode>();

        var relationType = GetSingleProperty(g, (BlankNode)relationNode, IsA);
        relationType.Should().Be(TreeRelationType);

        var relationNodeUri = GetSingleProperty(g, (BlankNode)relationNode, TreeNodePredicate);
        relationNodeUri.Should().BeOfType<UriNode>().And.Subject.Should()
            .Be(new UriNode(new Uri($"{BaseUri}{CollectionName}/_/{pageName}")));
    }


    [Fact]
    public async Task PageContainsConfiguredPrefixes()
    {
        const string definition = $"""
                                   @prefix abc: <http://abc.org/> .
                                   @prefix def: <http://def.org#> .
                                   </{CollectionName}>  a <https://w3id.org/ldes#EventStream> .
                                   """;
        var collection = new Collection { Name = CollectionName, Definition = definition };
        var sut = CreateSystemUnderTest(collection);

        using var stream = new MemoryStream();
        await using var streamWriter = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        var result = await sut.GetPageAsync(CollectionName, View.DefaultName, PageName);
        result.Should().NotBeNull();

        using var g = result.Graph;
        // expected:
        //  @prefix abc: <http://abc.org/> .
        //  @prefix def: <http://def.org#> .
        g.Should().NotBeNull();
        g.NamespaceMap.Prefixes.Should().Contain(["abc", "def"]);
        g.NamespaceMap.GetNamespaceUri("abc").Should().Be(new Uri("http://abc.org/"));
        g.NamespaceMap.GetNamespaceUri("def").Should().Be(new Uri("http://def.org#"));
    }

    [Fact]
    public async Task PageContainEventStreamDefinition()
    {
        var sut = CreateSystemUnderTest();

        using var stream = new MemoryStream();
        await using var streamWriter = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        var result = await sut.GetPageAsync(CollectionName, View.DefaultName, PageName);
        result.Should().NotBeNull();

        using var g = result.Graph;
        // expected:
        //   <https://example.com/feed/collection>
        //     a <https://w3id.org/ldes#EventStream> ;
        //     <https://w3id.org/ldes#versionOfPath> <http://purl.org/dc/terms/isVersionOf> ;
        //     <https://w3id.org/ldes#timestampPath> <http://www.w3.org/ns/prov#generatedAtTime> ;
        g.Should().NotBeNull();
        g.Triples.Should().Contain([
            new Triple(CollectionUri, IsA, EventStreamType),
            new Triple(CollectionUri, LdesVersionOfPathPredicate, DctIsVersionOf),
            new Triple(CollectionUri, LdesTimestampPathPredicate, ProvGeneratedAtTime)
        ]);
    }

    [Fact]
    public async Task PageContainsCustomEventStreamPredicatesIfConfigured()
    {
        const string versionOf = "example:versionOf";
        const string timestamp = "example:timestamp";
        const string definition = $"""
                                   @prefix ldes:   <https://w3id.org/ldes#> .
                                   @prefix example: <https://example.org/> .
                                   </{CollectionName}> a ldes:EventStream ;
                                                   ldes:versionOfPath {versionOf} ;
                                                   ldes:timestampPath {timestamp} .
                                   """;
        var collection = new Collection { Name = CollectionName, Definition = definition };
        var sut = CreateSystemUnderTest(collection);

        using var stream = new MemoryStream();
        await using var streamWriter = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        var result = await sut.GetPageAsync(CollectionName, View.DefaultName, PageName);
        result.Should().NotBeNull();

        using var g = result.Graph;
        // expected:
        //   <https://example.com/feed/collection>
        //     a <https://w3id.org/ldes#EventStream> ;
        //     <https://w3id.org/ldes#versionOfPath> <https://example.org/versionOf> ;
        //     <https://w3id.org/ldes#timestampPath> <https://example.org/timestamp> ;
        g.Should().NotBeNull();

        var versionOfPredicate = g.GetTriplesWithPredicate(g.CreateUriNode("ldes:versionOfPath")).Single().Object;
        versionOfPredicate.Should().Be(g.CreateUriNode(versionOf));

        var timestampPredicate = g.GetTriplesWithPredicate(g.CreateUriNode("ldes:timestampPath")).Single().Object;
        timestampPredicate.Should().Be(g.CreateUriNode(timestamp));
        // TODO: <{collection-name}> tree:shape <{configured-shape}>
    }

    [Fact]
    public async Task PageContainsRequiredNodePredicates()
    {
        var sut = CreateSystemUnderTest();

        using var stream = new MemoryStream();
        await using var streamWriter = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        var result = await sut.GetPageAsync(CollectionName, View.DefaultName, PageName);
        result.Should().NotBeNull();

        using var g = result.Graph;
        // expected:
        //   @base <https://example.com/ldes> . 
        //   </collection> a ldes:EventStream ; ldes:view </collection/{PageId}> .
        //
        //  </collection/{PageId}> a tree:Node .
        g.Should().NotBeNull();
        g.Triples.Should().Contain([
            new Triple(CollectionUri, IsA, EventStreamType),
            new Triple(CollectionUri, TreeViewPredicate, PageUri),
            new Triple(PageUri, IsA, TreeNodeType)
        ]);
    }

    [Fact]
    public async Task PageContainsGenericRelations()
    {
        var nextPageName = "589914c6-d4f3-41b1-982f-df1732c716de";
        var defaultView = new View { Name = View.DefaultName };
        var sut = CreateSystemUnderTest(
            setupPageLookup: (m, t) =>
            {
                m.Setup(x => x.GetPageRelationsAsync(t, It.IsAny<Page>()))
                    .ReturnsAsync([new PageRelation { Link = nextPageName }]);
                m.Setup(x => x.GetPageMembersAsync(t, It.IsAny<Page>())).ReturnsAsync([]);
            });
        var nextPageUri = new UriNode(new Uri($"{BaseUri}{CollectionName}/_/{nextPageName}"));

        using var stream = new MemoryStream();
        await using var streamWriter = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        var result = await sut.GetPageAsync(CollectionName, View.DefaultName, PageName);
        result.Should().NotBeNull();

        using var g = result.Graph;
        // expected:
        //   @base <https://example.com/ldes> . 
        //   </collection> a ldes:EventStream ; ldes:view </collection/{PageId}> .
        //
        //  </collection/{PageId}> a tree:Node .
        //  </collection/{PageId}> tree:relation [ a tree:Relation ; tree:node {nextPageUri}] .
        g.Should().NotBeNull();
        var viewType = GetSingleProperty(g, PageUri, IsA);
        viewType.Should().Be(TreeNodeType);

        var relationNode = GetSingleProperty(g, PageUri, TreeRelationPredicate);
        relationNode.Should().BeOfType<BlankNode>();

        var relationType = GetSingleProperty(g, (BlankNode)relationNode, IsA);
        relationType.Should().Be(TreeRelationType);

        var relationNodeUri = GetSingleProperty(g, (BlankNode)relationNode, TreeNodePredicate);
        relationNodeUri.Should().BeOfType<UriNode>().And.Subject.Should().Be(nextPageUri);

        helper.DumpGraph(g);
    }

    [Fact]
    public void PageContainsMembers()
    {
        // TODO: test that node contains:
        //       <{collection-name}> tree:member ?uri
        //       ?uri ?p ?o
    }

    // [Fact]
    // public void ContainsSpecializedRelations()
    // {
    //     // TODO: test that node contains:
    //     //       @prefix {collection-name}: <{collection-name}/> 
    //     //       {collection-name}:{node-guid} a tree:Node
    //     //       {collection-name}:{node-guid} tree:relation ?r
    //     //       ?r rdfs:subClassOf+ tree:Relation
    //     //       ?r tree:node ?n
    //     //       ?r tree:path ?p
    //     //       ?r tree:value ?v
    // }
}