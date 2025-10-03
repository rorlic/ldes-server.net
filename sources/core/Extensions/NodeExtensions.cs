using VDS.RDF;
using VDS.RDF.Nodes;

namespace AquilaSolutions.LdesServer.Core.Extensions;

public static class NodeExtensions
{
    public static string AsValueString(this INode n)
    {
        return n.AsValuedNode().AsString();
    }
    
    public static IUriNode AsUriNode(this INode n)
    {
        return n as IUriNode ?? throw new InvalidCastException();
    }
}