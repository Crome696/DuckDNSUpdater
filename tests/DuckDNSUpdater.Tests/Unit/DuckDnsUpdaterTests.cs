using System.Collections.Concurrent;
using DuckDNSUpdater.Models;
using DuckDNSUpdater.Services;

namespace DuckDNSUpdater.Tests.Unit;

public class DuckDnsUpdaterTests
{
    [Fact]
    public void Start_InvalidConfig_Throws()
    {
        using var updater = CreateUpdater(new FakeHttpMessageHandler());
        var config = new AppConfig { Domain = "", Token = "t", IntervalSeconds = 30 };

        Assert.Throws<InvalidOperationException>(() => updater.Start(config));
        Assert.False(updater.IsRunning);
    }

    [Fact]
    public async Task Start_RaisesUpdatedAndLog_ThenStopClearsRunning()
    {
        var handler = new FakeHttpMessageHandler()
            .When("api.ipify.org", "203.0.113.1")
            .When("duckdns.org/update", "OK");
        using var updater = CreateUpdater(handler);

        var updated = new TaskCompletionSource<DuckDnsUpdateResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var logs = new ConcurrentBag<string>();
        updater.Updated += (_, result) => updated.TrySetResult(result);
        updater.Log += (_, args) => logs.Add(args.Message);

        updater.Start(ValidConfig());

        var result = await updated.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.Success);
        Assert.Equal("203.0.113.1", result.IpAddress);
        Assert.Contains(logs, m => m.Contains("Updater started", StringComparison.Ordinal));
        Assert.Contains(logs, m => m.Contains("OK", StringComparison.Ordinal));

        await updater.StopAsync();

        Assert.False(updater.IsRunning);
        Assert.Contains(logs, m => m.Contains("Updater stopped", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Start_Twice_IsNoOp()
    {
        var handler = new FakeHttpMessageHandler()
            .When("api.ipify.org", "203.0.113.2")
            .When("duckdns.org/update", "OK");
        using var updater = CreateUpdater(handler);

        var updates = 0;
        var firstUpdate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        updater.Updated += (_, _) =>
        {
            if (Interlocked.Increment(ref updates) == 1)
            {
                firstUpdate.TrySetResult();
            }
        };

        updater.Start(ValidConfig());
        await firstUpdate.Task.WaitAsync(TimeSpan.FromSeconds(10));
        updater.Start(ValidConfig());

        await Task.Delay(200);
        Assert.Equal(1, updates);

        await updater.StopAsync();
    }

    private static DuckDnsUpdater CreateUpdater(FakeHttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        return new DuckDnsUpdater(new DuckDnsClient(http));
    }

    private static AppConfig ValidConfig() => new()
    {
        Domain = "host",
        Token = "token",
        IntervalSeconds = 30,
        AutoStart = false,
        WriteLogsToFile = false
    };
}
