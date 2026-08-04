using System.Text.Json;
using System.Text.Json.Serialization;
using DuckDNSUpdater.Models;

namespace DuckDNSUpdater.Services;

/// <summary>
/// Loads and saves <see cref="AppConfig"/> from a JSON file next to the application.
/// </summary>
public sealed class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    /// <summary>
    /// Full path to the configuration file.
    /// </summary>
    public string ConfigPath { get; }

    /// <summary>
    /// Creates a service that reads and writes <paramref name="configPath"/>,
    /// or <c>config.json</c> in the application base directory when omitted.
    /// </summary>
    public ConfigService(string? configPath = null)
    {
        ConfigPath = configPath
            ?? Path.Combine(AppContext.BaseDirectory, "config.json");
    }

    /// <summary>
    /// Loads configuration from disk, creating a default file when missing.
    /// </summary>
    public AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
        {
            var defaults = AppConfig.CreateDefault();
            Save(defaults);
            return defaults;
        }

        var json = File.ReadAllText(ConfigPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)
            ?? AppConfig.CreateDefault();

        Normalize(config);
        return config;
    }

    /// <summary>
    /// Validates, normalizes, and writes <paramref name="config"/> to disk.
    /// </summary>
    public void Save(AppConfig config)
    {
        Normalize(config);
        Validate(config);

        var directory = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    /// <summary>
    /// Ensures domain, token, and interval are within accepted bounds.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a value is invalid.</exception>
    public static void Validate(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Domain))
        {
            throw new InvalidOperationException("Domain must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(config.Token))
        {
            throw new InvalidOperationException("Token must not be empty.");
        }

        if (config.IntervalSeconds < 30)
        {
            throw new InvalidOperationException("Interval must be at least 30 seconds.");
        }

        if (config.IntervalSeconds > 86_400)
        {
            throw new InvalidOperationException("Interval must be at most 86400 seconds.");
        }
    }

    private static void Normalize(AppConfig config)
    {
        config.Domain = config.Domain.Trim();
        if (config.Domain.EndsWith(".duckdns.org", StringComparison.OrdinalIgnoreCase))
        {
            config.Domain = config.Domain[..^".duckdns.org".Length].TrimEnd('.');
        }

        config.Token = config.Token.Trim();

        if (config.IntervalSeconds < 30)
        {
            config.IntervalSeconds = 30;
        }
        else if (config.IntervalSeconds > 86_400)
        {
            config.IntervalSeconds = 86_400;
        }
    }
}
