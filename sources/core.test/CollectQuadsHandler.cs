using VDS.RDF;
using VDS.RDF.Parsing.Handlers;

namespace AquilaSolutions.LdesServer.Core.Test;

public class CollectQuadsHandler : BaseRdfHandler
{
    private readonly List<Quad> _quads = new();
    
    public IEnumerable<Quad> Quads => _quads.ToArray();
    
    protected override bool HandleTripleInternal(Triple t)
    {
        return HandleQuadInternal(t, null);
    }

    protected override bool HandleQuadInternal(Triple t, IRefNode? graph)
    {
        _quads.Add(new Quad(t, graph));
        return true;
    }

    public override bool AcceptsAll => true;
}