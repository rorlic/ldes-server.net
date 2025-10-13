using AquilaSolutions.LdesServer.Bucketization;

namespace AquilaSolutions.LdesServer;

public class BucketizerBackgroundService(BucketizerService service) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return service.DoWorkAsync(cancellationToken);
    }
}