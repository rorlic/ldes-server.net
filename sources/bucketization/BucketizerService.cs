using System.Data;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AquilaSolutions.LdesServer.Bucketization;

public class BucketizerService(
    MemberBucketizerConfiguration configuration,
    IServiceProvider serviceProvider,
    ILogger<BucketizerService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var worker = serviceProvider.GetService<MemberBucketizer>();
        return DoWorkAsync(worker!, cancellationToken);
    }

    private async Task DoWorkAsync(MemberBucketizer memberBucketizer, CancellationToken cancellationToken)
    {
        var waitBetweenChecksTimeout = TimeSpan.FromMilliseconds(configuration.LoopDelay);
        var memberBatchSize = configuration.MemberBatchSize;
        var workerId = memberBucketizer.WorkerId;

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
                    done = !await memberBucketizer
                        .TryBucketizeViewAsync(connection, memberBatchSize)
                        .ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogInformation($"{workerId}: Cancellation requested.");
                        return;
                    }
                }
            }
            catch (DbException exception)
            {
                logger.LogWarning(exception, $"{workerId}: Database exception caught while bucketizing.");
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