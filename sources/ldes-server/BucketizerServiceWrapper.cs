using AquilaSolutions.LdesServer.Bucketization;

namespace AquilaSolutions.LdesServer;

public class BucketizerServiceWrapper(BucketizerService service) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return service.DoWorkAsync(cancellationToken);
    }
}