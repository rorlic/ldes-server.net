using System.Data;
using AquilaSolutions.LdesServer.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AquilaSolutions.LdesServer.Pagination;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder RegisterPaginationMetrics(this IApplicationBuilder app)
    {
        Prometheus.Metrics.DefaultRegistry.AddBeforeCollectCallback(async cancel =>
        {
            const string paginate = "ldes_server_pagination_members_count";
            const string helpPaginate = "Number of paginated members per view";

            using var connection = app.ApplicationServices.GetRequiredService<IDbConnection>();
            var repository = app.ApplicationServices.GetRequiredService<IStatisticsRepository>();
            (await repository.GetViewStatisticsAsync(connection, cancel)).ToList().ForEach(x =>
            {
                Prometheus.Metrics.CreateGauge(paginate, helpPaginate, "collection", "view")
                    .WithLabels(x.Collection, x.View).Set(x.Paginated);
            });
        });
        return app;
    }
}