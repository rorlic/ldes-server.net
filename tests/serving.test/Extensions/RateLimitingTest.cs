// tests/serving.test/RateLimitingTest.cs

using System.Net;
using System.Reflection;
using FluentAssertions;
using LdesServer.Serving.Controllers;
using LdesServer.Serving.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LdesServer.Serving.Test.Extensions;

public class RateLimitingTest
{
    private const string Limited = "/limited";

    private sealed class Gate
    {
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static async Task<IHost> StartHostAsync(ServingConfiguration config, Func<Task> onLimitedRequest)
    {
        return await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(s =>
                {
                    s.AddRouting();
                    s.AddServingRateLimiting(config);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseRateLimiter();
                    app.UseEndpoints(e =>
                    {
                        e.MapGet(Limited, async ctx =>
                        {
                            await onLimitedRequest();
                            await ctx.Response.WriteAsync("ok");
                        }).RequireRateLimiting(RateLimitingExtensions.PolicyName);

                        e.MapGet("/health", () => "ok");
                    });
                }))
            .StartAsync();
    }

    [Fact]
    public async Task Returns429WhenLimitExceeded()
    {
        const int retryAfterSeconds = 7;
        var config = new ServingConfiguration { MaxConcurrentRequests = 1, QueueLimit = 0, RetryAfterSeconds = retryAfterSeconds };
        var gate = new Gate();
        using var host = await StartHostAsync(config, async () =>
        {
            gate.Entered.TrySetResult();
            await gate.Release.Task;
        });
        var client = host.GetTestClient();

        var busy = client.GetAsync(Limited);
        await gate.Entered.Task; // wait until the handler is really running

        var rejected = client.GetAsync(Limited);
        var result = await rejected;
        result.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        result.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromSeconds(retryAfterSeconds));

        gate.Release.SetResult();
        (await busy).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AcceptsRequestsAgainWhenLimitReset()
    {
        var config = new ServingConfiguration { MaxConcurrentRequests = 1 };
        var gate = new Gate();
        var calls = 0;
        using var host = await StartHostAsync(config, async () =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                gate.Entered.TrySetResult();
                await gate.Release.Task;
            }
        });
        var client = host.GetTestClient();

        var busy = client.GetAsync(Limited);
        await gate.Entered.Task;
        
        var rejected = client.GetAsync(Limited);
        (await rejected).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        gate.Release.SetResult();
        await busy;

        var allowed = client.GetAsync(Limited);
        (await allowed).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NotRateLimitedWithoutPolicy()
    {
        var config = new ServingConfiguration { MaxConcurrentRequests = 1 };
        var gate = new Gate();
        using var host = await StartHostAsync(config, async () =>
        {
            gate.Entered.TrySetResult();
            await gate.Release.Task;
        });
        var client = host.GetTestClient();

        var busy = client.GetAsync(Limited);
        await gate.Entered.Task;

        var allowed = client.GetAsync("/health");
        (await allowed).StatusCode.Should().Be(HttpStatusCode.OK);

        gate.Release.SetResult();
        await busy;
    }

    [Fact]
    public async Task AcceptsQueuedRequestsUpToQueueLimit()
    {
        var timeout = TimeSpan.FromSeconds(10);
        var config = new ServingConfiguration { MaxConcurrentRequests = 1, QueueLimit = 1 };
        var gate = new Gate();
        var calls = 0;
        using var host = await StartHostAsync(config, async () =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                gate.Entered.TrySetResult();
                await gate.Release.Task;
            }
        });
        var client = host.GetTestClient();

        try
        {
            var busy = client.GetAsync("/limited");
            await gate.Entered.Task.WaitAsync(timeout);

            var request1 = client.GetAsync("/limited");
            var request2 = client.GetAsync("/limited");

            var rejected = await Task.WhenAny(request1, request2).WaitAsync(timeout);
            var queued = rejected == request1 ? request2 : request1;

            (await rejected).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            queued.IsCompleted.Should().BeFalse("it should be waiting in the queue");

            gate.Release.SetResult();

            (await busy.WaitAsync(timeout)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await queued.WaitAsync(timeout)).StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            gate.Release.TrySetResult();   // never leave a request parked if an assertion fails
        }
    }
    
    [Fact]
    public void NodeControllerIsRateLimitedByServingPolicy()
    {
        var attribute = typeof(NodeController).GetCustomAttribute<EnableRateLimitingAttribute>();

        attribute.Should().NotBeNull();
        attribute.PolicyName.Should().Be(RateLimitingExtensions.PolicyName);
    }
}