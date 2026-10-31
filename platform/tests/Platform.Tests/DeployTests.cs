using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Platform.Api;
using Platform.Api.Auth;

namespace Platform.Tests;

/// <summary>What the servers need from the platform: /healthz with the build's version (deploy.conf HEALTH_PATH).</summary>
[Collection("platform")]
public class HealthTests(PlatformFactory api)
{
    [Fact]
    public async Task HealthzReportsTheVersionTheImageWasBuiltWith()
    {
        // The Dockerfile sets APP_VERSION from the build's VERSION.
        await using var built = api.WithWebHostBuilder(b => b.UseSetting(AppVersion.Setting, "4.5.6"));
        var response = await built.CreateClient().GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("4.5.6", (await response.Json()).Str("version"));
    }

    [Fact]
    public async Task WithoutABuildVersionHealthzReportsTheAssemblyVersion()
    {
        var health = await (await api.Client().GetAsync("/healthz")).Json();
        Assert.Equal(AppVersion.FromAssembly, health.Str("version"));
        Assert.Matches(@"^\d+\.\d+\.\d+", health.Str("version"));
        Assert.DoesNotContain("+", health.Str("version"));
    }

    [Fact]
    public async Task TheOldHealthCheckStillAnswers()
    {
        var response = await api.Client().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", (await response.Json()).Str("status"));
    }
}

/// <summary>Production traffic comes through Cloudflare and Traefik: the client is CF-Connecting-IP, not the connection.</summary>
public class ClientIpTests
{
    [Theory]
    [InlineData("203.0.113.7", "172.18.0.5", "203.0.113.7")]        // through Cloudflare: the header wins over Traefik's address
    [InlineData(" 2001:db8::7 ", "172.18.0.5", "2001:db8::7")]      // IPv6, with stray spaces
    [InlineData(null, "198.51.100.20", "198.51.100.20")]           // no header (a desk, tests): the connection
    [InlineData("", "198.51.100.20", "198.51.100.20")]
    [InlineData("not-an-ip", "198.51.100.20", "198.51.100.20")]    // a header that isn't an address is ignored
    [InlineData(null, "::ffff:198.51.100.20", "198.51.100.20")]    // IPv4 seen as IPv6 is the same client
    [InlineData(null, null, null)]
    public void PicksTheCloudflareHeaderThenTheConnection(string? header, string? remote, string? expected)
    {
        var http = new DefaultHttpContext();
        if (header is not null) http.Request.Headers[ClientIp.Header] = header;
        if (remote is not null) http.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        Assert.Equal(expected, ClientIp.Get(http));
    }
}
