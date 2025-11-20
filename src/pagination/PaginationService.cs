using LdesServer.Fragmentation;
using Microsoft.Extensions.Logging;

namespace LdesServer.Pagination;

public class PaginationService(
    BucketPaginator memberBucketizer, 
    BucketPaginatorConfiguration configuration, 
    IServiceProvider serviceProvider,
    ILogger<BucketPaginator> logger) 
    : FragmentationWorkerBase<BucketPaginator, BucketPaginatorConfiguration>(memberBucketizer, configuration, serviceProvider, logger);