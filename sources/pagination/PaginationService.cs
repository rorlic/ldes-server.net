using System.Data;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AquilaSolutions.LdesServer.Pagination;

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
                var done = false;
                while (!done)
                {
                    done = !await bucketPaginator.TryPaginateBucketsAsync(connection, memberBatchSize, defaultPageSize);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogInformation($"{workerId}: Cancellation requested.");
                        return;
                    }
                }
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