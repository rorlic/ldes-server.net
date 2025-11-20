using System.Data;
using LdesServer.Administration.Services;
using LdesServer.Core.Extensions;
using LdesServer.Core.Interfaces;
using LdesServer.Core.Models;
using FluentAssertions;
using Moq;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace LdesServer.Administration.Test.Services;

public class ViewServiceTests
{
    private static ViewService CreateSut(Mock<IDbTransaction> transaction,
        Mock<ICollectionRepository> collectionRepository, 
        Mock<IViewRepository> viewRepository,
        Collection? collection = null, View? view = null)
    {
        var connection = new Mock<IDbConnection>();
        connection.Setup(x => x.BeginTransaction()).Returns(transaction.Object);

        collectionRepository
            .Setup(x => x.GetCollectionAsync(transaction.Object, It.IsAny<string>()))
            .ReturnsAsync(collection);

        viewRepository
            .Setup(x => x.CreateViewAsync(transaction.Object, It.IsAny<Collection>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(view);

        return new ViewService(connection.Object, collectionRepository.Object,
            viewRepository.Object);
    }

    [Fact]
    public async Task DoesNotCreateViewIfCollectionNotFound()
    {
        var transaction = new Mock<IDbTransaction>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var viewRepository = new Mock<IViewRepository>();

        var sut = CreateSut(transaction, collectionRepository, viewRepository);

        using var definition = new Graph();

        var result = await sut.CreateViewAsync(definition, "unknown");
        result.Should().BeNull();

        collectionRepository.Verify(x => x.GetCollectionAsync(transaction.Object, It.IsAny<string>()));
        viewRepository.Verify(
            x => x.CreateViewAsync(transaction.Object, It.IsAny<Collection>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task DoesNotCreateViewIfDefinitionInvalid()
    {
        var transaction = new Mock<IDbTransaction>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var viewRepository = new Mock<IViewRepository>();

        const string collectionName = "collection";
        var collection = new Collection { Name = collectionName, Definition = string.Empty };

        var sut = CreateSut(transaction, collectionRepository, viewRepository, collection);

        const string viewName = "view";
        const string collectionDefinition =
            $"@prefix tree: <https://w3id.org/tree#> . <http://example.org/{collectionName}/{viewName}> tree:pageSize 100, 250 .";

        using var definition = new Graph();
        StringParser.Parse(definition, collectionDefinition);

        var result = await sut.CreateViewAsync(definition, collectionName);
        result?.IsValid.Should().BeFalse();

        collectionRepository.Verify(x => x.GetCollectionAsync(transaction.Object, It.IsAny<string>()));
        viewRepository.Verify(
            x => x.CreateViewAsync(transaction.Object, It.IsAny<Collection>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ThrowsExceptionIfViewIriNotFound()
    {
        var transaction = new Mock<IDbTransaction>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var viewRepository = new Mock<IViewRepository>();

        const string collectionName = "something";
        var collection = new Collection { Name = collectionName, Definition = string.Empty };

        var sut = CreateSut(transaction, collectionRepository, viewRepository, collection);

        var action = async () =>
        {
            using var definition = new Graph();
            definition.NamespaceMap.AddNamespace("tree", new Uri("https://w3id.org/tree#"));
            return await sut.CreateViewAsync(definition, collectionName);
        };
        await action.Should().ThrowAsync<ArgumentException>();

        collectionRepository.Verify(x => x.GetCollectionAsync(transaction.Object, It.IsAny<string>()));
        viewRepository.Verify(
            x => x.CreateViewAsync(transaction.Object, It.IsAny<Collection>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatesViewIfDefinitionValid()
    {
        var transaction = new Mock<IDbTransaction>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var viewRepository = new Mock<IViewRepository>();

        const string collectionName = "collection";
        const string viewName = "view";
        var collection = new Collection { Name = collectionName, Definition = string.Empty };
        var view = new View { Name = viewName };

        var sut = CreateSut(transaction, collectionRepository, viewRepository, collection, view);

        const string viewDefinition =
            $"@prefix tree: <https://w3id.org/tree#> . <http://example.org/{collectionName}/{viewName}> tree:pageSize 100 .";

        using var definition = new Graph();
        StringParser.Parse(definition, viewDefinition, new TurtleParser());

        var result = await sut.CreateViewAsync(definition, collectionName);
        result?.IsValid.Should().BeTrue();

        collectionRepository.Verify(x => x.GetCollectionAsync(transaction.Object, collectionName));
        viewRepository.Verify(x => x.CreateViewAsync(transaction.Object, collection, viewName, It.IsAny<string>()));
    }
}