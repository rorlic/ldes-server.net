using AquilaSolutions.LdesServer.Core.Extensions;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Nodes;

namespace AquilaSolutions.LdesServer.Core.Test.Extensions;

public class GraphExtensionsTest
{
    [Fact]
    public void CanAddBaseUri()
    {
        var baseUri = new Uri("http://example.org/");
        var g = new Graph().WithBaseUri(baseUri);
        g.BaseUri.Should().Be(baseUri);
    }

    [Fact]
    public void CanAddStandardPrefix()
    {
        var prefixes = new Dictionary<string, string>
        {
            { "tree", "https://w3id.org/tree#" },
            { "ldes", "https://w3id.org/ldes#" },
            { "dct", "http://purl.org/dc/terms/" },
            { "prov", "http://www.w3.org/ns/prov#" },
        };
        var g = new Graph().WithStandardPrefixes();
        foreach (var prefix in prefixes)
        {
            g.NamespaceMap.Prefixes.Should().Contain(prefix.Key);
            g.NamespaceMap.GetNamespaceUri(prefix.Key).Should().Be(new Uri(prefix.Value));
        }
    }

    [Fact]
    public void CanFindOneByQNamePredicate()
    {
        var quads = LoadResource.FromTurtle("Data.Example.ttl");
        var g = new Graph();
        g.Assert(quads.Select(x => x.AsTriple()));
        g.NamespaceMap.AddNamespace(
            "verkeersmetingen", new Uri("https://implementatie.data.vlaanderen.be/ns/vsds-verkeersmetingen#"));

        var result = g.FindOneByQNamePredicate("verkeersmetingen:Verkeerstelling.tellingresultaat");
        result.Should().NotBeNull();
        result.Object.AsValuedNode().AsInteger().Should().Be(0);
    }

    [Fact]
    public void ReturnsNullIfCannotFindOneByQNamePredicate()
    {
        var quads = LoadResource.FromTurtle("Data.Example.ttl");
        var g = new Graph();
        g.Assert(quads.Select(x => x.AsTriple()));
        g.NamespaceMap.AddNamespace("dummy", new Uri("http://dummy.org/"));

        var result = g.FindOneByQNamePredicate("dummy:nothing");
        result.Should().BeNull();
    }

    private static readonly string TheSubject =
        new("observatie:00076HR1/2023-09-27T00:15:00/count/medium/2025-02-15T11:02:29.312619345");

    private static readonly string ThePredicate =
        new("dct:isVersionOf");

    private static readonly string TheObject =
        new("observatie:00076HR1/2023-09-27T00:15:00/count/medium");

    [Fact]
    public void CanGetObjectBySubjectPredicate()
    {
        var quads = LoadResource.FromTurtle("Data.Example.ttl");
        var g = new Graph();
        g.Assert(quads.Select(x => x.AsTriple()));
        g.NamespaceMap.AddNamespace("observatie",
            new Uri("https://verkeerscentrum.be/id/verkeersmetingen/observatie/"));
        g.NamespaceMap.AddNamespace("dct", new Uri("http://purl.org/dc/terms/"));

        var result = g.GetObjectBySubjectPredicate(
            g.CreateUriNode(TheSubject), g.CreateUriNode(ThePredicate));
        result.Should().Be(g.CreateUriNode(TheObject));
    }

    [Fact]
    public void CanFindObjectBySubjectPredicate()
    {
        var quads = LoadResource.FromTurtle("Data.Example.ttl");
        var g = new Graph();
        g.Assert(quads.Select(x => x.AsTriple()));
        g.NamespaceMap.AddNamespace("observatie",
            new Uri("https://verkeerscentrum.be/id/verkeersmetingen/observatie/"));
        g.NamespaceMap.AddNamespace("dct", new Uri("http://purl.org/dc/terms/"));

        var result = g.FindObjectBySubjectPredicate(
            g.CreateUriNode(TheSubject), g.CreateUriNode(ThePredicate));
        result.Should().NotBeNull().And.Be(g.CreateUriNode(TheObject));
    }

    [Fact]
    public void ResultsNullIfCannotFindObjectBySubjectPredicate()
    {
        var quads = LoadResource.FromTurtle("Data.Example.ttl");
        var g = new Graph();
        g.Assert(quads.Select(x => x.AsTriple()));
        g.NamespaceMap.AddNamespace("observatie",
            new Uri("https://verkeerscentrum.be/id/verkeersmetingen/observatie/"));
        g.NamespaceMap.AddNamespace("dummy", new Uri("http://dummy.org/"));

        var result = g.FindObjectBySubjectPredicate(
            g.CreateUriNode(TheSubject), g.CreateUriNode("dummy:nothing"));
        result.Should().BeNull();
    }

    [Fact]
    public void CanGetSubjectByPredicateObject()
    {
        var quads = LoadResource.FromTurtle("Data.Example.ttl");
        var g = new Graph();
        g.Assert(quads.Select(x => x.AsTriple()));
        g.NamespaceMap.AddNamespace("observatie",
            new Uri("https://verkeerscentrum.be/id/verkeersmetingen/observatie/"));
        g.NamespaceMap.AddNamespace("dct", new Uri("http://purl.org/dc/terms/"));

        var result = g.GetSubjectByPredicateObject(
            g.CreateUriNode(ThePredicate), g.CreateUriNode(TheObject));
        result.Should().NotBeNull().And.Be(g.CreateUriNode(TheSubject));
    }

    [Fact]
    public void CanAddOneTriple()
    {
        var g = new Graph();
        g.NamespaceMap.AddNamespace("observatie",
            new Uri("https://verkeerscentrum.be/id/verkeersmetingen/observatie/"));
        g.NamespaceMap.AddNamespace("dct", new Uri("http://purl.org/dc/terms/"));

        g.WithTriple(g.CreateUriNode(TheSubject),
            g.CreateUriNode(ThePredicate), g.CreateUriNode(TheObject));

        g.Triples.Should().BeEquivalentTo(new Triple[]
        {
            new(g.CreateUriNode(TheSubject), g.CreateUriNode(ThePredicate), g.CreateUriNode(TheObject))
        });
    }

    [Fact]
    public void CanAddMultipleTriple()
    {
        var g = new Graph();
        g.NamespaceMap.AddNamespace("observatie",
            new Uri("https://verkeerscentrum.be/id/verkeersmetingen/observatie/"));
        g.NamespaceMap.AddNamespace("dct", new Uri("http://purl.org/dc/terms/"));

        g.WithTriples([
            new(g.CreateUriNode(TheSubject), g.CreateUriNode(ThePredicate), g.CreateUriNode(TheObject)),
            new(g.CreateUriNode(TheSubject), g.CreateUriNode("dct:created"),
                g.CreateLiteralNode("2023-09-27T00:15:00", new Uri("http://www.w3.org/2001/XMLSchema#dateTime")))
        ]);

        g.Triples.Should().BeEquivalentTo(new Triple[]
        {
            new(g.CreateUriNode(TheSubject), g.CreateUriNode(ThePredicate), g.CreateUriNode(TheObject)),
            new(g.CreateUriNode(TheSubject), g.CreateUriNode("dct:created"),
                g.CreateLiteralNode("2023-09-27T00:15:00", new Uri("http://www.w3.org/2001/XMLSchema#dateTime")))
        });
    }
    
//     private static readonly TurtleParser TurtleParser = new();
//
//     [Fact]
//     public async Task CanGetPredicatePathDefinition()
//     {
//         // NOTE: the property path definition for a predicate path is a single triple as the predicate itself fully defines it
//         const string rdf = """
//                            @prefix ex: <http://example.org/> .
//                            [] ex:path ex:parent .
//                            """;
//         using var stream = await rdf.AsStream();
//         using var reader = new StreamReader(stream);
//         using var g = new Graph();
//         TurtleParser.Load(g, reader);
//
//         var triples = g.GetPropertyPathDefinition(g.CreateUriNode("ex:path")).ToArray();
//         triples.Should().HaveCount(1);
//
//         var t = triples[0];
//         t.Predicate.Should().Be(g.CreateUriNode("ex:path"));
//         t.Object.Should().Be(g.CreateUriNode("ex:parent"));
//     }
//
//     [Fact]
//     public async Task CanGetSequencePathDefinition()
//     {
//         // NOTE: the property path definition for a sequence path is the collection of triples defining the list and all its items, which are property paths themselves 
//         const string rdf = """
//                            @prefix ex: <http://example.org/> .
//                            [] ex:path (ex:parent ex:firstName) .
//                            """;
//         using var stream = await rdf.AsStream();
//         using var reader = new StreamReader(stream);
//         using var g = new Graph();
//         TurtleParser.Load(g, reader);
//
//         var triples = g.GetPropertyPathDefinition(g.CreateUriNode("ex:path")).ToArray();
//         triples.Should().HaveCount(5);
//
//         using var definition = new Graph().WithTriples(triples);
//         definition.NamespaceMap.Import(g.NamespaceMap);
//         
//         var path = definition.FindOneByQNamePredicate("ex:path")?.Object;
//         path.Should().NotBeNull();
//
//         var items = definition.GetListItems(path).ToArray();
//         items.Should().HaveCount(2);
//         items.Should()
//             .BeEquivalentTo([definition.CreateUriNode("ex:parent"), definition.CreateUriNode("ex:firstName")]);
//     }
//
//     [Fact]
//     public async Task CanGetAlternativePath()
//     {
//         // NOTE: the property path definition for an alternative path is the collection of triples defining the blank node, the list and all its items, which are property paths themselves 
//         const string rdf = """
//                            @prefix ex: <http://example.org/> .
//                            @prefix sh: <http://www.w3.org/ns/shacl#> .
//                            [] ex:path [ sh:alternativePath (ex:father ex:mother) ] .
//                            """;
//         using var stream = await rdf.AsStream();
//         using var reader = new StreamReader(stream);
//         using var g = new Graph();
//         TurtleParser.Load(g, reader);
//
//         var triples = g.GetPropertyPathDefinition(g.CreateUriNode("ex:path")).ToArray();
//         triples.Should().HaveCount(6);
//
//         using var definition = new Graph().WithTriples(triples);
//         definition.NamespaceMap.Import(g.NamespaceMap);
//
//         var path = definition.FindOneByQNamePredicate("ex:path")?.Object;
//         path.Should().NotBeNull();
//
//         var list = definition.GetObjectBySubjectPredicate(path, definition.CreateUriNode("sh:alternativePath"));
//         var items = definition.GetListItems(list).ToArray();
//         items.Should().HaveCount(2);
//         items.Should().BeEquivalentTo([definition.CreateUriNode("ex:father"), definition.CreateUriNode("ex:mother")]);
//     }
//
//     [Fact]
//     public async Task CanGetInversePath()
//     {
//         // NOTE: the property path definition for an inverse path is the collection of triples defining the blank node and its object, which is a property paths 
//         const string rdf = """
//                            @prefix ex: <http://example.org/> .
//                            @prefix sh: <http://www.w3.org/ns/shacl#> .
//                            [] ex:path [ sh:inversePath ex:parent ] .
//                            """;
//         using var stream = await rdf.AsStream();
//         using var reader = new StreamReader(stream);
//         using var g = new Graph();
//         TurtleParser.Load(g, reader);
//
//         var triples = g.GetPropertyPathDefinition(g.CreateUriNode("ex:path")).ToArray();
//         triples.Should().HaveCount(2);
//
//         using var definition = new Graph().WithTriples(triples);
//         definition.NamespaceMap.Import(g.NamespaceMap);
//
//         var path = definition.FindOneByQNamePredicate("ex:path")?.Object;
//         path.Should().NotBeNull();
//
//         var predicate = definition.GetObjectBySubjectPredicate(path, definition.CreateUriNode("sh:inversePath"));
//         predicate.Should().Be(definition.CreateUriNode("ex:parent"));
//     }
//
//     [Theory]
//     [InlineData("sh:zeroOrMorePath")]
//     [InlineData("sh:oneOrMorePath")]
//     [InlineData("sh:zeroOrOnePath")]
//     public async Task CanGetNOrMPath(string nOrM)
//     {
//         // NOTE: the property path definition for a zero-or-more, one-or-more or zero-or-one path is the collection of triples defining the blank node and its object, which is a property paths 
//         // example: sequence path where the first item is a predicate path, and the second item is an N-or-M path
//         var rdf = $"""
//                    @prefix ex: <http://example.org/> .
//                    @prefix sh: <http://www.w3.org/ns/shacl#> .
//                    @prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
//                    @prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .
//                    [] ex:path ( rdf:type [ {nOrM} rdfs:subClassOf ] ) .
//                    """;
//         using var stream = await rdf.AsStream();
//         using var reader = new StreamReader(stream);
//         using var g = new Graph();
//         TurtleParser.Load(g, reader);
//
//         var triples = g.GetPropertyPathDefinition(g.CreateUriNode("ex:path")).ToArray();
//         triples.Should().HaveCount(6);
//
//         using var definition = new Graph().WithTriples(triples);
//         definition.NamespaceMap.Import(g.NamespaceMap);
//
//         var path = definition.FindOneByQNamePredicate("ex:path")?.Object;
//         path.Should().NotBeNull();
//
//         var items = definition.GetListItems(path).ToArray();
//         items.Should().HaveCount(2);
//         items[0].Should().Be(g.CreateUriNode("rdf:type"));
//
//         var blank = items[1];
//         blank.NodeType.Should().Be(NodeType.Blank);
//         
//         var @object = definition.GetObjectBySubjectPredicate(blank, definition.CreateUriNode(nOrM)); 
//         @object.Should().Be(g.CreateUriNode("rdfs:subClassOf"));
//     }
}