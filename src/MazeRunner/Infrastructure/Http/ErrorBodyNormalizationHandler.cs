using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MazeRunner.Infrastructure.Http;

sealed class ErrorBodyNormalizationHandler : DelegatingHandler
{
    static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> Map =
        new Dictionary<string, IReadOnlyDictionary<int, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["/api/mazes/enter"] = new Dictionary<int, string>
            {
                [400] = "Maze name is required.",
                [404] = "Maze not found.",
                [409] = "Already in a maze or already played this maze.",
                [412] = "Register first."
            },
            ["/api/maze/move"] = new Dictionary<int, string> { [412] = "Enter a maze first." },
            ["/api/maze/collectScore"] = new Dictionary<int, string>
            {
                [403] = "Not on a collection tile or hand is empty.",
                [412] = "Enter a maze first."
            },
            ["/api/maze/exit"] = new Dictionary<int, string>
            {
                [403] = "Not on an exit tile.",
                [412] = "Enter a maze first."
            },
            ["/api/maze/possibleActions"] = new Dictionary<int, string> { [412] = "Enter a maze first." },
            ["/api/player/register"] = new Dictionary<int, string>
            {
                [400] = "Name must be 1–50 chars.",
                [409] = "Already registered. Use 'forget' to re-register."
            },
            ["/api/player"] = new Dictionary<int, string> { [404] = "You haven't registered yet." }
        };

    static bool LooksLikeJson(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var t = s.TrimStart();
        if (!(t.StartsWith("{") || t.StartsWith("["))) return false;
        try { using var _ = JsonDocument.Parse(t); return true; } catch { return false; }
    }

    static string Friendly(string path, int status)
        => Map.TryGetValue(path, out var by) && by.TryGetValue(status, out var msg) ? msg : $"HTTP {status}";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await base.SendAsync(request, ct).ConfigureAwait(false);
        if (response.IsSuccessStatusCode) return response;

        var path = request.RequestUri is null ? "" : request.RequestUri.AbsolutePath;
        var status = (int)response.StatusCode;

        string? body = null;
        if (response.Content != null)
        {
            body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (LooksLikeJson(body))
            {
                if (response.Content.Headers.ContentType is { } ctHeader &&
                    !ctHeader.MediaType!.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return response;
            }
        }

        var problem = new
        {
            title = Friendly(path, status),
            status,
            detail = string.IsNullOrWhiteSpace(body) ? null : body
        };

        var json = JsonSerializer.Serialize(problem);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var normalized = new HttpResponseMessage(response.StatusCode)
        {
            ReasonPhrase = response.ReasonPhrase,
            RequestMessage = response.RequestMessage,
            Version = response.Version,
            Content = content
        };

        foreach (var h in response.Headers) normalized.Headers.TryAddWithoutValidation(h.Key, h.Value);
        response.Dispose();
        return normalized;
    }
}
