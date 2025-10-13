using System.Data;
using System.Data.Common;
using AquilaSolutions.LdesServer.Pagination;

namespace AquilaSolutions.LdesServer;

public class PaginationService(
    BucketPaginatorConfiguration configuration,
    IServiceProvider serviceProvider,
    ILogger<PaginationService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var worker = serviceProvider.GetService<BucketPaginator>();
        return DoWorkAsync(worker!, cancellationToken);
    }
    
    
    private async Task DoWorkAsync(BucketPaginator bucketPaginator, CancellationToken cancellationToken)
    {
        var waitBetweenChecksTimeout = TimeSpan.FromMilliseconds(configuration.LoopDelay);
        var memberBatchSize = configuration.MemberBatchSize;
        var defaultPageSize = configuration.DefaultPageSize;
        var workerId = bucketPaginator.WorkerId;

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation($"{workerId}: Cancellation requested.");
                return;
            }

            try
            {
                using var connection = serviceProvider.GetRequiredService<IDbConnection>();
                bool processed;
                do
                {
                    processed = await bucketPaginator
                        .TryPaginateBucketsAsync(connection, memberBatchSize, defaultPageSize)
                        .ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogInformation($"{workerId}: Cancellation requested.");
                        return;
                    }
                } while (processed);
            }
            catch (DbException exception)
            {
                logger.LogWarning(exception, $"{workerId}: Database exception caught while paginating.");
            }
            finally
            {
                GC.Collect(0, GCCollectionMode.Optimized);
            }

            logger.LogDebug($"{workerId}: Sleeping for {waitBetweenChecksTimeout.TotalSeconds} seconds...");
            await Task.Delay(waitBetweenChecksTimeout, cancellationToken).ConfigureAwait(false);
        }
    }

}