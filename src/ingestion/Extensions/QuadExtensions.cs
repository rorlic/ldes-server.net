using LdesServer.Ingestion.Algorithms.SplitMessage;
using VDS.RDF;

namespace LdesServer.Ingestion.Extensions;

public static class QuadExtensions
{
    internal static IEnumerable<Quad[]> SplitIntoEntitiesUsing(
        this IEnumerable<Quad> quads, ISplitMessageStrategy splitMessage)
    {
        var context = splitMessage.InitializeSplittingContext();
        var entities = new Dictionary<string, List<Quad>?>();

        foreach (var quad in quads)
        {
            var key = splitMessage.AssignQuadToEntity(quad, context);
            if (key is null) continue;

            if (!entities.TryGetValue(key, out var entity))
            {
                entity = [];
                entities.Add(key, entity);
            }

            entity!.Add(quad);
        }

        splitMessage.FinalizeSplitting(entities, context);
        return entities.Values.Where(x => x is not null).Select(x => x!.ToArray());
    }
}