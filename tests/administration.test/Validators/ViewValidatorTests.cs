using LdesServer.Administration.Validators;
using LdesServer.Core.Extensions;
using LdesServer.Core.Test.Extensions;
using FluentAssertions;
using FluentValidation.Results;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace LdesServer.Administration.Test.Validators;

public class ViewValidatorTests
{
    private static ValidationResult ValidateView(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var data = new Graph().WithBaseUri(new Uri("https://example.org/"));

        var parser = new TurtleParser();
        parser.Load(data, reader);

        return new ViewValidator().Validate(data);
    }

    #region LSDN-21 - A view definition MUST contain at most one tree:Node

    [Theory]
    [InlineData("<occupancy/by-page> <https://w3id.org/tree#viewDescription> [] . ")]
    [InlineData("<occupancy/by-page> <https://w3id.org/tree#fragmentationStrategy> () . ")]
    [InlineData("<occupancy/by-page> <https://w3id.org/tree#pageSize> 500 . ")]
    [InlineData("<occupancy/by-page> <https://w3id.org/ldes#retentionPolicy> [] . ")]
    public async Task AcceptsNoTreeNode(string definition)
    {
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptsSingleTreeNode()
    {
        const string definition = "<occupancy/by-page> a <https://w3id.org/tree#Node> . ";
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultipleTreeNodes()
    {
        const string definition = """
                                  </occupancy/by-page> a <https://w3id.org/tree#Node> . 
                                  </occupancy/by-page2> a <https://w3id.org/tree#Node> .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-21 - A view definition MUST contain at most one tree:Node

    #region LSDN-22 - A view MUST be identified using a named node (and not a blank node)

    [Fact]
    public async Task RefusesUnnamedDefinition()
    {
        const string definition = "[] a <https://w3id.org/tree#Node> .";
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("<https://example.org/feed/occupancy> a <https://w3id.org/tree#Node> .")]
    [InlineData("@base <https://example.org> . </occupancy> a <https://w3id.org/tree#Node> .")]
    public async Task AcceptsNamedNode(string definition)
    {
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeTrue();
    }

    #endregion LSDN-22 - A view MUST be identified using a named node (and not a blank node)

    #region LSDN-23 - A view MAY contain at most one fragmentation strategy

    [Fact]
    public async Task AcceptsNoFragmentationStrategy()
    {
        const string definition = "<occupancy> a <https://w3id.org/tree#Node> .";
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("<occupancy/by-page> <https://w3id.org/tree#fragmentationStrategy> () .")]
    [InlineData(
        "@prefix tree: <https://w3id.org/tree#> . </occupancy/by-page> tree:viewDescription [ tree:fragmentationStrategy () ] .")]
    public async Task AcceptsSingleFragmentationStrategy(string definition)
    {
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultipleFragmentationStrategies()
    {
        const string definition =
            "@prefix tree: <https://w3id.org/tree#> . </occupancy/by-page> tree:fragmentationStrategy (), ([]) .";
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-23 - A view MAY contain at most one fragmentation strategy

    #region LSDN-24 - A view MAY contain at most one page size using a strictly positive integer

    [Fact]
    public async Task AcceptsMissingPageSize()
    {
        const string definition = "<occupancy> a <https://w3id.org/tree#Node> .";
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("<occupancy> <https://w3id.org/tree#pageSize> 10 .")]
    [InlineData("@prefix tree: <https://w3id.org/tree#> . </occupancy> tree:viewDescription [ tree:pageSize 10 ] .")]
    [InlineData(
        "@prefix xsd: <http://www.w3.org/2001/XMLSchema#> . </occupancy> <https://w3id.org/tree#pageSize> \"10\"^^xsd:integer .")]
    [InlineData("<occupancy> <https://w3id.org/tree#pageSize> \"10\"^^<http://www.w3.org/2001/XMLSchema#integer> .")]
    public async Task AcceptsSinglePageSize(string definition)
    {
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultiplePageSizes()
    {
        const string definition = "<occupancy> <https://w3id.org/tree#pageSize> 100, 250 .";
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("<occupancy> <https://w3id.org/tree#pageSize> 0 .")]
    [InlineData("<occupancy> <https://w3id.org/tree#pageSize> -25 .")]
    public async Task RefusesNegativeOrZeroPageSize(string definition)
    {
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-24 - A view MAY contain at most one page size using a strictly positive integer

    #region LSDN-27 - A view MUST only contain well-known fragmentation strategies

    [Theory]
    [InlineData("""
                @prefix tree: <https://w3id.org/tree#> . 
                @prefix lsdn: <https://ldes-server.net/ns/> . 
                @prefix ex: <https://example.org/> . 
                </occupancy/by-page> tree:fragmentationStrategy ( [a lsdn:TimeFragmentation; tree:path ex:something; ] ) .
                """)]
    [InlineData("""
                @prefix tree: <https://w3id.org/tree#> . 
                @prefix lsdn: <https://ldes-server.net/ns/> . 
                @prefix ex: <https://example.org/> . 
                </occupancy/by-page> tree:viewDescription [ tree:fragmentationStrategy ( [a lsdn:TimeFragmentation; tree:path ex:something;] ) ] .
                """)]
    public async Task AcceptsWellKnownFragmentationStrategy(string definition)
    {
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesUnknownFragmentationStrategy()
    {
        const string definition = """
                                  @prefix tree: <https://w3id.org/tree#> . 
                                  @prefix ex: <https://example.org/> . 
                                  </occupancy> tree:fragmentationStrategy ([a ex:UnknownFragmentation]) .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-27 - A view MUST only contain well-known fragmentation strategies

    #region LSDN-28 - A time fragmentation MUST specify the fragmentation path

    [Fact]
    public async Task RefusesNoFragmentationPath()
    {
        const string definition = """
                                  @prefix tree: <https://w3id.org/tree#> . 
                                  @prefix lsdn: <https://ldes-server.net/ns/> . 
                                  </occupancy/by-page> tree:fragmentationStrategy ( [a lsdn:TimeFragmentation] ) .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("""
                @prefix tree: <https://w3id.org/tree#> .
                @prefix lsdn: <https://ldes-server.net/ns/> . 
                @prefix ex: <https://example.org/> . 
                </occupancy/by-page> tree:fragmentationStrategy ( [a lsdn:TimeFragmentation; tree:path ex:something] ) .
                """)]
    [InlineData("""
                @prefix ex: <https://example.org/> . 
                @prefix lsdn: <https://ldes-server.net/ns/> . 
                @prefix tree: <https://w3id.org/tree#> . 
                </occupancy/by-page> tree:viewDescription [ tree:fragmentationStrategy ( [a lsdn:TimeFragmentation; tree:path ex:something] ) ] .
                """)]
    public async Task AcceptsSingleFragmentationPath(string definition)
    {
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultipleFragmentationPaths()
    {
        const string definition = """
                                  @prefix tree: <https://w3id.org/tree#> .
                                  @prefix lsdn: <https://ldes-server.net/ns/> . 
                                  @prefix ex: <https://example.org/> . 
                                  </occupancy/by-page> tree:fragmentationStrategy ( [a lsdn:TimeFragmentation; tree:path ex:something; tree:path ex:somethingElse; ] ) .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-28 - A time fragmentation MUST specify the fragmentation path
    
    #region LSDN-30 - A time fragmentation MAY specify the time buckets
    
    [Fact]
    public async Task AcceptsNoTimeFragmentationBuckets()
    {
        const string definition = """
                                  @prefix tree: <https://w3id.org/tree#> . 
                                  @prefix lsdn: <https://ldes-server.net/ns/> . 
                                  @prefix ex: <https://example.org/> . 
                                  </occupancy/by-page> tree:fragmentationStrategy ( [a lsdn:TimeFragmentation; tree:path ex:something] ) .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public async Task AcceptsMultipleTimeFragmentationBuckets()
    {
        const string definition = """
                                  @prefix tree: <https://w3id.org/tree#> . 
                                  @prefix lsdn: <https://ldes-server.net/ns/> . 
                                  @prefix xsd:  <http://www.w3.org/2001/XMLSchema#> . 
                                  @prefix ex: <https://example.org/> . 
                                  </occupancy/by-page> tree:fragmentationStrategy ( 
                                    [a lsdn:TimeFragmentation; tree:path ex:something; lsdn:bucket 
                                      "P10Y"^^xsd:duration, "P1Y"^^xsd:duration, "P3M"^^xsd:duration, 
                                      "P15D"^^xsd:duration, "P1D"^^xsd:duration, "PT1H"^^xsd:duration, 
                                      "PT15M"^^xsd:duration, "PT30S"^^xsd:duration
                                    ]
                                  ) .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public async Task RefusesNonDurationTimeFragmentationBuckets()
    {
        const string definition = """
                                  @prefix tree: <https://w3id.org/tree#> . 
                                  @prefix lsdn: <https://ldes-server.net/ns/> . 
                                  @prefix xsd:  <http://www.w3.org/2001/XMLSchema#> . 
                                  @prefix ex: <https://example.org/> . 
                                  </occupancy/by-page> tree:fragmentationStrategy ( 
                                    [a lsdn:TimeFragmentation; tree:path ex:something; 
                                      lsdn:bucket "15 days";
                                      lsdn:bucket 60
                                    ] 
                                  ) .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeFalse();
    }
    
    [Fact]
    public async Task RefusesInvalidDurationTimeFragmentationBuckets()
    {
        const string definition = """
                                  @prefix tree: <https://w3id.org/tree#> . 
                                  @prefix lsdn: <https://ldes-server.net/ns/> . 
                                  @prefix xsd:  <http://www.w3.org/2001/XMLSchema#> . 
                                  @prefix ex: <https://example.org/> . 
                                  </occupancy/by-page> tree:fragmentationStrategy ( 
                                    [a lsdn:TimeFragmentation; tree:path ex:something; 
                                      lsdn:bucket "P2W"^^xsd:duration
                                    ] 
                                  ) .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeFalse();
    }
    
    [Fact]
    public async Task RefusesComplexDurationTimeFragmentationBuckets()
    {
        const string definition = """
                                  @prefix tree: <https://w3id.org/tree#> . 
                                  @prefix lsdn: <https://ldes-server.net/ns/> . 
                                  @prefix xsd:  <http://www.w3.org/2001/XMLSchema#> . 
                                  @prefix ex: <https://example.org/> . 
                                  </occupancy/by-page> tree:fragmentationStrategy ( 
                                    [a lsdn:TimeFragmentation; tree:path ex:something; 
                                      lsdn:bucket "P1Y6M"^^xsd:duration
                                    ] 
                                  ) .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateView(stream);
        result.IsValid.Should().BeFalse();
    }
    
    #endregion LSDN-30 - A time fragmentation MAY specify the time buckets

}