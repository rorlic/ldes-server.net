using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Namespaces;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms.CreateMember;

/// <summary>
/// Used for ingesting version entities (i.e. entities which are already versions of some state object). 
/// </summary>
/// <param name="predicate">An optional predicate (defaults to http://purl.org/dc/terms/isVersionOf)</param>
public class WithEntityMaterializationStrategy(IUriNode? predicate)
    : ICreateMemberStrategy
{
    private IUriNode Predicate { get; } = predicate ?? new UriNode(new Uri($"{Prefix.dct}isVersionOf"));

    /// <summary>
    /// Materializes a version entity, which is an entity containing a triple to represent a version to state relation
    /// using a configured predicate (typically dct:isVersionOf). The strategy searches for this relation triple
    /// (?entityId ?predicate ?stateObjectId) and uses the object URI (?stateObjectId) to replace all entityId subjects
    /// with this object URI, in addition to removing the relation triple.
    /// </summary>
    /// <returns>A member with the entity graph containing triples where the entityId subject is replaced by the object
    /// URI found, the triple containing the relation is removed and all other triples are returned as-is.</returns>
    /// <exception cref="ArgumentException">Thrown if entity does not contain a relation triple.</exception>
    public Member CreateMember(IEnumerable<Quad> quads, IUriNode memberId, IUriNode entityId, DateTimeOffset createdAt)
    {
        var quadList = quads.ToList();
        var versionId = entityId;
        var versionTriples = quadList.Where(VersionIdFilter).ToArray();
        var relations = versionTriples
            .Where(x => x.Predicate.Equals(Predicate) && x.Object.NodeType == NodeType.Uri).ToArray();

        if (relations.Length != 1)
            throw new ArgumentException(
                $"The entity does not contain a unique relation for predicate {Predicate} and subject {versionId} or the object is not a named node.");

        var relation = relations[0];
        var stateObjectId = (relation.Object as IUriNode)!;
        var stateTriples = versionTriples
            .Where(x => !x.Predicate.Equals(Predicate))
            .Select(x => new Quad(stateObjectId, x.Predicate, x.Object, x.Graph));

        quadList.RemoveAll(VersionIdFilter);
        quadList.AddRange(stateTriples);

        return Member.From(quadList.ToArray(), memberId, stateObjectId, createdAt);

        bool VersionIdFilter(Quad x) => versionId.Equals(x.Subject);
    }
}