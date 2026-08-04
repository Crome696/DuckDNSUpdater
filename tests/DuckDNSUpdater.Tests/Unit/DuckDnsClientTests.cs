using DuckDNSUpdater.Services;

namespace DuckDNSUpdater.Tests.Unit;

public class DuckDnsClientTests
{
    [Fact]
    public async Task GetPublicIpAsync_ReturnsValidIpv4()
    {
        var handler = new FakeHttpMessageHandler()
            .When("api.ipify.org", "203.0.113.10");
        using var client = CreateClient(handler);

        var ip = await client.GetPublicIpAsync();

        Assert.Equal("203.0.113.10", ip);
    }

    [Fact]
    public async Task GetPublicIpAsync_RejectsNonIpv4()
    {
        var handler = new FakeHttpMessageHandler()
            .When("api.ipify.org", "not-an-ip");
        using var client = CreateClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetPublicIpAsync());
    }

    [Fact]
    public async Task UpdateAsync_WithExplicitIp_BuildsExpectedUrlAndMapsOk()
    {
        var handler = new FakeHttpMessageHandler()
            .When("duckdns.org/update", "OK");
        using var client = CreateClient(handler);

        var result = await client.UpdateAsync("my-host", "secret-token", "198.51.100.7");

        Assert.True(result.Success);
        Assert.Equal("OK", result.Response);
        Assert.Equal("198.51.100.7", result.IpAddress);

        var uri = Assert.Single(handler.RequestUris);
        Assert.NotNull(uri);
        Assert.Contains("domains=my-host", uri.Query, StringComparison.Ordinal);
        Assert.Contains("token=secret-token", uri.Query, StringComparison.Ordinal);
        Assert.Contains("ip=198.51.100.7", uri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateAsync_MapsKoAsFailure()
    {
        var handler = new FakeHttpMessageHandler()
            .When("duckdns.org/update", "KO");
        using var client = CreateClient(handler);

        var result = await client.UpdateAsync("my-host", "secret-token", "198.51.100.7");

        Assert.False(result.Success);
        Assert.Equal("KO", result.Response);
    }

    [Fact]
    public async Task UpdateAsync_WithoutIp_ResolvesViaIpify()
    {
        var handler = new FakeHttpMessageHandler()
            .When("api.ipify.org", "203.0.113.44")
            .When("duckdns.org/update", "OK");
        using var client = CreateClient(handler);

        var result = await client.UpdateAsync("host", "tok");

        Assert.True(result.Success);
        Assert.Equal("203.0.113.44", result.IpAddress);
        Assert.Equal(2, handler.RequestUris.Count);
    }

    private static DuckDnsClient CreateClient(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });
}
