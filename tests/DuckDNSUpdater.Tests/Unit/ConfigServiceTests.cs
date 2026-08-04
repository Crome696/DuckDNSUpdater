using DuckDNSUpdater.Models;
using DuckDNSUpdater.Services;

namespace DuckDNSUpdater.Tests.Unit;

public class ConfigServiceTests
{
    [Fact]
    public void Validate_EmptyDomain_Throws()
    {
        var config = ValidConfig();
        config.Domain = "  ";

        var ex = Assert.Throws<InvalidOperationException>(() => ConfigService.Validate(config));
        Assert.Contains("Domain", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_EmptyToken_Throws()
    {
        var config = ValidConfig();
        config.Token = "";

        var ex = Assert.Throws<InvalidOperationException>(() => ConfigService.Validate(config));
        Assert.Contains("Token", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_IntervalTooLow_Throws()
    {
        var config = ValidConfig();
        config.IntervalSeconds = 29;

        var ex = Assert.Throws<InvalidOperationException>(() => ConfigService.Validate(config));
        Assert.Contains("30", ex.Message);
    }

    [Fact]
    public void Validate_IntervalTooHigh_Throws()
    {
        var config = ValidConfig();
        config.IntervalSeconds = 86_401;

        var ex = Assert.Throws<InvalidOperationException>(() => ConfigService.Validate(config));
        Assert.Contains("86400", ex.Message);
    }

    [Fact]
    public void Validate_ValidConfig_DoesNotThrow()
    {
        ConfigService.Validate(ValidConfig());
    }

    [Fact]
    public void Load_MissingFile_CreatesDefaults()
    {
        using var temp = new TempConfigDir();
        var service = new ConfigService(temp.ConfigPath);

        var config = service.Load();

        Assert.True(File.Exists(temp.ConfigPath));
        Assert.Equal("my-host", config.Domain);
        Assert.Equal(300, config.IntervalSeconds);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        using var temp = new TempConfigDir();
        var service = new ConfigService(temp.ConfigPath);
        var expected = new AppConfig
        {
            Domain = "home-lab",
            Token = "abc-123",
            IntervalSeconds = 120,
            AutoStart = true,
            WriteLogsToFile = true
        };

        service.Save(expected);
        var loaded = service.Load();

        Assert.Equal(expected.Domain, loaded.Domain);
        Assert.Equal(expected.Token, loaded.Token);
        Assert.Equal(expected.IntervalSeconds, loaded.IntervalSeconds);
        Assert.Equal(expected.AutoStart, loaded.AutoStart);
        Assert.Equal(expected.WriteLogsToFile, loaded.WriteLogsToFile);
    }

    [Fact]
    public void Save_StripsDuckDnsSuffix()
    {
        using var temp = new TempConfigDir();
        var service = new ConfigService(temp.ConfigPath);
        var config = ValidConfig();
        config.Domain = "my-host.duckdns.org";

        service.Save(config);
        var loaded = service.Load();

        Assert.Equal("my-host", loaded.Domain);
    }

    [Fact]
    public void Load_ClampsIntervalBelowMinimum()
    {
        using var temp = new TempConfigDir();
        File.WriteAllText(temp.ConfigPath, """
            {
              "domain": "host",
              "token": "tok",
              "intervalSeconds": 5,
              "autoStart": false,
              "writeLogsToFile": false
            }
            """);

        var loaded = new ConfigService(temp.ConfigPath).Load();

        Assert.Equal(30, loaded.IntervalSeconds);
    }

    private static AppConfig ValidConfig() => new()
    {
        Domain = "host",
        Token = "token",
        IntervalSeconds = 60,
        AutoStart = false,
        WriteLogsToFile = false
    };

    private sealed class TempConfigDir : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "DuckDNSUpdaterTests_" + Guid.NewGuid().ToString("N"));
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
