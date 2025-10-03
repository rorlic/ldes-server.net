using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Namespaces;
using AquilaSolutions.LdesServer.Core.Test.Extensions;
using AquilaSolutions.LdesServer.Fragmentation.Models;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace AquilaSolutions.LdesServer.Fragmentation.Test;

public class TimeFragmentationTests
{ 
    private static Graph LoadGraph(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var g = new Graph();

        var parser = new TurtleParser();
        parser.Load(g, reader);
        return g;
    }

    private static TimeFragmentation CreateSut(Stream stream)
    {
        using var g = LoadGraph(stream);

        var fragmentationListRoot = g.FindOneByQNamePredicate(QNames.tree.fragmentationStrategy)?.Object;
        fragmentationListRoot.Should().NotBeNull();

        var fragmentationDefinition = g.GetListItems(fragmentationListRoot).SingleOrDefault();
        fragmentationDefinition.Should().NotBeNull();

        return TimeFragmentation.From(g, fragmentationDefinition);
    }

    private const string Predicate = "ex:something";
    
    private static async Task<Member> CreateMember(string timestamp)
    {
        const string entityId = "ex:memberId";
        var definition = $"""
                          @prefix xsd: <http://www.w3.org/2001/XMLSchema#> .
                          @prefix ex:  <https://example.org/> . 
                          {entityId} {Predicate} "{timestamp}"^^xsd:dateTime.
                          """;
        using var stream = await definition.AsStream();
        using var g = LoadGraph(stream);
        var entityUri = g.CreateUriNode(entityId);
        var quads = g.Triples.Select(x => new Quad(x, entityUri));

        var memberUri = g.CreateUriNode("ex:dummy");
        return Member.From(quads, memberUri, entityUri, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task WhenNoTimeBucketsAreDefinedThenDefaultsAreApplied()
    {
        const string definition = $"""
                                   @prefix tree: <https://w3id.org/tree#> . 
                                   @prefix lsdn: <https://ldes-server.net/> . 
                                   @prefix ex:   <https://example.org/> . 
                                   </occupancy/by-page> tree:fragmentationStrategy ([a lsdn:TimeFragmentation; 
                                     tree:path {Predicate}
                                   ]) .
                                   """;
        using var stream = await definition.AsStream();
        var sut = CreateSut(stream);
        var member = await CreateMember("2025-08-20T07:42:00Z");

        var result = sut.TimeBucketPathsFor(member).ToArray();
        result.Length.Should().Be(1);
        
        var path = result[0].ToArray();
        path.Should().BeEquivalentTo([
            new TimeBucket("2025-01-01T00:00:00Z", "2026-01-01T00:00:00Z", "[2025 .. 2026]"),
            new TimeBucket("2025-08-01T00:00:00Z", "2025-09-01T00:00:00Z", "[2025-08 .. 2025-09]"),
            new TimeBucket("2025-08-20T00:00:00Z", "2025-08-21T00:00:00Z", "[2025-08-20 .. 2025-08-21]"),
            new TimeBucket("2025-08-20T07:00:00Z", "2025-08-20T08:00:00Z", "[2025-08-20T07:00:00Z .. 2025-08-20T08:00:00Z]"),
        ]);
    }
    
    [Fact]
    public async Task WhenTimeBucketsAreDefinedThenTheseAreUsedInsteadOfDefaults()
    {
        const string definition = $"""
                                   @prefix tree: <https://w3id.org/tree#> . 
                                   @prefix lsdn: <https://ldes-server.net/> . 
                                   @prefix ex:   <https://example.org/> . 
                                   @prefix xsd:  <http://www.w3.org/2001/XMLSchema#> . 
                                   </occupancy/by-page> tree:fragmentationStrategy ([a lsdn:TimeFragmentation; 
                                     tree:path ({Predicate});
                                     lsdn:bucket "PT6H"^^xsd:duration, "P1D"^^xsd:duration, "PT15M"^^xsd:duration
                                   ]) .
                                   """;
        using var stream = await definition.AsStream();
        var sut = CreateSut(stream);
        var member = await CreateMember("2025-08-20T07:42:00Z");

        var result = sut.TimeBucketPathsFor(member).ToArray();
        result.Length.Should().Be(1);
        
        var path = result[0].ToArray();
        path.Should().ContainInOrder([
            new TimeBucket("2025-08-20T00:00:00Z", "2025-08-21T00:00:00Z", "[2025-08-20 .. 2025-08-21]"),
            new TimeBucket("2025-08-20T06:00:00Z", "2025-08-20T12:00:00Z", "[2025-08-20T06:00:00Z .. 2025-08-20T12:00:00Z]"),
            new TimeBucket("2025-08-20T07:30:00Z", "2025-08-20T07:45:00Z", "[2025-08-20T07:30:00Z .. 2025-08-20T07:45:00Z]"),
        ]);
    }
}