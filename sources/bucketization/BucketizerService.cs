using AquilaSolutions.LdesServer.Fragmentation;
using Microsoft.Extensions.Logging;

namespace AquilaSolutions.LdesServer.Bucketization;

public class BucketizerService(
    MemberBucketizer memberBucketizer, 
    MemberBucketizerConfiguration configuration, 
    IServiceProvider serviceProvider,
    ILogger<MemberBucketizer> logger) 
    : FragmentationWorkerBase<MemberBucketizer, MemberBucketizerConfiguration>(memberBucketizer, configuration, serviceProvider, logger);