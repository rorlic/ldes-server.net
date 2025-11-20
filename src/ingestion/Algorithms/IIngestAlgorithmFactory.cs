using LdesServer.Ingestion.Algorithms.CreateMember;
using LdesServer.Ingestion.Algorithms.IdentifyEntity;
using LdesServer.Ingestion.Algorithms.IdentifyMember;
using LdesServer.Ingestion.Algorithms.IdentifyVersion;
using LdesServer.Ingestion.Algorithms.SplitMessage;
using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms;

public interface IIngestAlgorithmFactory
{
    ISplitMessageStrategy CreateSplitMessageStrategy(IGraph g);
    IIdentifyEntityStrategy CreateIdentifyEntityStrategy(IGraph g);
    IIdentifyVersionStrategy CreateIdentifyVersionStrategy(IGraph g);
    IIdentifyMemberStrategy CreateIdentifyMemberStrategy(IGraph g);
    ICreateMemberStrategy CreateCreateMemberStrategy(IGraph g);
}