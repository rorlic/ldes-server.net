using System.Data;
using System.Reflection;
using AquilaSolutions.LdesServer.Core.InputFormatters;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Models.Configuration;
using AquilaSolutions.LdesServer.Ingestion.Algorithms;
using AquilaSolutions.LdesServer.Ingestion.Controllers;
using AquilaSolutions.LdesServer.Ingestion.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace AquilaSolutions.LdesServer.Ingestion.Test.Controllers;

public class PostMembersTest
{
    private static readonly LdesServerConfiguration Configuration = new() { BaseUri = "http://dummy.org/" };

    private MemberController CreateSut()
    {
        return new MemberController();
    }

    [Fact]
    public async Task RefusesMessageForNonExistingCollection()
    {
        var transaction = new Mock<IDbTransaction>();
        var connection = new Mock<IDbConnection>();
        connection.Setup(x => x.BeginTransaction()).Returns(transaction.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IDbConnection))).Returns(connection.Object);

        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetCollectionAsync(transaction.Object, It.IsAny<string>()))
            .ReturnsAsync(() => null);
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var loggerMock = new Mock<ILogger<MemberService>>();
        
        const string resourceName = "Data.SingleEntityInDefaultGraph.ttl";
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName().Name;
        var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"{assemblyName}.Resources.{resourceName}");

        var result = await CreateSut().CreateMembers("non-existing-collection", stream!, RdfMimeTypes.Turtle,
                new MemberService(serviceProvider.Object, memberRepositoryMock.Object, collectionRepositoryMock.Object,
                    new IngestAlgorithmFactory(), Configuration, loggerMock.Object),
                new LinkedDataReader(Configuration))
            .ConfigureAwait(false);

        collectionRepositoryMock.Verify(x => x.GetCollectionAsync(transaction.Object, It.IsAny<string>()), Times.Once);
        result.As<IStatusCodeActionResult>().StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task AcceptsSingleEntityMessageAndStoresOneMember()
    {
        const string collectionName = "some-collection";
        const string resourceName = "Data.SingleEntityInDefaultGraph.ttl";

        const string definition = $"""
                                   @prefix lsdn:   <https://ldes-server.net/> .
                                   @prefix ingest: <https://ldes-server.net/ingest#> .
                                   <{collectionName}>  a <https://w3id.org/ldes#EventStream> ;
                                                   lsdn:ingestion [
                                                      ingest:splitMessage [a ingest:SplitMessageAsSingleEntity];
                                                      ingest:identifyEntity [a ingest:IdentifyEntityBySingleNamedNode];
                                                      ingest:identifyVersion [a ingest:IdentifyVersionBySubjectAndPredicatePath; 
                                                         ingest:p <http://purl.org/dc/terms/created>];
                                                      ingest:identifyMember [a ingest:IdentifyMemberByEntityIdAndVersion; 
                                                         ingest:separator "/"];
                                                      ingest:createMember [a ingest:CreateMemberAsIs ]
                                                   ] .
                                   """;

        var transaction = new Mock<IDbTransaction>();
        var connection = new Mock<IDbConnection>();
        connection.Setup(x => x.BeginTransaction()).Returns(transaction.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IDbConnection))).Returns(connection.Object);

        var collection = new Collection { Name = collectionName, Definition = definition };
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetCollectionAsync(transaction.Object, collectionName))
            .ReturnsAsync(() => collection);

        const string expectedMemberId = "http://en.wikipedia.org/wiki/Robin_Hood/2025-01-20T11:38:00.000+01:00";

        var memberRepositoryMock = new Mock<IMemberRepository>();
        memberRepositoryMock.Setup(x =>
                x.StoreMembersAsync(transaction.Object, collection, It.IsAny<IEnumerable<Member>>()))
            .ReturnsAsync(() => [expectedMemberId]);

        var loggerMock = new Mock<ILogger<MemberService>>();
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName().Name;
        var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"{assemblyName}.Resources.{resourceName}");

        // returns HTTP 201 and generated member identifier ID(s), sends notification for each member created
        var result = await CreateSut().CreateMembers(collectionName, stream!, RdfMimeTypes.Turtle,
                new MemberService(serviceProvider.Object, memberRepositoryMock.Object, collectionRepositoryMock.Object,
                    new IngestAlgorithmFactory(), Configuration, loggerMock.Object),
                new LinkedDataReader(Configuration))
            .ConfigureAwait(false);

        memberRepositoryMock.Verify(
            x => x.StoreMembersAsync(transaction.Object, collection, It.IsAny<IEnumerable<Member>>()),
            Times.Once);

        var json = result as JsonResult;
        json.Should().NotBeNull();
        json.Value.Should().BeOfType<string[]>().And.BeEquivalentTo(new[] { expectedMemberId });
        json.StatusCode.Should().Be(StatusCodes.Status201Created);
    }
}