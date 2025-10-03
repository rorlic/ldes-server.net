using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Namespaces;
using VDS.RDF;
using VDS.RDF.Nodes;

namespace AquilaSolutions.LdesServer.Core.Models;

public class LdesNode : IDisposable
{
    public class Info(DateTime updatedAt, TimeSpan validityPeriod, bool open)
    {
        public DateTime UpdatedAt { get; } = updatedAt;
        public TimeSpan ValidityPeriod { get; } = validityPeriod;
        public bool Open { get; } = open;

        public string CacheControl
        {
            get
            {
                var maxAge = ValidityPeriod.TotalSeconds;
                var cacheControl = $"public, max-age={maxAge:0}";
                if (!Open) cacheControl += ", immutable";
                return cacheControl;
            }
        }

        private static readonly string[] Months =
            ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        
        public string LastModified
        {
            get
            {
                var dt = UpdatedAt;
                var dow = dt.DayOfWeek.ToString().Substring(0, 3);
                var lastModified = $"{dow}, {dt.Day:00} {Months[dt.Month-1]} {dt.Year:0000} {dt.Hour:00}:{dt.Minute:00}:{dt.Second:00} GMT";
                return lastModified;
            }
        }

        private static string CreateMd5(string input)
        {
            // Use input string to calculate MD5 hash
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes);
        }
        
        public string ETag => CreateMd5(UpdatedAt.ToString("O"));
    }

    private readonly IGraph _graph;
    private readonly IEnumerable<Member> _members;
    private readonly IUriNode _eventStreamUri;
    private readonly IUriNode _versionOfPredicate;
    private readonly IUriNode _timestampPredicate;
    private readonly IUriNode _treeMemberPredicate;

    // Create an LDES node
    public LdesNode(IGraph graph, IEnumerable<Member> members, Info metadata)
    {
        _graph = graph;
        _members = members;
        Metadata = metadata;

        var rdfType = graph.CreateUriNode(QNames.rdf.type);
        var ldesEventStream = graph.CreateUriNode(QNames.ldes.EventStream);
        _eventStreamUri = (graph.GetSubjectByPredicateObject(rdfType, ldesEventStream) as IUriNode)!;

        var versionOfPath = graph.CreateUriNode(QNames.ldes.versionOfPath);
        _versionOfPredicate = (graph.GetObjectBySubjectPredicate(_eventStreamUri, versionOfPath) as IUriNode)!;

        var timestampPath = graph.CreateUriNode(QNames.ldes.timestampPath);
        _timestampPredicate = (graph.GetObjectBySubjectPredicate(_eventStreamUri, timestampPath) as IUriNode)!;

        _treeMemberPredicate = graph.CreateUriNode(QNames.tree.member);

        // Set ldes:immutable flag on the tree:Node if needed
        if (!metadata.Open)
        {
            var treeNode = graph.CreateUriNode(QNames.tree.Node);
            if (graph.GetSubjectByPredicateObject(rdfType, treeNode) is IUriNode nodeUri)
            {
                var ldesImmutable = graph.CreateUriNode(QNames.ldes.immutable);
                graph.Assert(new Triple(nodeUri, ldesImmutable, new BooleanNode(true)));
            }
        }
    }

    public void Dispose() => _graph.Dispose();

    public Info Metadata { get; }

    private IEnumerable<Triple> FlattenEntity(Member member, UriNode memberUri, UriNode entityUri)
    {
        // NOTE: when a member is serialized as version triples, we need to exclude the
        //       version-of and timestamp predicates to prevent duplicate definitions for
        //       these predicates (as there can only be one!)
        return member
            .ToTriples()
            .Where(x => !(x.Subject.Equals(entityUri) &&
                          (x.Predicate.Equals(_versionOfPredicate) || x.Predicate.Equals(_timestampPredicate))))
            .Select(x => x.Subject.Equals(entityUri) ? new Triple(memberUri, x.Predicate, x.Object) : x);
    }
    
    public TripleStore Store
    {
        get
        {
            var store = new TripleStore();
            var g = new Graph().WithBaseUri(_graph.BaseUri).WithTriples(_graph.Triples);
            g.NamespaceMap.Import(_graph.NamespaceMap);
            store.Add(g);

            foreach (var member in _members)
            {
                var memberUri = new UriNode(new Uri(member.MemberId));
                var entityUri = new UriNode(new Uri(member.EntityId));
                var timestamp = new DateTimeNode(member.CreatedAt);
                var metadata = new Triple[]
                {
                    new(_eventStreamUri, _treeMemberPredicate, memberUri),
                    new(memberUri, _versionOfPredicate, entityUri),
                    new(memberUri, _timestampPredicate, timestamp)
                };

                g.WithTriples(metadata);
                store.Add(new Graph(memberUri).WithTriples(member.ToTriples()));
            }

            return store;
        }
    }

    public IEnumerable<Quad> Quads
    {
        get
        {
            // Add members using TREE profile specification (https://treecg.github.io/specification/profile)
            // This allows streaming parsing of the line formats (application/n-quads and application/n-triples).
            var quads = new List<Quad>();
            quads.AddRange(_graph.Triples.Select(x => new Quad(x, null)));

            foreach (var member in _members)
            {
                var memberUri = new UriNode(new Uri(member.MemberId));
                var entityUri = new UriNode(new Uri(member.EntityId));
                var timestamp = new DateTimeNode(member.CreatedAt);
                var metadata = new Quad[]
                {
                    new(_eventStreamUri, _treeMemberPredicate, memberUri, null),
                    new(memberUri, _versionOfPredicate, entityUri, null),
                    new(memberUri, _timestampPredicate, timestamp, null)
                };

                quads.AddRange(metadata);
                quads.AddRange(member.ToTriples().Select(x => new Quad(x, memberUri)));
            }

            return quads;
        }
    }


    public IGraph Graph
    {
        get
        {
            var g = new Graph().WithBaseUri(_graph.BaseUri).WithTriples(_graph.Triples);
            g.NamespaceMap.Import(_graph.NamespaceMap);

            foreach (var member in _members)
            {
                var memberUri = new UriNode(new Uri(member.MemberId));
                var entityUri = new UriNode(new Uri(member.EntityId));
                var timestamp = new DateTimeNode(member.CreatedAt);
                var metadata = new Triple[]
                {
                    new(_eventStreamUri, _treeMemberPredicate, memberUri),
                    new(memberUri, _versionOfPredicate, entityUri),
                    new(memberUri, _timestampPredicate, timestamp)
                };

                g.WithTriples(metadata);
                g.WithTriples(FlattenEntity(member, memberUri, entityUri));
            }

            return g;
        }
    }


    public IEnumerable<Triple> Triples
    {
        get
        {
            // Add members using TREE profile specification (https://treecg.github.io/specification/profile)
            // This allows streaming parsing of the line formats (application/n-quads and application/n-triples).
            var triples = new List<Triple>();
            triples.AddRange(_graph.Triples);

            foreach (var member in _members)
            {
                var memberUri = new UriNode(new Uri(member.MemberId));
                var entityUri = new UriNode(new Uri(member.EntityId));
                var timestamp = new DateTimeNode(member.CreatedAt);
                var metadata = new Triple[]
                {
                    new(_eventStreamUri, _treeMemberPredicate, memberUri),
                    new(memberUri, _versionOfPredicate, entityUri),
                    new(memberUri, _timestampPredicate, timestamp)
                };

                triples.AddRange(metadata);
                triples.AddRange(FlattenEntity(member, memberUri, entityUri));
            }

            return triples;
        }
    }
}