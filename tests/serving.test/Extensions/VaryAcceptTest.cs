// tests/serving.test/VaryAcceptTest.cs

using System.Net;
using FluentAssertions;
using LdesServer.Serving.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LdesServer.Serving.Test.Extensions;

public class VaryAcceptTest
{
    private const string Feed = "/feed";

    private static async Task<IHost> StartHostAsync()
    {
        return await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(s => s.AddRouting())
                .Configure(app =>
                {
                    // must run before routing so that 404s are covered as well
                    app.UseVaryAccept(Feed);
                    app.UseRouting();
                    app.UseEndpoints(e =>
                    {
                        e.MapMethods($"{Feed}/node", ["GET", "HEAD"], () => "ok");
                        e.MapPost($"{Feed}/node", () => "created");
                        e.MapGet($"{Feed}/error", () => Results.StatusCode(500));

                        // endpoint that sets its own Vary header lines, taken from the ?vary= query values
                        e.MapGet($"{Feed}/preset", ctx =>
                        {
                            foreach (var value in ctx.Request.Query["vary"])
                                ctx.Response.Headers.Append("Vary", value);
                            return ctx.Response.WriteAsync("ok");
                        });

                        e.MapGet("/feedback", () => "ok");
                        e.MapGet("/health", () => "ok");
                    });
                }))
            .StartAsync();
    }

    private static string[] Vary(HttpResponseMessage response) => [.. response.Headers.Vary];

    private static int CountAccept(HttpResponseMessage response) =>
        Vary(response).Count(v => v.Equals("Accept", StringComparison.OrdinalIgnoreCase));

    private static string Preset(params string[] varyLines) =>
        $"{Feed}/preset?" + string.Join("&", varyLines.Select(v => $"vary={Uri.EscapeDataString(v)}"));

    // ---- when the header is added -------------------------------------------------------

    [Fact]
    public async Task AddsVaryAcceptToGetResponse()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync($"{Feed}/node");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Vary(response).Should().Equal("Accept");
    }

    [Fact]
    public async Task AddsVaryAcceptToHeadResponse()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, $"{Feed}/node"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Vary(response).Should().Equal("Accept");
    }

    [Fact]
    public async Task AddsVaryAcceptToNotFoundResponse()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync($"{Feed}/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        Vary(response).Should().Equal("Accept");
    }

    [Fact]
    public async Task AddsVaryAcceptToErrorResponse()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync($"{Feed}/error");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        Vary(response).Should().Equal("Accept");
    }

    [Fact]
    public async Task DoesNotAddVaryAcceptToPostResponse()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().PostAsync($"{Feed}/node", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Vary(response).Should().BeEmpty();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/feedback")] // shares the "/feed" prefix but is a different path segment
    public async Task DoesNotAddVaryAcceptOutsideConfiguredPath(string path)
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Vary(response).Should().BeEmpty();
    }

    [Fact]
    public async Task MatchesConfiguredPathCaseInsensitively()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/FEED/node");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Vary(response).Should().Equal("Accept");
    }

    // ---- existing Vary values -----------------------------------------------------------

    [Fact]
    public async Task KeepsExistingVaryValuesAndAppendsAccept()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync(Preset("Accept-Encoding"));

        Vary(response).Should().BeEquivalentTo("Accept-Encoding", "Accept");
    }

    [Fact]
    public async Task DoesNotTreatAcceptPrefixedHeaderAsAccept()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync(Preset("Accept-Language"));

        Vary(response).Should().BeEquivalentTo("Accept-Language", "Accept");
    }

    [Theory]
    [InlineData("Accept")]
    [InlineData("accept")]
    [InlineData("ACCEPT")]
    public async Task DoesNotDuplicateAcceptAlreadyPresent(string existing)
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync(Preset(existing));

        CountAccept(response).Should().Be(1);
        Vary(response).Should().HaveCount(1);
    }

    [Fact]
    public async Task DoesNotDuplicateAcceptInsideCommaSeparatedValue()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync(Preset("Accept-Encoding, Accept"));

        CountAccept(response).Should().Be(1);
        Vary(response).Should().BeEquivalentTo("Accept-Encoding", "Accept");
    }

    [Fact]
    public async Task DoesNotDuplicateAcceptAcrossMultipleVaryHeaderLines()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync(Preset("Accept-Encoding", "Accept"));

        CountAccept(response).Should().Be(1);
        Vary(response).Should().BeEquivalentTo("Accept-Encoding", "Accept");
    }

    [Fact]
    public async Task DoesNotAppendAcceptToVaryStar()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync(Preset("*"));

        Vary(response).Should().Equal("*");
    }

    [Fact]
    public async Task DoesNotAppendAcceptWhenStarIsPartOfList()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync(Preset("Accept-Encoding, *"));

        CountAccept(response).Should().Be(0);
        Vary(response).Should().BeEquivalentTo("Accept-Encoding", "*");
    }
}