using System.Data;
using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models.Configuration;
using AquilaSolutions.LdesServer.Core.Namespaces;
using AquilaSolutions.LdesServer.Ingestion.Algorithms;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.CreateMember;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyEntity;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyMember;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyVersion;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.SplitMessage;
using AquilaSolutions.LdesServer.Ingestion.Extensions;
using AquilaSolutions.LdesServer.Ingestion.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VDS.RDF;
using VDS.RDF.Nodes;

namespace AquilaSolutions.LdesServer.Ingestion.Services;

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

    private static IngestAlgorithms IngestStateObjectsDefaults(IGraph g)
    {
        var separator = g.FindOneByQNamePredicate(QNames.ldes.versionDelimiter)?.Object.AsValueString() ?? "/";
        return new IngestAlgorithms(
            new ByNamedNodeStrategy(),
            new BySingleNamedNodeStrategy(),
            new WithIngestTimestampStrategy(),
            new ByEntityIdAndVersionStrategy(separator),
            new AsIsStrategy());
    }

    private static IngestAlgorithms IngestVersionObjectDefaults(IGraph g)
    {
        var timestampPath = g.FindOneByQNamePredicate(QNames.ldes.timestampPath)?.Object?.AsUriNode();
        var versionOfPath = g.FindOneByQNamePredicate(QNames.ldes.versionOfPath)?.Object?.AsUriNode();
        return new IngestAlgorithms(
            new AsSingleEntityStrategy(),
            new BySingleNamedNodeStrategy(),
            new BySubjectAndPredicatePathStrategy(timestampPath ?? g.CreateUriNode(QNames.prov.generatedAtTime)),
            new WithEntityIdStrategy(),
            new WithEntityMaterializationStrategy(versionOfPath ?? g.CreateUriNode(QNames.dct.isVersionOf)));
    }

    private IngestAlgorithms CreateAlgorithms(IGraph g)
    {
        if (configuration.Compatible)
        {
            // Returns the LDESServer4J defaults for ingestion
            // See https://informatievlaanderen.github.io/VSDS-LDESServer4J/3.6.2/configuration/event-stream for more info.
            var createVersions =
                g.FindOneByQNamePredicate(QNames.ldes.createVersions)?.Object?.AsValuedNode()?.AsBoolean() ?? false;
            return createVersions ? IngestStateObjectsDefaults(g) : IngestVersionObjectDefaults(g);
        }

        return new IngestAlgorithms(
            algorithmFactory.CreateSplitMessageStrategy(g),
            algorithmFactory.CreateIdentifyEntityStrategy(g),
            algorithmFactory.CreateIdentifyVersionStrategy(g),
            algorithmFactory.CreateIdentifyMemberStrategy(g),
            algorithmFactory.CreateCreateMemberStrategy(g)
        );
    }
}