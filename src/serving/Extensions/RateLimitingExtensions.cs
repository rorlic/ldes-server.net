// src/serving/RateLimitingExtensions.cs

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace LdesServer.Serving.Extensions;

public static class RateLimitingExtensions
{
    public const string PolicyName = "serving";

    public static IServiceCollection AddServingRateLimiting(
        this IServiceCollection services, ServingConfiguration config) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddConcurrencyLimiter(PolicyName, limiter =>
            {
                limiter.PermitLimit = config.MaxConcurrentRequests;
                limiter.QueueLimit = config.QueueLimit;
                limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = config.RetryAfterSeconds.ToString();
                return ValueTask.CompletedTask;
            };
        });
}