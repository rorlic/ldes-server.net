using LdesServer.Core.Test.Extensions;
using LdesServer.Administration.Validators;
using FluentAssertions;
using FluentValidation.Results;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace LdesServer.Administration.Test.Validators;

public class CollectionValidatorTests
{
    private static ValidationResult ValidateCollection(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var data = new Graph();

        var parser = new TurtleParser();
        parser.Load(data, reader);

        return new CollectionValidator().Validate(data);
    }

    #region LSDN-01 - A collection definition MUST contain exactly one ldes:EventStream

    [Fact]
    public async Task AcceptsSingleCollection()
    {
        const string definition = """
                                  </occupancy> a <https://w3id.org/ldes#EventStream> . 
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesDuplicateDefinition()
    {
        const string definition = """
                                  </occupancy> a <https://w3id.org/ldes#EventStream> . 
                                  </occupancy2> a <https://w3id.org/ldes#EventStream> .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-01 - A collection definition MUST contain exactly one ldes:EventStream

    #region LSDN-02 - A collection MUST be identified using a named node (and not a blank node)

    [Fact]
    public async Task RefusesUnnamedDefinition()
    {
        const string definition = """
                                  [] a <https://w3id.org/ldes#EventStream> .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AcceptsNamedNode()
    {
        const string definition = """
                                  <https://example.org/feed/occupancy> a <https://w3id.org/ldes#EventStream> . 
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    #endregion LSDN-02 - A collection MUST be identified using a named node (and not a blank node)

    #region LSDN-03 - A collection MAY contain at most one set of ingestion algorithms

    [Fact]
    public async Task AcceptsMissingSetOfIngestAlgorithms()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptsSingleSetOfIngestAlgorithms()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultipleSetsOfIngestAlgorithms()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  </occupancy> a <https://w3id.org/ldes#EventStream> ;
                                               lsdn:ingestion [], [] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-03 - A collection MAY contain at most one set of ingestion algorithms

    #region LSDN-04 - A collection MAY define at most once how a message is split into entities

    [Fact]
    public async Task AcceptsMissingSplitMessageAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptsOneSplitMessageAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:splitMessage [ a ingest:SplitMessageAsSingleEntity ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultipleSplitMessageAlgorithms()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:splitMessage [
                                                           a ingest:SplitMessageAsSingleEntity
                                                       ], [
                                                           a ingest:SplitMessageAsSingleEntity
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-04 - A collection MAY define at most once how a message is split into entities

    #region LSDN-05 - A collection MUST only use one of the well-known message splitting algorithms

    [Theory]
    [InlineData("SplitMessageAsSingleEntity")]
    [InlineData("SplitMessageByNamedGraph")]
    [InlineData("SplitMessageByNamedNode")]
    // [InlineData("SplitMessageByPredicateAndObject")] - checked by other tests
    public async Task AcceptsWellKnownSplitMessageAlgorithm(string algorithmName)
    {
        var definition = $"""
                          @prefix lsdn:   <https://ldes-server.net/ns/> .
                          @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                          </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                          lsdn:ingestion [
                                              ingest:splitMessage [ a ingest:{algorithmName} ]
                                          ] .
                          """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesUnknownSplitMessageAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:splitMessage [
                                                           a ingest:UnknownSplitMessageAlgorithm
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-05 - A collection MUST only use one of the well-known message splitting algorithms

    #region LSDN-06 - A SplitMessageByPredicateAndObject MUST specify the predicate and object paths

    [Fact]
    public async Task AcceptsSplitMessageByPredicateAndObjectAlgorithmWithPredicateAndObject()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:splitMessage [ a ingest:SplitMessageByPredicateAndObject ;
                                                          ingest:p <http://example.org/something> ;
                                                          ingest:o <http://example.org/value>
                                                      ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ingest:p")]
    [InlineData("ingest:o")]
    public async Task RefusesSplitMessageByPredicateAndObjectAlgorithmWithoutPredicateOrObject(string property)
    {
        var definition = $"""
                          @prefix lsdn:   <https://ldes-server.net/ns/> .
                          @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                          </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                          lsdn:ingestion [
                                               ingest:splitMessage [a ingest:SplitMessageByPredicateAndObject ;
                                                 {property} <http://example.org/dummy>
                                               ]
                                          ] .
                          """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-06 - A SplitMessageByPredicateAndObject MUST specify the predicate and object paths

    #region LSDN-07 - A collection MAY define at most once how an entity is identified

    [Fact]
    public async Task AcceptsMissingIdentifyEntityAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptsOneIdentifyEntityAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:identifyEntity [ a ingest:IdentifyEntityBySingleNamedNode ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultipleIdentifyEntityAlgorithms()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyEntity [
                                                           a ingest:IdentifyEntityBySingleNamedNode
                                                       ], [
                                                           a ingest:IdentifyEntityBySingleNamedNode
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region LSDN-08 - A collection MUST only use one of the well-known entity identification algorithms

    [Theory]
    // [InlineData("IdentifyEntityByEntityType")] - checked by other tests
    // [InlineData("IdentifyEntityByPredicateAndObject")] - checked by other tests
    [InlineData("IdentifyEntityBySingleNamedNode")]
    public async Task AcceptsWellKnownIdentifyEntityAlgorithm(string algorithmName)
    {
        var definition = $"""
                          @prefix lsdn:   <https://ldes-server.net/ns/> .
                          @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                          </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                          lsdn:ingestion [
                                              ingest:identifyEntity [ a ingest:{algorithmName} ]
                                          ] .
                          """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesUnknownIdentifyEntityAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyEntity [
                                                           a ingest:UnknownIdentifyEntityAlgorithm
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region LSDN-09 - An IdentifyEntityByEntityType MUST specify the object path

    [Fact]
    public async Task AcceptsIdentifyEntityByEntityTypeAlgorithmWithObject()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:identifyEntity [ a ingest:IdentifyEntityByEntityType ;
                                                          ingest:o <http://example.org/value>
                                                      ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesIdentifyEntityByEntityTypeAlgorithmWithoutObject()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyEntity [a ingest:IdentifyEntityByEntityType
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region LSDN-10 - An IdentifyEntityByPredicateAndObject MUST specify the predicate and object paths

    [Fact]
    public async Task AcceptsIdentifyEntityByPredicateAndObjectAlgorithmWithPredicateAndObject()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:identifyEntity [ a ingest:IdentifyEntityByPredicateAndObject ;
                                                          ingest:p <http://example.org/something> ;
                                                          ingest:o <http://example.org/value>
                                                      ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ingest:p")]
    [InlineData("ingest:o")]
    public async Task RefusesIdentifyEntityByPredicateAndObjectAlgorithmWithoutPredicateOrObject(string property)
    {
        var definition = $"""
                          @prefix lsdn:   <https://ldes-server.net/ns/> .
                          @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                          </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                          lsdn:ingestion [
                                               ingest:identifyEntity [a ingest:IdentifyEntityByPredicateAndObject ;
                                                 {property} <http://example.org/dummy>
                                               ]
                                          ] .
                          """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region LSDN-11 - A collection MAY define at most once how a version is identified

    [Fact]
    public async Task AcceptsMissingIdentifyVersionAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptsOneIdentifyVersionAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:identifyVersion [ a ingest:IdentifyVersionWithIngestTimestamp ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultipleIdentifyVersionAlgorithms()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:splitMessage [
                                                           a ingest:IdentifyVersionWithIngestTimestamp
                                                       ], [
                                                           a ingest:IdentifyVersionWithIngestTimestamp
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-11 - A collection MAY define at most once how a version is identified

    #region LSDN-12 - A collection MUST only use one of the well-known version identification algorithms

    [Theory]
    // [InlineData("IdentifyVersionBySubjectAndPredicate")] - checked by other tests
    [InlineData("IdentifyVersionWithIngestTimestamp")]
    public async Task AcceptsWellKnownIdentifyVersionAlgorithm(string algorithmName)
    {
        var definition = $"""
                          @prefix lsdn:   <https://ldes-server.net/ns/> .
                          @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                          </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                          lsdn:ingestion [
                                              ingest:identifyVersion [ a ingest:{algorithmName} ]
                                          ] .
                          """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesUnknownIdentifyVersionAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyVersion [
                                                           a ingest:UnknownIdentifyVersionAlgorithm
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-12 - A collection MUST only use one of the well-known version identification algorithms

    #region LSDN-13 - An IdentifyVersionBySubjectAndPredicatePath MUST specify the predicate path

    [Fact]
    public async Task AcceptsIdentifyVersionBySubjectAndPredicatePathAlgorithmWithPredicate()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:identifyVersion [ 
                                                        a ingest:IdentifyVersionBySubjectAndPredicatePath ;
                                                        ingest:p <http://example.org/something>
                                                      ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesIdentifyVersionBySubjectAndPredicatePathAlgorithmWithoutPredicate()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyVersion [
                                                            a ingest:IdentifyVersionBySubjectAndPredicatePath
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-13 - An IdentifyVersionBySubjectAndPredicatePath MUST specify the predicate path

    #region LSDN-14 - A collection MAY define at most once how a member is identified

    [Fact]
    public async Task AcceptsMissingIdentifyMemberAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptsOneIdentifyMemberAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:identifyMember [ a ingest:IdentifyMemberWithEntityId ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultipleIdentifyMemberAlgorithms()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyMember [
                                                           a ingest:IdentifyMemberWithEntityId
                                                       ], [
                                                           a ingest:IdentifyMemberWithEntityId
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-14 - A collection MAY define at most once how a member is identified

    #region LSDN-15 - A collection MUST only use one of the well-known member identification algorithms

    [Theory]
    // [InlineData("IdentifyMemberByEntityAndVersion")] - checked by other tests
    [InlineData("IdentifyMemberWithEntityId")]
    public async Task AcceptsWellKnownIdentifyMemberAlgorithm(string algorithmName)
    {
        var definition = $"""
                          @prefix lsdn:   <https://ldes-server.net/ns/> .
                          @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                          </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                          lsdn:ingestion [
                                              ingest:identifyMember [ a ingest:{algorithmName} ]
                                          ] .
                          """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesUnknownIdentifyMemberAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyMember [
                                                           a ingest:UnknownIdentifyMemberAlgorithm
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-15 - A collection MUST only use one of the well-known member identification algorithms

    #region LSDN-16 - An IdentifyMemberByEntityIdAndVersion MAY specify a string separator

    [Fact]
    public async Task AcceptsIdentifyMemberByEntityAndVersionAlgorithmWithStringSeparator()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:identifyMember [ 
                                                        a ingest:IdentifyMemberByEntityIdAndVersion ;
                                                        ingest:separator "/"
                                                      ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("<http://example.com/invalid-separator>")]
    [InlineData("[a ingest:invalid-separator]")]
    [InlineData("123")]
    [InlineData("\"23\"^^<http://www.w3.org/2001/XMLSchema#integer>")]
    public async Task RefusesIdentifyMemberByEntityAndVersionAlgorithmWithNonStringSeparator(string separator)
    {
        var definition = $"""
                          @prefix lsdn:   <https://ldes-server.net/ns/> .
                          @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                          </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                          lsdn:ingestion [
                                               ingest:identifyMember [ 
                                                a ingest:IdentifyMemberByEntityIdAndVersion ;
                                                ingest:separator {separator}
                                              ]
                                          ] .
                          """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-16 - An IdentifyMemberByEntityIdAndVersion MAY specify a string separator

    #region LSDN-17 - A collection MAY define at most once how a member is created

    [Fact]
    public async Task AcceptsMissingCreateMemberAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptsOneCreateMemberAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:createMember [ a ingest:CreateMemberAsIs ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultipleCreateMemberAlgorithms()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:createMember [
                                                           a ingest:CreateMemberAsIs
                                                       ], [
                                                           a ingest:CreateMemberAsIs
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-17 - A collection MAY define at most once how a member is created

    #region LSDN-18 - A collection MUST only use one of the well-known member creation algorithms

    [Theory]
    // [InlineData("CreateMemberWithEntityMaterialization")] - checked by other tests
    [InlineData("CreateMemberAsIs")]
    public async Task AcceptsWellKnownCreateMemberAlgorithm(string algorithmName)
    {
        var definition = $"""
                          @prefix lsdn:   <https://ldes-server.net/ns/> .
                          @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                          </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                          lsdn:ingestion [
                                              ingest:createMember [ a ingest:{algorithmName} ]
                                          ] .
                          """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesUnknownCreateMemberAlgorithm()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:createMember [
                                                           a ingest:UnknownCreateMemberAlgorithm
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-18 - A collection MUST only use one of the well-known member creation algorithms

    #region LSDN-19 - A CreateMemberWithEntityMaterialization MAY specify an IRI predicate

    [Fact]
    public async Task AcceptsCreateMemberWithEntityMaterializationAlgorithmWithIriPredicate()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:createMember [ 
                                                        a ingest:CreateMemberWithEntityMaterialization ;
                                                        ingest:p <https://example.org/some-predicate>
                                                      ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("\"invalid-predicate\"")]
    [InlineData("[a ingest:invalid-predicate]")]
    [InlineData("123")]
    [InlineData("\"23\"^^<http://www.w3.org/2001/XMLSchema#integer>")]
    public async Task RefusesCreateMemberWithEntityMaterializationAlgorithmWithNonIriPredicate(string predicate)
    {
        var definition = $"""
                          @prefix lsdn:   <https://ldes-server.net/ns/> .
                          @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                          </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                          lsdn:ingestion [
                                               ingest:createMember [ 
                                                a ingest:CreateMemberWithEntityMaterialization ;
                                                ingest:p {predicate}
                                              ]
                                          ] .
                          """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-19 - A CreateMemberWithEntityMaterialization MAY specify a predicate

    #region LSDN-25 - A collection MAY define at most one timestamp path as a predicate path
    
    // NOTE: ldes:timestampPath: this is a SHACL property path that identifies an xsd:dateTime literal within each member.
    //       This timestamp determines the chronological order in which members of the event stream are added.
    // TODO: When ldes:timestampPath is set, no member can be added to the LDES with a timestamp earlier than the latest published member.
    
    [Fact]
    public async Task AcceptsMissingTimestampPath()
    {
        const string definition = """
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptsOneTimestampPath()
    {
        const string definition = """
                                  @prefix ldes:   <https://w3id.org/ldes#> .
                                  @prefix dct:    <http://purl.org/dc/terms/> .
                                  </occupancy>    a ldes:EventStream ;
                                                  ldes:timestampPath dct:created .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultipleTimestampPaths()
    {
        const string definition = """
                                  @prefix ldes:   <https://w3id.org/ldes#> .
                                  @prefix dct:    <http://purl.org/dc/terms/> .
                                  </occupancy>    a ldes:EventStream ;
                                                  ldes:timestampPath dct:created, dct:modified .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RefusesTimestampPathWithNonPredicatePath()
    {
        const string definition = """
                                  @prefix ldes:   <https://w3id.org/ldes#> .
                                  @prefix dct:    <http://purl.org/dc/terms/> .
                                  @prefix ex:     <http://example.org/> .
                                  </occupancy>    a ldes:EventStream ;
                                                  ldes:timestampPath ( ex:something dct:created ) .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-25 - A collection MAY define at most one timestamp path as a predicate path

    #region LSDN-26 - A collection MAY define at most one version-of path as a predicate path
    
    // NOTE: ldes:versionOfPath: when your entities are versioned, this property points at the object that tells you what entity it is a version of (e.g., dcterms:isVersionOf).
    
    [Fact]
    public async Task AcceptsMissingVersionOfPath()
    {
        const string definition = """
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptsOneVersionOfPath()
    {
        const string definition = """
                                  @prefix ldes:   <https://w3id.org/ldes#> .
                                  @prefix ex:     <http://example.org/> .
                                  @prefix dct:    <http://purl.org/dc/terms/> .
                                  </occupancy>    a ldes:EventStream ;
                                                  ldes:versionOfPath dct:isVersionOf .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesMultipleVersionOfPaths()
    {
        const string definition = """
                                  @prefix ldes:   <https://w3id.org/ldes#> .
                                  @prefix dct:    <http://purl.org/dc/terms/> .
                                  @prefix ex:     <http://example.org/> .
                                  </occupancy>    a ldes:EventStream ;
                                                  ldes:versionOfPath dct:isVersionOf, ex:somethingElse .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RefusesVersionOfPathWithNonPredicatePath()
    {
        const string definition = """
                                  @prefix ldes:   <https://w3id.org/ldes#> .
                                  @prefix ex:     <http://example.org/> .
                                  @prefix dct:    <http://purl.org/dc/terms/> .
                                  </occupancy>    a ldes:EventStream ;
                                                  ldes:versionOfPath ( ex:something dct:isVersionOf ) .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-26 - A collection MAY define at most one version-of path as a predicate path
   
    #region LSDN-29 - An IdentifyVersionBySubjectAndSequencePath MUST specify the sequence path

    [Fact]
    public async Task AcceptsIdentifyVersionBySubjectAndSequencePathAlgorithmWithSingleItemList()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  @prefix ex:     <http://example.org/> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:identifyVersion [ 
                                                        a ingest:IdentifyVersionBySubjectAndSequencePath ;
                                                        ingest:p (ex:something)
                                                      ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptsIdentifyVersionBySubjectAndSequencePathAlgorithmWithMultipleItemList()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  @prefix ex:     <http://example.org/> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                      ingest:identifyVersion [ 
                                                        a ingest:IdentifyVersionBySubjectAndSequencePath ;
                                                        ingest:p (ex:something ex:SomethingElse)
                                                      ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefusesIdentifyVersionBySubjectAndSequencePathAlgorithmWithoutPredicate()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyVersion [
                                                            a ingest:IdentifyVersionBySubjectAndSequencePath
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RefusesIdentifyVersionBySubjectAndSequencePathAlgorithmWithEmptyPredicate()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyVersion [
                                                            a ingest:IdentifyVersionBySubjectAndSequencePath ;
                                                            ingest:p ()
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RefusesIdentifyVersionBySubjectAndSequencePathAlgorithmWithPredicatePath()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  @prefix ex:     <http://example.org/> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyVersion [
                                                            a ingest:IdentifyVersionBySubjectAndSequencePath ;
                                                            ingest:p ex:something
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RefusesIdentifyVersionBySubjectAndSequencePathAlgorithmWithNonListPredicate()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyVersion [
                                                            a ingest:IdentifyVersionBySubjectAndSequencePath ;
                                                            ingest:p "not-a-list"
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RefusesIdentifyVersionBySubjectAndSequencePathAlgorithmWithIncorrectSequencePath ()
    {
        const string definition = """
                                  @prefix lsdn:   <https://ldes-server.net/ns/> .
                                  @prefix ingest: <https://ldes-server.net/ns/ingest#> .
                                  @prefix ex:     <http://example.org/> .
                                  </occupancy>    a <https://w3id.org/ldes#EventStream> ;
                                                  lsdn:ingestion [
                                                       ingest:identifyVersion [
                                                            a ingest:IdentifyVersionBySubjectAndSequencePath ;
                                                            ingest:p ( ex:something "not-a-predicate" )
                                                       ]
                                                  ] .
                                  """;
        using var stream = await definition.AsStream();
        var result = ValidateCollection(stream);
        result.IsValid.Should().BeFalse();
    }

    #endregion LSDN-29 - An IdentifyVersionBySubjectAndSequencePath MUST specify the sequence path
    
}