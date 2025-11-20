using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.SplitMessage;

public abstract class BySubjectStrategyBase : ISplitMessageStrategy
{
    protected abstract bool IsEntity(Quad quad);

    private class State
    {
        public IDictionary<IRefNode, List<Quad>> QuadsBySubject { get; } = new Dictionary<IRefNode, List<Quad>>();
        public IDictionary<IRefNode, List<Quad>> ObjectReferences { get; } = new Dictionary<IRefNode, List<Quad>>();
    }

    public object InitializeSplittingContext()
    {
        return new State();
    }

    private static List<Quad> GetOrCreateQuads(IDictionary<IRefNode, List<Quad>> mapping, IRefNode reference)
    {
        if (!mapping.TryGetValue(reference, out var quads))
        {
            mapping.Add(reference, quads = new List<Quad>());
        }

        return quads;
    }

    public string? AssignQuadToEntity(Quad quad, object? context)
    {
        var state = context as State;
        ArgumentNullException.ThrowIfNull(state);

        var subject = quad.Subject;
        var key = subject.NodeType == NodeType.Uri && IsEntity(quad) ? subject.ToString() : null;

        if (key is null && subject is IRefNode subjectRef)
        {
            // for non-entities, group quads by subject
            GetOrCreateQuads(state.QuadsBySubject, subjectRef).Add(quad);

            // and keep a list of references to the subject
            GetOrCreateQuads(state.ObjectReferences, subjectRef);
        }

        if (quad.Object is IRefNode objectRef)
        {
            // add object reference
            GetOrCreateQuads(state.ObjectReferences, objectRef).Add(quad);
        }

        return key;
    }

    private static IRefNode? FindEntityRef(IRefNode? subject, IDictionary<IRefNode, List<Quad>> objectReferences)
    {
        ArgumentNullException.ThrowIfNull(subject);

        if (!objectReferences.TryGetValue(subject, out var nodeReferences))
            throw new ArgumentException($"Found an orphaned node {subject}.");

        var references = nodeReferences
            .Select(x => x.Subject as IRefNode)
            .Select(x => x is UriNode ? x : FindEntityRef(x, objectReferences))
            .Distinct()
            .ToArray();

        return references.Length == 1
            ? references.First()
            : throw new ArgumentException(
                $"Uri node '{subject}' is referenced by multiple nodes: {string.Join(", ", references.Select(x => x!.ToString()))}");
    }

    public void FinalizeSplitting(Dictionary<string, List<Quad>?> entityMap, object? context)
    {
        var state = context as State;
        ArgumentNullException.ThrowIfNull(state);

        var entityKeys = entityMap.Keys.ToArray();
        foreach (var subject in state.QuadsBySubject.Keys)
        {
            if (entityKeys.Contains(subject.ToString())) continue; // skip entity keys
            
            var entityRef = FindEntityRef(subject, state.ObjectReferences);
            if (entityRef is null)
                throw new InvalidOperationException($"Unable to find entity reference for subject '{subject}'.");
            if (!entityMap.TryGetValue(entityRef.ToString(), out var entity))
                throw new InvalidOperationException($"Unable to find entity for entity reference '{entityRef}'.");
            if (entity is null)
                throw new InvalidOperationException($"Entity for entity reference '{entityRef}' should not be null.");
            state.QuadsBySubject[subject].ForEach(entity.Add);
        }
        
        state.QuadsBySubject.Clear();
        state.ObjectReferences.Clear();
    }
}