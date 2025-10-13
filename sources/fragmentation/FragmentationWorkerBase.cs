using System.Data;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AquilaSolutions.LdesServer.Fragmentation;

public abstract class FragmentationWorkerBase<TWorker, TConfiguration>(
    TWorker worker,
    TConfiguration configuration,
    IServiceProvider serviceProvider,
    ILogger<TWorker> logger) 
    where TWorker : IFragmentationWorker<TConfiguration>
    where TConfiguration : IFragmentationWorkerConfiguration
{
    /// <summary>
    /// Do some fragmentation work, one-off or until cancelled
    /// </summary>
    /// <param name="cancellationToken">The token indicating cancellation</param>
    public async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        var workerId = worker.WorkerId;

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
                    processed = await worker.ProcessAsync(connection, configuration)
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
                logger.LogWarning(exception, $"{workerId}: Database exception caught while bucketizing.");
            }
            finally
            {
                GC.Collect(0, GCCollectionMode.Optimized);
            }

            if (configuration.LoopDelay == null) return;
            
            var delay = configuration.LoopDelay.Value;
            if (delay < 0) throw new ArgumentException("Loop delay must be a positive value.");
            
            var waitBetweenChecksTimeout = TimeSpan.FromMilliseconds(delay);
            logger.LogDebug($"{workerId}: Sleeping for {waitBetweenChecksTimeout.TotalSeconds} seconds...");
            await Task.Delay(waitBetweenChecksTimeout!, cancellationToken).ConfigureAwait(false);
        }
    }
}