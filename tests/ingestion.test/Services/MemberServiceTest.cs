using System.Data;
using LdesServer.Core.Interfaces;
using LdesServer.Core.Models;
using LdesServer.Core.Models.Configuration;
using LdesServer.Ingestion.Algorithms;
using LdesServer.Ingestion.Services;
using Microsoft.Extensions.Logging;
using Moq;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace LdesServer.Ingestion.Test.Services;

public class MemberServiceTest
{
    [Fact]
    public async Task WhenNoIngestConfigurationProvidedThenUsesDefaultConfiguration()
    {
        const string collectionName = "collection";
        var stream = LoadResource.GetEmbeddedStream("Data.SingleEntityInDefaultGraph.ttl");
        using var reader = new StreamReader(stream);
        
        using var store = new TripleStore();
        var graph = new Graph();
        new TurtleParser().Load(graph, reader);
        store.Add(graph);
        
        var transaction = new Mock<IDbTransaction>();
        var connection = new Mock<IDbConnection>();
        connection.Setup(x => x.BeginTransaction()).Returns(transaction.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IDbConnection))).Returns(connection.Object);

        var collectionRepository = new Mock<ICollectionRepository>();
        var collection = new Collection { Name = collectionName, Definition = $"<{collectionName}> a <https://w3id.org/ldes#EventStream> ." };
        collectionRepository.Setup(x => x.GetCollectionAsync(transaction.Object, collectionName))
            .ReturnsAsync(collection);

        var memberRepositoryMock = new Mock<IMemberRepository>();
        memberRepositoryMock
            .Setup(x => x.StoreMembersAsync(transaction.Object, collection, It.IsAny<IEnumerable<Member>>()))
            .Verifiable();

        var loggerMock = new Mock<ILogger<MemberService>>();
        var sut = new MemberService(serviceProvider.Object, memberRepositoryMock.Object, collectionRepository.Object,
            new IngestAlgorithmFactory(), new LdesServerConfiguration(), loggerMock.Object);
        await sut.IngestCollectionMembersAsync(collectionName, store.Quads);

        memberRepositoryMock.Verify();
    }
}