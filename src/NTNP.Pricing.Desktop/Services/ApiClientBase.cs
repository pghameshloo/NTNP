using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NTNP.Pricing.Contracts.Auth;
using NTNP.Pricing.Contracts.Common;

namespace NTNP.Pricing.Desktop.Services;

/// <summary>
/// Section 33 — every desktop call to the server goes through here: it resolves the current API
/// base URL from <see cref="IServerConnectionSettingsService"/> on every call (so re-pointing the
/// client via the Server Connection Settings screen takes effect immediately, no restart), attaches
/// the current bearer token, transparently refreshes and retries once on a 401, and translates any
/// non-success response into a rich <see cref="ApiException"/> instead of leaving callers to parse
/// raw HTTP. This is the ONE place that talks HTTP — every module's typed client is a thin wrapper
/// over the Get/Post/Put/Delete helpers below (Section 33: the desktop client never touches SQL
/// Server directly; this class is its only channel to the server).
/// </summary>
public class ApiClientBase
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IServerConnectionSettingsService _serverSettings;
    private readonly AppSession _session;

    // Guards against multiple concurrent requests each independently trying to refresh on a 401.
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public ApiClientBase(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session)
    {
        _http = http;
        _serverSettings = serverSettings;
        _session = session;
    }

    protected Task<TResponse> GetAsync<TResponse>(string relativeUrl, CancellationToken ct = default) =>
        SendAsync<TResponse>(() => new HttpRequestMessage(HttpMethod.Get, BuildUri(relativeUrl)), ct);

    protected Task<TResponse> PostAsync<TResponse>(string relativeUrl, object? body = null, CancellationToken ct = default) =>
        SendAsync<TResponse>(() => WithBody(new HttpRequestMessage(HttpMethod.Post, BuildUri(relativeUrl)), body), ct);

    protected Task PostAsync(string relativeUrl, object? body = null, CancellationToken ct = default) =>
        SendAsync(() => WithBody(new HttpRequestMessage(HttpMethod.Post, BuildUri(relativeUrl)), body), ct);

    protected Task<TResponse> PutAsync<TResponse>(string relativeUrl, object? body = null, CancellationToken ct = default) =>
        SendAsync<TResponse>(() => WithBody(new HttpRequestMessage(HttpMethod.Put, BuildUri(relativeUrl)), body), ct);

    protected Task DeleteAsync(string relativeUrl, object? body = null, CancellationToken ct = default) =>
        SendAsync(() => WithBody(new HttpRequestMessage(HttpMethod.Delete, BuildUri(relativeUrl)), body), ct);

    protected Task<TResponse> DeleteAsync<TResponse>(string relativeUrl, object? body = null, CancellationToken ct = default) =>
        SendAsync<TResponse>(() => WithBody(new HttpRequestMessage(HttpMethod.Delete, BuildUri(relativeUrl)), body), ct);

    /// <summary>Downloads a binary payload (a report PDF/Excel) as raw bytes plus its server-suggested filename.</summary>
    protected async Task<(byte[] Bytes, string? FileName, string ContentType)> GetBytesAsync(string relativeUrl, CancellationToken ct = default)
    {
        var response = await SendRawAsync(() => new HttpRequestMessage(HttpMethod.Get, BuildUri(relativeUrl)), ct);
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName;
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return (bytes, fileName?.Trim('"'), contentType);
    }

    protected async Task<TResponse> PostFileAsync<TResponse>(string relativeUrl, byte[] fileContent, string fileName, CancellationToken ct = default)
    {
        return await SendAsync<TResponse>(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(relativeUrl));
            var multipart = new MultipartFormDataContent();
            var fileContentPart = new ByteArrayContent(fileContent);
            fileContentPart.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            multipart.Add(fileContentPart, "file", fileName);
            request.Content = multipart;
            return request;
        }, ct);
    }

    private Uri BuildUri(string relativeUrl) => new(new Uri(_serverSettings.ApiBaseUrl.TrimEnd('/') + "/"), relativeUrl.TrimStart('/'));

    private static HttpRequestMessage WithBody(HttpRequestMessage request, object? body)
    {
        if (body is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        return request;
    }

    private async Task<TResponse> SendAsync<TResponse>(Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        var response = await SendRawAsync(requestFactory, ct);
        if (typeof(TResponse) == typeof(NoContentMarker))
            return default!;

        var result = await response.Content.ReadFromJsonAsync<TResponse>(Json, ct);
        return result ?? throw new ApiException((int)response.StatusCode, new ApiErrorResponse("empty-response", "Empty response", (int)response.StatusCode, new[] { "The server returned an empty response body." }));
    }

    private async Task SendAsync(Func<HttpRequestMessage> requestFactory, CancellationToken ct) => await SendRawAsync(requestFactory, ct);

    private async Task<HttpResponseMessage> SendRawAsync(Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        var response = await SendOnceAsync(requestFactory, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_session.RefreshToken))
        {
            response.Dispose();
            if (await TryRefreshAsync(ct))
            {
                response = await SendOnceAsync(requestFactory, ct);
            }
        }

        if (response.IsSuccessStatusCode)
        {
            _session.IsServerReachable = true;
            return response;
        }

        ApiErrorResponse? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(Json, ct);
        }
        catch
        {
            // Non-JSON error body (e.g. a raw 502 from a reverse proxy) — fall through with error == null.
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            _session.Clear(); // the refresh attempt (if any) also failed — the session is truly over

        throw new ApiException((int)response.StatusCode, error);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        var request = requestFactory();
        if (!string.IsNullOrEmpty(_session.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        try
        {
            var response = await _http.SendAsync(request, ct);
            _session.IsServerReachable = true;
            return response;
        }
        catch (HttpRequestException)
        {
            _session.IsServerReachable = false;
            throw;
        }
    }

    private async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        await RefreshLock.WaitAsync(ct);
        try
        {
            // Another concurrent call may have already refreshed while we waited for the lock.
            if (string.IsNullOrEmpty(_session.RefreshToken)) return false;

            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, BuildUri("api/auth/refresh"))
            {
                Content = new StringContent(JsonSerializer.Serialize(new RefreshTokenRequest(_session.RefreshToken), Json), Encoding.UTF8, "application/json"),
            };
            var response = await _http.SendAsync(refreshRequest, ct);
            if (!response.IsSuccessStatusCode) return false;

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>(Json, ct);
            if (login is null) return false;

            _session.ApplyLogin(login, remember: _session.TryLoadRememberedRefreshToken() is not null);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    /// <summary>Sentinel used with <see cref="SendAsync{TResponse}"/> for endpoints that return 204 No Content.</summary>
    protected sealed class NoContentMarker;
}
