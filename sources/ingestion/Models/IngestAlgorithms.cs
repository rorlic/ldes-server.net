using AquilaSolutions.LdesServer.Ingestion.Algorithms.CreateMember;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyEntity;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyMember;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyVersion;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.SplitMessage;

namespace AquilaSolutions.LdesServer.Ingestion.Models;

internal class IngestAlgorithms(
    ISplitMessageStrategy splitMessageStrategy,
    IIdentifyEntityStrategy identifyEntityStrategy,
    IIdentifyVersionStrategy searchVersionStrategy,
    IIdentifyMemberStrategy identifyMemberStrategy,
    ICreateMemberStrategy createMemberStrategy)
{
    public ISplitMessageStrategy SplitMessage { get; } = splitMessageStrategy;
    public IIdentifyEntityStrategy IdentifyEntity { get; } = identifyEntityStrategy;
    public IIdentifyVersionStrategy SearchVersion { get; } = searchVersionStrategy;
    public IIdentifyMemberStrategy IdentifyMember { get; } = identifyMemberStrategy;
    public ICreateMemberStrategy CreateMember { get; } = createMemberStrategy;
}