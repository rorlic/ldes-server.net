using LdesServer.Core.Extensions;
using LdesServer.Core.Models;
using LdesServer.Core.Namespaces;
using LdesServer.Fragmentation.Models;
using Microsoft.Extensions.Logging;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Query;

namespace LdesServer.Fragmentation;

public class TimeFragmentation
{
    public IUriNode[] SequencePath { get; }
    private readonly IEnumerable<ITimeBucketPeriod> _periods;

    private TimeFragmentation(IUriNode[] sequencePath, IEnumerable<ITimeBucketPeriod> periods)
    {
        SequencePath = sequencePath;
        _periods = periods;
    }

    public static TimeFragmentation From(IGraph g, INode fragmentation)
    {
        var predicate = g.CreateUriNode(QNames.tree.path);
        var fragmentationPath = g.GetObjectBySubjectPredicate(fragmentation, predicate);
        var sequencePath = fragmentationPath.IsListRoot(g)
            ? g.GetSequencePath(fragmentationPath).ToArray()
            : [fragmentationPath.AsUriNode()];

        var durations = g.GetTriplesWithSubjectPredicate(fragmentation, g.CreateUriNode(QNames.lsdn.bucket)).ToArray();
        var periods = durations.Length == 0
            ? [new SimpleYearsPeriod(1), new SimpleMonthsPeriod(1), new SimpleDaysPeriod(1), new SimpleHoursPeriod(1)]
            : durations.Select(x => TimeBucketPeriodBase.From(x.Object.AsValueString())).ToArray();

        return new TimeFragmentation(sequencePath, periods);
    }

    private static readonly Uri DateTimeType = new("http://www.w3.org/2001/XMLSchema#dateTime");

    public IEnumerable<TimeBucketPath> TimeBucketPathsFor(Member member)
    {
        using var g = new Graph().WithStandardPrefixes().WithServerPrefixes();
        g.Assert(member.ToTriples());

        var subject = g.CreateUriNode(new Uri(member.EntityId));
        var objects = SequencePath.Aggregate(new INode[] { subject }, (subjects, p) =>
            subjects.SelectMany(s => g.GetTriplesWithSubjectPredicate(s, p).Select(x => x.Object)).ToArray());
        var timestamps = objects
            .Where(x => x is ILiteralNode literal && literal.DataType.Equals(DateTimeType))
            .Cast<ILiteralNode>()
            .Select(x => x.AsValuedNode().AsDateTimeOffset())
            .Distinct()
            .ToArray();

        return timestamps.Select(ts =>
            new TimeBucketPath(_periods
                .Select(p => p.CalculateBucket(ts))
                .OrderBy(b => b.From)
            ));
    }
}