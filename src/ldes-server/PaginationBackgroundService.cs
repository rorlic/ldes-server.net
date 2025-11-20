using LdesServer.Pagination;

namespace LdesServer;

public class PaginationBackgroundService(PaginationService service) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return service.DoWorkAsync(cancellationToken);
    }
}
