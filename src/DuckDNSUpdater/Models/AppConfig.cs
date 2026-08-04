namespace DuckDNSUpdater.Models;

/// <summary>
/// Application settings persisted in <c>config.json</c>.
/// </summary>
public sealed class AppConfig
{
    /// <summary>
    /// DuckDNS subdomain without the <c>.duckdns.org</c> suffix.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// DuckDNS account token used to authorize updates.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Seconds between automatic DNS updates (30–86400).
    /// </summary>
    public int IntervalSeconds { get; set; } = 300;

    /// <summary>
    /// When <c>true</c>, the updater starts automatically when the app launches.
    /// </summary>
    public bool AutoStart { get; set; }

    /// <summary>
    /// When <c>true</c>, log lines are also appended to <c>duckdns-updater.log</c>.
    /// </summary>
    public bool WriteLogsToFile { get; set; }

    /// <summary>
    /// Creates a template configuration with placeholder values.
    /// </summary>
    public static AppConfig CreateDefault() => new()
    {
        Domain = "my-host",
        Token = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
        IntervalSeconds = 300,
        AutoStart = false,
        WriteLogsToFile = false
    };

    /// <summary>
    /// Returns a shallow copy of this configuration.
    /// </summary>
    public AppConfig Clone() => new()
    {
        Domain = Domain,
        Token = Token,
        IntervalSeconds = IntervalSeconds,
        AutoStart = AutoStart,
        WriteLogsToFile = WriteLogsToFile
    };
}
