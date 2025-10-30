using System.Data;
using AquilaSolutions.LdesServer.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AquilaSolutions.LdesServer.Ingestion;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder RegisterIngestionMetrics(this IApplicationBuilder app)
    {
        Prometheus.Metrics.DefaultRegistry.AddBeforeCollectCallback(async cancel =>
        {
            const string ingest = "ldes_server_ingested_members_count";
            const string helpIngest = "Number of ingested members per collection";

            using var connection = app.ApplicationServices.GetRequiredService<IDbConnection>();
            var repository = app.ApplicationServices.GetRequiredService<IStatisticsRepository>();
            (await repository.GetCollectionStatisticsAsync(connection, cancel)).ToList().ForEach(x =>
            {
                Prometheus.Metrics.CreateGauge(ingest, helpIngest, "collection")
                    .WithLabels(x.Collection).Set(x.Ingested);
            });
        });
        return app;
    }
}