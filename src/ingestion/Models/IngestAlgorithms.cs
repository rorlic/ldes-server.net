using LdesServer.Ingestion.Algorithms.CreateMember;
using LdesServer.Ingestion.Algorithms.IdentifyEntity;
using LdesServer.Ingestion.Algorithms.IdentifyMember;
using LdesServer.Ingestion.Algorithms.IdentifyVersion;
using LdesServer.Ingestion.Algorithms.SplitMessage;

namespace LdesServer.Ingestion.Models;

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