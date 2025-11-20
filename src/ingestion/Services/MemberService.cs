using System.Data;
using LdesServer.Core.Extensions;
using LdesServer.Core.Interfaces;
using LdesServer.Core.Models.Configuration;
using LdesServer.Ingestion.Algorithms;
using LdesServer.Ingestion.Extensions;
using LdesServer.Ingestion.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VDS.RDF;

namespace LdesServer.Ingestion.Services;

public class MemberService(
    IServiceProvider serviceProvider,
    IMemberRepository memberRepository,
    ICollectionRepository collectionRepository,
    IIngestAlgorithmFactory algorithmFactory,
    LdesServerConfiguration configuration,
    ILogger<MemberService> logger)
{
    public async Task<IEnumerable<string>?> IngestCollectionMembersAsync(string collectionName, IEnumerable<Quad> message)
    {
        using var connection = serviceProvider.GetRequiredService<IDbConnection>();
        using var transaction = connection.BeginTransaction();

        var collection = await collectionRepository.GetCollectionAsync(transaction, collectionName)
            .ConfigureAwait(false);
        if (collection is null) return null;

        using var g = collection.ParseDefinition(configuration.GetBaseUri()).WithServerPrefixes();

        var algorithms = CreateAlgorithms(g);
        var createdAt = DateTimeOffset.UtcNow;
        var members = message.SplitIntoEntitiesUsing(algorithms.SplitMessage).Select(quads =>
        {
            var entityId = algorithms.IdentifyEntity.SearchEntityIdentifier(quads);
            var version = algorithms.SearchVersion.FindOrCreateEntityVersion(quads, entityId, createdAt);
            var memberId = algorithms.IdentifyMember.FindOrCreateMemberIdentifier(quads, entityId, version);
            return algorithms.CreateMember.CreateMember(quads, memberId, entityId, createdAt);
        }).ToArray();

        var result = (await memberRepository.StoreMembersAsync(transaction, collection, members)).ToList();
        transaction.Commit();
        result.ForEach(x => logger.LogInformation($"Ingested member: {x}"));
        return result;
    }

    private IngestAlgorithms CreateAlgorithms(IGraph g)
    {
        return new IngestAlgorithms(
            algorithmFactory.CreateSplitMessageStrategy(g),
            algorithmFactory.CreateIdentifyEntityStrategy(g),
            algorithmFactory.CreateIdentifyVersionStrategy(g),
            algorithmFactory.CreateIdentifyMemberStrategy(g),
            algorithmFactory.CreateCreateMemberStrategy(g)
        );
    }
}