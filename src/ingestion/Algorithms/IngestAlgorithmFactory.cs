using LdesServer.Core.Extensions;
using LdesServer.Core.Namespaces;
using LdesServer.Ingestion.Algorithms.CreateMember;
using LdesServer.Ingestion.Algorithms.IdentifyEntity;
using LdesServer.Ingestion.Algorithms.IdentifyMember;
using LdesServer.Ingestion.Algorithms.IdentifyVersion;
using LdesServer.Ingestion.Algorithms.SplitMessage;
using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms;

/// <summary>
/// Class for creating algorithms
/// </summary>
public class IngestAlgorithmFactory : IIngestAlgorithmFactory
{
    private static string? ExtractStrategyType(IGraph g, INode? config, string strategyTypePrefix)
    {
        if (config is null) return null;
        var rdfType = g.FindObjectBySubjectPredicate(config, g.CreateUriNode(QNames.rdf.type))?.AsValueString();
        return rdfType?.Replace($"{Prefix.ingest}{strategyTypePrefix}", "");
    }

    public ISplitMessageStrategy CreateSplitMessageStrategy(IGraph g)
    {
        var config = g.FindOneByQNamePredicate("ingest:splitMessage")?.Object;
        var type = ExtractStrategyType(g, config, "SplitMessage");
        var predicate = g.CreateUriNode("ingest:p");
        var @object = g.CreateUriNode("ingest:o");
        return type switch
        {
            "AsSingleEntity" => new AsSingleEntityStrategy(),
            "ByNamedGraph" => new ByNamedGraphStrategy(),
            null or "ByNamedNode" => new ByNamedNodeStrategy(),
            "ByPredicateAndObject" => new SplitMessage.ByPredicateAndObjectStrategy(
                g.GetObjectBySubjectPredicate(config!, predicate).AsUriNode(),
                g.FindObjectBySubjectPredicate(config!, @object)),
            _ => throw new ArgumentException($"Unknown algorithm '{type}' for message splitting.")
        };
    }

    public IIdentifyEntityStrategy CreateIdentifyEntityStrategy(IGraph g)
    {
        var config = g.FindOneByQNamePredicate("ingest:identifyEntity")?.Object;
        var type = ExtractStrategyType(g, config, "IdentifyEntity");
        var predicate = g.CreateUriNode("ingest:p");
        var @object = g.CreateUriNode("ingest:o");

        return type switch
        {
            null or "BySingleNamedNode" => new BySingleNamedNodeStrategy(),
            "ByEntityType" => new ByEntityTypeStrategy(
                g.GetObjectBySubjectPredicate(config!, @object)),
            "ByPredicateAndObject" => new IdentifyEntity.ByPredicateAndObjectStrategy(
                g.GetObjectBySubjectPredicate(config!, predicate).AsUriNode(),
                g.GetObjectBySubjectPredicate(config!, @object)),
            _ => throw new ArgumentException($"Unknown algorithm '{type}' for entity identification.")
        };
    }

    public IIdentifyVersionStrategy CreateIdentifyVersionStrategy(IGraph g)
    {
        var config = g.FindOneByQNamePredicate("ingest:identifyVersion")?.Object;
        var type = ExtractStrategyType(g, config, "IdentifyVersion");
        var predicate = g.CreateUriNode("ingest:p");

        return type switch
        {
            null or "WithIngestTimestamp" => new WithIngestTimestampStrategy(),
            "BySubjectAndPredicatePath" => new BySubjectAndPredicatePathStrategy(
                g.GetObjectBySubjectPredicate(config!, predicate).AsUriNode()),
            "BySubjectAndSequencePath" => new BySubjectAndSequencePathStrategy(
                g.GetSequencePath(g.GetObjectBySubjectPredicate(config!, predicate)).ToArray()),
            _ => throw new ArgumentException($"Unknown algorithm '{type}' for version identification.")
        };
    }

    public IIdentifyMemberStrategy CreateIdentifyMemberStrategy(IGraph g)
    {
        var config = g.FindOneByQNamePredicate("ingest:identifyMember")?.Object;
        var type = ExtractStrategyType(g, config, "IdentifyMember");
        var separator = g.CreateUriNode("ingest:separator");
        
        return type switch
        {
            null or "ByEntityIdAndVersion" => new ByEntityIdAndVersionStrategy(config is null
                ? null
                : g.FindObjectBySubjectPredicate(config, separator)?.AsValueString()),
            "WithEntityId" => new WithEntityIdStrategy(),
            _ => throw new ArgumentException($"Unknown algorithm '{type}' for member identification.")
        };
    }

    public ICreateMemberStrategy CreateCreateMemberStrategy(IGraph g)
    {
        var config = g.FindOneByQNamePredicate("ingest:createMember")?.Object;
        var type = ExtractStrategyType(g, config, "CreateMember");
        var predicate = g.CreateUriNode("ingest:p");
        
        return type switch
        {
            null or "AsIs" => new AsIsStrategy(),
            "WithEntityMaterialization" => new WithEntityMaterializationStrategy(
                g.FindObjectBySubjectPredicate(config!, predicate)?.AsUriNode()),
            _ => throw new ArgumentException($"Unknown algorithm '{type}' for member creation.")
        };
    }
}