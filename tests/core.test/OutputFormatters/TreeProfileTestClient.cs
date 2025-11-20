using VDS.RDF;
using VDS.RDF.Parsing.Handlers;

namespace LdesServer.Core.Test.OutputFormatters;

internal class TreeProfileTestClient(string baseIri) : BaseRdfHandler
{
    private bool ReadingHypermedia => _member is null;

    private readonly List<Quad> _hypermedia = new();
    private readonly List<List<Quad>> _members = new();
    private List<Quad>? _member;

    public IEnumerable<IEnumerable<Quad>> GetMembers() => _members;
    public IEnumerable<Quad> GetHypermedia() => _hypermedia;

    public IEnumerable<Quad> GetQuads() => _members.Aggregate(_hypermedia,
        (current, quads) =>
        {
            current.AddRange(quads);
            return current;
        });

    protected override bool HandleTripleInternal(Triple t)
    {
        return HandleQuadInternal(t, null);
    }

    private const string TreeMember = "https://w3id.org/tree#member";
    private const string TreeView = "https://w3id.org/tree#view";

    protected override bool HandleQuadInternal(Triple t, IRefNode? graph)
    {
        if (ReadingHypermedia)
        {
            _hypermedia.Add(new Quad(t, graph));
            if (t.Predicate.ToString().Equals(TreeMember))
            {
                _member = new List<Quad>();
                _members.Add(_member);
            }
        }
        else
        {
            if (t.Predicate.ToString().Equals(TreeMember))
            {
                _hypermedia.Add(new Quad(t, graph));
                _member = new List<Quad>();
                _members.Add(_member);
            }
            else if (t.Subject.ToString().Equals(baseIri) ||
                     t.Predicate.ToString().Equals(TreeView))
            {
                _member = null;
                _hypermedia.Add(new Quad(t, graph));
            }
            else
            {
                _member!.Add(new Quad(t, graph));
            }
        }

        return true;
    }

    public override bool AcceptsAll => true;
}