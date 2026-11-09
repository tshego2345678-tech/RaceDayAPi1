using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
namespace RaceDayMvc.Services;

public class ApiResult<T>
{
    public bool Ok { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}

/// <summary>All communication with the Part 2 API goes through this class (no DB access, no Azure access in MVC).</summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ApiClient(HttpClient http, IHttpContextAccessor ctx) { _http = http; _ctx = ctx; }

    public Task<ApiResult<T>> Get<T>(string url) => Send<T>(HttpMethod.Get, url);
    public Task<ApiResult<T>> Post<T>(string url, object? body) => Send<T>(HttpMethod.Post, url, body);
    public Task<ApiResult<T>> Put<T>(string url, object? body) => Send<T>(HttpMethod.Put, url, body);
    public Task<ApiResult<object>> Delete(string url) => Send<object>(HttpMethod.Delete, url);

    public async Task<ApiResult<T>> Upload<T>(string url, IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var part = new StreamContent(file.OpenReadStream());
        part.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(part, "file", file.FileName);
        return await Send<T>(HttpMethod.Post, url, null, content);
    }

    private async Task<ApiResult<T>> Send<T>(HttpMethod method, string url, object? body = null, HttpContent? content = null)
    {
        try
        {
            using var req = new HttpRequestMessage(method, url);
            var token = _ctx.HttpContext?.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (content != null) req.Content = content;
            else if (body != null) req.Content = JsonContent.Create(body, options: Json);

            using var res = await _http.SendAsync(req);
            var text = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                return new ApiResult<T> { Error = Friendly(res.StatusCode, text) };

            T? data = default;
            if (!string.IsNullOrWhiteSpace(text) && typeof(T) != typeof(object))
                data = JsonSerializer.Deserialize<T>(text, Json);
            return new ApiResult<T> { Ok = true, Data = data };
        }
        catch (HttpRequestException) { return new ApiResult<T> { Error = "The RaceDay service is currently unavailable. Please try again shortly." }; }
        catch (TaskCanceledException) { return new ApiResult<T> { Error = "The request timed out. Please try again." }; }
        catch (JsonException) { return new ApiResult<T> { Error = "We received an unexpected response from the server." }; }
    }

    private static string Friendly(HttpStatusCode code, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            // The Part 2 API returns plain-string error messages, e.g. BadRequest("Email is already registered.")
            if (doc.RootElement.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(doc.RootElement.GetString()))
                return doc.RootElement.GetString()!;
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                return m.GetString()!;
        }
        catch
        {
            // Not JSON: plain-text error body
            var t = body.Trim('"', ' ', '\n', '\r');
            if (t.Length is > 0 and < 200) return t;
        }
        return code switch
        {
            HttpStatusCode.Unauthorized => "Invalid credentials or your session has expired.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "The requested item could not be found.",
            HttpStatusCode.Conflict => "That conflicts with existing data (e.g. you are already enrolled).",
            HttpStatusCode.BadRequest => "Some information provided is invalid. Please check and try again.",
            _ => "Something went wrong. Please try again."
        };
    }
}
