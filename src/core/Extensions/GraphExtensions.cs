using LdesServer.Core.Namespaces;
using VDS.RDF;

namespace LdesServer.Core.Extensions;

public static class GraphExtensions
{
    public static IGraph WithBaseUri(this IGraph g, Uri baseUri)
    {
        g.BaseUri = baseUri;
        return g;
    }


    public static IGraph WithStandardPrefixes(this IGraph g)
    {
        g.NamespaceMap.AddNamespace(nameof(Prefix.ldes), new Uri(Prefix.ldes));
        g.NamespaceMap.AddNamespace(nameof(Prefix.tree), new Uri(Prefix.tree));
        g.NamespaceMap.AddNamespace(nameof(Prefix.dct), new Uri(Prefix.dct));
        g.NamespaceMap.AddNamespace(nameof(Prefix.prov), new Uri(Prefix.prov));
        return g;
    }

    public static IGraph WithServerPrefixes(this IGraph g)
    {
        g.NamespaceMap.AddNamespace(nameof(Prefix.lsdn), new Uri(Prefix.lsdn));
        g.NamespaceMap.AddNamespace(nameof(Prefix.ingest), new Uri(Prefix.ingest));
        return g;
    }

    public static Triple? FindOneByQNamePredicate(this IGraph g, string qName)
    {
        return g.GetTriplesWithPredicate(g.CreateUriNode(qName)).SingleOrDefault();
    }

    public static INode GetObjectBySubjectPredicate(this IGraph g, INode subject, INode predicate)
    {
        return g.GetTriplesWithSubjectPredicate(subject, predicate).Single().Object;
    }

    public static INode? FindObjectBySubjectPredicate(this IGraph g, INode subject, INode predicate)
    {
        return g.GetTriplesWithSubjectPredicate(subject, predicate).SingleOrDefault()?.Object;
    }

    public static INode GetSubjectByPredicateObject(this IGraph g, INode predicate, INode @object)
    {
        return g.GetTriplesWithPredicateObject(predicate, @object).Single().Subject;
    }

    public static INode GetSubjectByPredicateObject(this IGraph g, string qNamePredicate, string qNameObject)
    {
        return g.GetTriplesWithPredicateObject(g.CreateUriNode(qNamePredicate), g.CreateUriNode(qNameObject))
            .Single().Subject;
    }

    public static IGraph WithTriple(this IGraph g, IRefNode subject, IUriNode predicate, INode @object)
    {
        g.Assert(new Triple(subject, predicate, @object));
        return g;
    }

    public static IGraph WithTriples(this IGraph g, IEnumerable<Triple> triples)
    {
        g.Assert(triples);
        return g;
    }
    
    public static IEnumerable<IUriNode> GetSequencePath(this IGraph g, INode listRoot)
    {
        return g.GetListItems(listRoot).UriNodes();
    }
}