using System.Data;
using AquilaSolutions.LdesServer.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AquilaSolutions.LdesServer.Bucketization;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder RegisterBucketizationMetrics(this IApplicationBuilder app)
    {
        Prometheus.Metrics.DefaultRegistry.AddBeforeCollectCallback(async cancel =>
        {
            const string bucketize = "ldes_server_bucket_members_count";
            const string helpBucketized = "Number of bucketized members per view";

            using var connection = app.ApplicationServices.GetRequiredService<IDbConnection>();
            var repository = app.ApplicationServices.GetRequiredService<IStatisticsRepository>();
            (await repository.GetViewStatisticsAsync(connection, cancel)).ToList().ForEach(x =>
            {
                Prometheus.Metrics.CreateGauge(bucketize, helpBucketized, "collection", "view")
                    .WithLabels(x.Collection, x.View).Set(x.Bucketized);
            });
        });
        return app;
    }
}