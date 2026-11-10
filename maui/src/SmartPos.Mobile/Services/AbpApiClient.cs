using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class AbpApiClient
{
    private readonly HttpClient _http;
    private readonly SessionService _session;
    private readonly AppSettings _settings;

    public AbpApiClient(HttpClient http, SessionService session, AppSettings settings)
    {
        _http = http;
        _session = session;
        _settings = settings;
        if (_http.BaseAddress == null && !string.IsNullOrWhiteSpace(_settings.ApiBaseUrl))
        {
            _http.BaseAddress = new Uri(_settings.ApiBaseUrl.TrimEnd('/') + "/");
        }
    }

    public string ResolveImageUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        var baseUrl = _settings.ApiBaseUrl.TrimEnd('/');
        return path.StartsWith('/') ? baseUrl + path : $"{baseUrl}/{path}";
    }

    public async Task<T> GetAsync<T>(string relativeUrl, CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Get, relativeUrl);
        using var response = await _http.SendAsync(request, ct);
        return await ReadAsync<T>(response, ct);
    }

    public async Task<T> PostAsync<T>(string relativeUrl, object? body, CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Post, relativeUrl);
        if (body != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonDefaults.Options), Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(request, ct);
        return await ReadAsync<T>(response, ct);
    }

    public async Task PostAsync(string relativeUrl, object? body, CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Post, relativeUrl);
        if (body != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonDefaults.Options), Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<T> PutAsync<T>(string relativeUrl, object? body, CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Put, relativeUrl);
        if (body != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonDefaults.Options), Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(request, ct);
        return await ReadAsync<T>(response, ct);
    }

    public async Task DeleteAsync(string relativeUrl, CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, relativeUrl);
        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl)
    {
        var request = new HttpRequestMessage(method, relativeUrl.TrimStart('/'));
        if (!string.IsNullOrWhiteSpace(_session.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        }

        if (_session.TenantId.HasValue)
        {
            request.Headers.TryAddWithoutValidation("Abp.TenantId", _session.TenantId.Value.ToString());
        }

        if (_session.BranchId.HasValue)
        {
            request.Headers.TryAddWithoutValidation("SmartPos.BranchId", _session.BranchId.Value.ToString());
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(ExtractError(json) ?? $"HTTP {(int)response.StatusCode}");
        }

        var envelope = JsonSerializer.Deserialize<AbpResponse<T>>(json, JsonDefaults.Options);
        if (envelope == null)
        {
            throw new ApiException("Empty API response.");
        }

        if (!envelope.Success || envelope.Error != null)
        {
            throw new ApiException(envelope.Error?.Message ?? envelope.Error?.Details ?? "Request failed.");
        }

        return envelope.Result!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(ExtractError(json) ?? $"HTTP {(int)response.StatusCode}");
        }

        var envelope = JsonSerializer.Deserialize<AbpResponse<object>>(json, JsonDefaults.Options);
        if (envelope != null && (!envelope.Success || envelope.Error != null))
        {
            throw new ApiException(envelope.Error?.Message ?? "Request failed.");
        }
    }

    private static string? ExtractError(string json)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<AbpResponse<object>>(json, JsonDefaults.Options);
            return envelope?.Error?.Message ?? envelope?.Error?.Details;
        }
        catch
        {
            return null;
        }
    }
}

public class ApiException : Exception
{
    public ApiException(string message) : base(message)
    {
    }
}
