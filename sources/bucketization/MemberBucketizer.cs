using System.Data;
using System.Data.Common;
using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Namespaces;
using AquilaSolutions.LdesServer.Fragmentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Bucketization;

public class MemberBucketizer(
    IViewRepository viewRepository,
    DefaultBucketizer defaultBucketizer,
    TimeBucketizer timeBucketizer,
    ILogger<MemberBucketizer> logger)
{
    internal string WorkerId { get; } = Guid.NewGuid().ToString();

    internal async Task<bool> TryBucketizeViewAsync(IDbConnection connection, short memberBatchSize)
    {
        using var transaction = connection.BeginTransaction();

        var view = await viewRepository
            .GetViewReadyForBucketizationAsync(transaction)
            .ConfigureAwait(false);

        if (view is null)
        {
            logger.LogDebug($"{WorkerId}: No views to bucketize.");
            return false;
        }

        var viewName = view.Name;
        logger.LogInformation($"{WorkerId}: Bucketizing view {view.Name} ...");

        using var g = view.ParseDefinition();
        var fragmentationListRoot =
            g.FindOneByQNamePredicate(QNames.tree.fragmentationStrategy)?.Object;
        var fragmentationDefinitions =
            fragmentationListRoot is null ? [] : g.GetListItems(fragmentationListRoot).ToArray();

        int? processed;
        switch (fragmentationDefinitions.Length)
        {
            case 0:
                logger.LogInformation(
                    $"{WorkerId}: Default bucketizing view {viewName} (max {memberBatchSize} members)...");
                processed = await defaultBucketizer
                    .BucketizeViewAsync(transaction, view, memberBatchSize)
                    .ConfigureAwait(false);
                logger.LogInformation($"{WorkerId}: Done default bucketizing view {viewName}.");
                break;
            case 1:
            {
                var fragmentation = fragmentationDefinitions[0];
                var typeNode =
                    g.GetObjectBySubjectPredicate(fragmentation, g.CreateUriNode(QNames.rdf.type));
                var type = typeNode.ToString().Replace(Prefix.lsdn, $"{nameof(Prefix.lsdn)}:");
                switch (type)
                {
                    case QNames.lsdn.TimeFragmentation:
                    {
                        var timeFragmentation = TimeFragmentation.From(g, fragmentation);
                        logger.LogInformation(
                            $"{WorkerId}: Time bucketizing view {viewName} (max {memberBatchSize} members)...");
                        processed = await timeBucketizer
                            .BucketizeViewAsync(transaction, view, timeFragmentation, memberBatchSize)
                            .ConfigureAwait(false);
                        logger.LogInformation($"{WorkerId}: Done time bucketizing view {viewName}.");
                        break;
                    }
                    default:
                        throw new NotImplementedException($"Unknown fragmentation type: '{type}'");
                }

                break;
            }
            default:
                // TODO: handle multiple fragmentations?
                throw new NotImplementedException("Multiple fragmentations are currently not supported");
        }

        if (processed is null)
        {
            logger.LogWarning($"{WorkerId}: View {viewName} could not be bucketized.");
            return false;
        }
        
        transaction.Commit();
        logger.LogInformation($"{WorkerId}: Done bucketizing view {view.Name} for now.");
        return true;
    }
}