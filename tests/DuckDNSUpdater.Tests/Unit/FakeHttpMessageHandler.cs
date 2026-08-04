using System.Net;
using System.Text;

namespace DuckDNSUpdater.Tests.Unit;

/// <summary>
/// Maps request URL prefixes to canned response bodies for isolated HTTP tests.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(Func<HttpRequestMessage, bool> Match, HttpStatusCode Status, string Body)> _rules = [];
    private readonly List<HttpRequestMessage> _requests = [];

    public IReadOnlyList<Uri?> RequestUris =>
        _requests.Select(r => r.RequestUri).ToList();

    public FakeHttpMessageHandler When(string urlContains, string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _rules.Add((
            req => req.RequestUri?.ToString().Contains(urlContains, StringComparison.OrdinalIgnoreCase) == true,
            status,
            body));
        return this;
    }

    public FakeHttpMessageHandler When(Func<HttpRequestMessage, bool> match, string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _rules.Add((match, status, body));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request);

        foreach (var (match, status, body) in _rules)
        {
            if (match(request))
            {
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "text/plain")
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"No fake rule for {request.RequestUri}", Encoding.UTF8, "text/plain")
        });
    }
}
