using System.Net;
using System.Net.Http.Headers;

namespace DuckDNSUpdater.Services;

/// <summary>
/// HTTP client for resolving the public IPv4 address and updating DuckDNS.
/// </summary>
public sealed class DuckDnsClient : IDisposable
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates a client using an optional shared <see cref="HttpClient"/>.
    /// </summary>
    public DuckDnsClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        if (httpClient is null)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("DuckDNSUpdater", "1.0.0"));
        }
    }

    /// <summary>
    /// Returns the current public IPv4 address via ipify.
    /// </summary>
    public async Task<string> GetPublicIpAsync(CancellationToken cancellationToken = default)
    {
        var ip = (await _httpClient.GetStringAsync("https://api.ipify.org", cancellationToken)).Trim();

        if (!IPAddress.TryParse(ip, out var address) || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new InvalidOperationException($"Invalid public IPv4 address: {ip}");
        }

        return address.ToString();
    }

    /// <summary>
    /// Updates the given DuckDNS domain. When <paramref name="ip"/> is null or empty,
    /// the public IPv4 address is resolved automatically.
    /// </summary>
    public async Task<DuckDnsUpdateResult> UpdateAsync(
        string domain,
        string token,
        string? ip = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedIp = string.IsNullOrWhiteSpace(ip)
            ? await GetPublicIpAsync(cancellationToken)
            : ip.Trim();

        var url =
            $"https://www.duckdns.org/update?domains={Uri.EscapeDataString(domain)}" +
            $"&token={Uri.EscapeDataString(token)}" +
            $"&ip={Uri.EscapeDataString(resolvedIp)}";

        var responseText = (await _httpClient.GetStringAsync(url, cancellationToken)).Trim();
        var success = string.Equals(responseText, "OK", StringComparison.OrdinalIgnoreCase);

        return new DuckDnsUpdateResult(success, responseText, resolvedIp, DateTime.Now);
    }

    /// <inheritdoc />
    public void Dispose() => _httpClient.Dispose();
}

/// <summary>
/// Outcome of a single DuckDNS update request.
/// </summary>
/// <param name="Success">Whether DuckDNS responded with OK.</param>
/// <param name="Response">Raw response body from DuckDNS.</param>
/// <param name="IpAddress">IPv4 address that was submitted.</param>
/// <param name="Timestamp">Local time when the update completed.</param>
public readonly record struct DuckDnsUpdateResult(
    bool Success,
    string Response,
    string IpAddress,
    DateTime Timestamp);
