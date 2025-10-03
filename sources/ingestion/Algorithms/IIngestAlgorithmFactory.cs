using AquilaSolutions.LdesServer.Ingestion.Algorithms.CreateMember;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyEntity;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyMember;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyVersion;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.SplitMessage;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms;

public interface IIngestAlgorithmFactory
{
    ISplitMessageStrategy CreateSplitMessageStrategy(IGraph g);
    IIdentifyEntityStrategy CreateIdentifyEntityStrategy(IGraph g);
    IIdentifyVersionStrategy CreateIdentifyVersionStrategy(IGraph g);
    IIdentifyMemberStrategy CreateIdentifyMemberStrategy(IGraph g);
    ICreateMemberStrategy CreateCreateMemberStrategy(IGraph g);
}