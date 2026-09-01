using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace NTNP.Pricing.Desktop.Tests.TestSupport;

/// <summary>
/// Stands in for the real server in ViewModel tests. Route a request to a canned response by
/// registering a handler for "METHOD path" (path is matched by prefix, query string ignored) via
/// <see cref="When"/>; every match is recorded in <see cref="Requests"/> so tests can assert what a
/// view model actually sent (e.g. the exact request body of a Save).
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly List<(string Key, Func<HttpRequestMessage, HttpResponseMessage> Respond)> _routes = new();

    public List<HttpRequestMessage> Requests { get; } = new();

    public FakeHttpMessageHandler When(HttpMethod method, string pathPrefix, Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _routes.Add(($"{method.Method} {pathPrefix}", respond));
        return this;
    }

    public FakeHttpMessageHandler WhenJson<T>(HttpMethod method, string pathPrefix, T body, HttpStatusCode status = HttpStatusCode.OK) =>
        When(method, pathPrefix, _ => new HttpResponseMessage(status) { Content = JsonContent.Create(body, options: Json) });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var path = request.RequestUri!.AbsolutePath.TrimStart('/');

        // Match on a proper path-segment boundary (exact match, or the prefix is followed by '/') and
        // prefer the LONGEST matching prefix — not just the first-registered one. Without this, a
        // route like "api/project-revisions/{id}" registered before "api/project-revisions/{id}/mto"
        // would swallow every request to the more specific sub-route too (both share that prefix),
        // silently serving the wrong canned response instead of a 404 or the intended match.
        var candidates = _routes.Where(r =>
        {
            var parts = r.Key.Split(' ', 2);
            if (!string.Equals(parts[0], request.Method.Method, StringComparison.OrdinalIgnoreCase)) return false;
            var prefix = parts[1].TrimStart('/');
            return path.Equals(prefix, StringComparison.Ordinal) ||
                   (path.StartsWith(prefix, StringComparison.Ordinal) && path.Length > prefix.Length && path[prefix.Length] == '/');
        });
        var match = candidates.OrderByDescending(r => r.Key.Length).FirstOrDefault();

        if (match.Respond is null)
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent($"No fake route registered for {request.Method} {path}") });

        return Task.FromResult(match.Respond(request));
    }
}
