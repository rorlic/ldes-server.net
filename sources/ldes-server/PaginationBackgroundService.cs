using AquilaSolutions.LdesServer.Pagination;

namespace AquilaSolutions.LdesServer;

public class PaginationBackgroundService(PaginationService service) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return service.DoWorkAsync(cancellationToken);
    }
}
