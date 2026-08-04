using DuckDNSUpdater.Models;

namespace DuckDNSUpdater.Tests.Unit;

public class AppConfigTests
{
    [Fact]
    public void CreateDefault_HasExpectedPlaceholders()
    {
        var config = AppConfig.CreateDefault();

        Assert.Equal("my-host", config.Domain);
        Assert.Equal("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx", config.Token);
        Assert.Equal(300, config.IntervalSeconds);
        Assert.False(config.AutoStart);
        Assert.False(config.WriteLogsToFile);
    }

    [Fact]
    public void Clone_IsIndependentCopy()
    {
        var original = new AppConfig
        {
            Domain = "alpha",
            Token = "token-1",
            IntervalSeconds = 60,
            AutoStart = true,
            WriteLogsToFile = true
        };

        var clone = original.Clone();
        clone.Domain = "beta";
        clone.Token = "token-2";
        clone.IntervalSeconds = 120;
        clone.AutoStart = false;
        clone.WriteLogsToFile = false;

        Assert.Equal("alpha", original.Domain);
        Assert.Equal("token-1", original.Token);
        Assert.Equal(60, original.IntervalSeconds);
        Assert.True(original.AutoStart);
        Assert.True(original.WriteLogsToFile);
        Assert.Equal("beta", clone.Domain);
    }
}
