using System.Data;
using AquilaSolutions.LdesServer.Administration.Services;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Models.Configuration;
using FluentAssertions;
using Moq;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace AquilaSolutions.LdesServer.Administration.Test.Services;

public class CollectionServiceTests
{
    private static CollectionService CreateSut(Mock<IDbTransaction> transaction,
        Mock<ICollectionRepository> collectionRepository, Mock<IViewRepository> viewRepository, Collection collection)
    {
        var connection = new Mock<IDbConnection>();
        connection.Setup(x => x.BeginTransaction()).Returns(transaction.Object);

        collectionRepository
            .Setup(x => x.CreateCollectionAsync(transaction.Object, collection.Name, It.IsAny<string>()))
            .ReturnsAsync(collection);

        viewRepository
            .Setup(x => x.CreateViewAsync(transaction.Object, It.IsAny<Collection>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new View { Name = string.Empty });

        return new CollectionService(
            new LdesServerConfiguration(), connection.Object, collectionRepository.Object, viewRepository.Object);
    }

    [Fact]
    public async Task DoesNotCreateCollectionIfDefinitionInvalid()
    {
        var transaction = new Mock<IDbTransaction>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var viewRepository = new Mock<IViewRepository>();

        const string collectionName = "collection";
        const string collectionDefinition = "[] a <https://w3id.org/ldes#EventStream> .";
        var collection = new Collection { Name = collectionName, Definition = collectionDefinition };

        var sut = CreateSut(transaction, collectionRepository, viewRepository, collection);

        using var definition = new Graph();
        StringParser.Parse(definition, collectionDefinition, new TurtleParser());

        var result = await sut.CreateCollectionAsync(definition);
        result.IsValid.Should().BeFalse();

        collectionRepository.Verify(
            x => x.CreateCollectionAsync(transaction.Object, collectionName, It.IsAny<string>()), Times.Never);
        viewRepository.Verify(
            x => x.CreateViewAsync(transaction.Object, It.IsAny<Collection>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatesCollectionIfDefinitionValid()
    {
        var transaction = new Mock<IDbTransaction>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var viewRepository = new Mock<IViewRepository>();

        const string collectionName = "collection";
        const string collectionDefinition =
            $"@prefix ldes: <https://w3id.org/ldes#> . <http://example.org/{collectionName}>  a ldes:EventStream .";
        var collection = new Collection { Name = collectionName, Definition = collectionDefinition };

        var sut = CreateSut(transaction, collectionRepository, viewRepository, collection);

        using var definition = new Graph();
        StringParser.Parse(definition, collectionDefinition, new TurtleParser());

        var result = await sut.CreateCollectionAsync(definition);
        result.IsValid.Should().BeTrue();

        collectionRepository.Verify(x =>
            x.CreateCollectionAsync(transaction.Object, collectionName, It.IsAny<string>()));
        viewRepository.Verify(x =>
            x.CreateViewAsync(transaction.Object, collection, It.IsAny<string>(), It.IsAny<string>()));
    }
}