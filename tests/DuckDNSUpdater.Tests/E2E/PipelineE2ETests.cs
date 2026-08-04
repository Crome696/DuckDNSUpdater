using System.Collections.Concurrent;
using DuckDNSUpdater.Models;
using DuckDNSUpdater.Services;
using DuckDNSUpdater.Tests.Unit;

namespace DuckDNSUpdater.Tests.E2E;

public class PipelineE2ETests
{
    [Fact]
    public async Task SaveConfig_Start_SuccessfulUpdate_ThenStop()
    {
        using var temp = new TempConfigDir();
        var configService = new ConfigService(temp.ConfigPath);
        var handler = new FakeHttpMessageHandler()
            .When("api.ipify.org", "203.0.113.50")
            .When("duckdns.org/update", "OK");
        using var updater = CreateUpdater(handler);

        var config = new AppConfig
        {
            Domain = "e2e-host",
            Token = "e2e-token",
            IntervalSeconds = 30,
            AutoStart = false,
            WriteLogsToFile = false
        };
        configService.Save(config);
        var loaded = configService.Load();
        Assert.Equal("e2e-host", loaded.Domain);

        var updated = new TaskCompletionSource<DuckDnsUpdateResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var logs = new ConcurrentBag<string>();
        updater.Updated += (_, result) => updated.TrySetResult(result);
        updater.Log += (_, args) => logs.Add(args.Message);

        updater.Start(loaded);

        var result = await updated.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.Success);
        Assert.Equal("203.0.113.50", result.IpAddress);
        Assert.Contains(logs, m => m.Contains("OK", StringComparison.Ordinal));
        Assert.True(updater.IsRunning);

        await updater.StopAsync();
        Assert.False(updater.IsRunning);
    }

    [Fact]
    public async Task Start_DuckDnsReturnsKo_RaisesFailedUpdate()
    {
        using var temp = new TempConfigDir();
        var configService = new ConfigService(temp.ConfigPath);
        var handler = new FakeHttpMessageHandler()
            .When("api.ipify.org", "203.0.113.51")
            .When("duckdns.org/update", "KO");
        using var updater = CreateUpdater(handler);

        var config = new AppConfig
        {
            Domain = "e2e-host",
            Token = "e2e-token",
            IntervalSeconds = 30
        };
        configService.Save(config);

        var updated = new TaskCompletionSource<DuckDnsUpdateResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var logs = new ConcurrentBag<string>();
        updater.Updated += (_, result) => updated.TrySetResult(result);
        updater.Log += (_, args) => logs.Add(args.Message);

        updater.Start(configService.Load());

        var result = await updated.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(result.Success);
        Assert.Equal("KO", result.Response);
        Assert.Contains(logs, m => m.Contains("Failed", StringComparison.Ordinal));

        await updater.StopAsync();
        Assert.False(updater.IsRunning);
    }

    private static DuckDnsUpdater CreateUpdater(FakeHttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        return new DuckDnsUpdater(new DuckDnsClient(http));
    }

    private sealed class TempConfigDir : IDisposable
    {
        public string DirectoryPath { get; } =
            Path.Combine(Path.GetTempPath(), "DuckDNSUpdaterE2E_" + Guid.NewGuid().ToString("N"));

        public string ConfigPath => Path.Combine(DirectoryPath, "config.json");

        public TempConfigDir() => Directory.CreateDirectory(DirectoryPath);

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
