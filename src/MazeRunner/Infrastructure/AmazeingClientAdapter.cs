using System.Text.Json;
using HightechICT.Amazeing.Client.Rest;
using MazeRunner.Application;
using Microsoft.Extensions.Configuration;

namespace MazeRunner.Infrastructure;

public sealed class AmazeingClientAdapter : IMazeService
{
    private readonly AmazeingClient _client;

    public AmazeingClientAdapter(HttpClient http, IConfiguration cfg)
    {
        var baseUrl = cfg["Api:BaseUrl"] ?? "https://maze.kluster.htiprojects.nl/";
        var token   = cfg["Api:Token"] ?? string.Empty;
        var timeout = int.TryParse(cfg["Api:TimeoutSeconds"], out var t) ? t : 30;

        http.Timeout = TimeSpan.FromSeconds(timeout);
        if (!string.IsNullOrWhiteSpace(token))
            http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"HTI Thanks You {token}");

        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/json, text/plain, */*");

        _client = new AmazeingClient(http) { BaseUrl = baseUrl, ReadResponseAsString = true };
    }

    public Task<PlayerInfo> RegisterAsync(string name, CancellationToken ct) =>
        Invoke("register", () => _client.RegisterPlayer(name, ct));

    public Task ForgetAsync(CancellationToken ct) =>
        Invoke("forget", async () => { await _client.ForgetPlayer(ct); return 0; });

    public async Task<IReadOnlyList<MazeInfo>> AllMazesAsync(CancellationToken ct) =>
        await Invoke("list", async () => (await _client.AllMazes(ct)).ToList());

    public Task<PossibleActionsAndCurrentScore> EnterAsync(string mazeName, CancellationToken ct) =>
        Invoke("enter", () => _client.EnterMaze(mazeName, ct));

    public Task<PossibleActionsAndCurrentScore> MoveAsync(Direction direction, CancellationToken ct) =>
        Invoke("move", () => _client.Move(direction, ct));

    public Task<PossibleActionsAndCurrentScore> CollectScoreAsync(CancellationToken ct) =>
        Invoke("collect", () => _client.CollectScore(ct));

    public Task ExitMazeAsync(CancellationToken ct) =>
        Invoke("exit", async () => { await _client.ExitMaze(ct); return 0; });

    public Task<PlayerInfo> GetPlayerAsync(CancellationToken ct) =>
        Invoke("player", () => _client.GetPlayerInfo(ct));

    public Task<PossibleActionsAndCurrentScore> PossibleActionsAsync(CancellationToken ct) =>
        Invoke("status", () => _client.PossibleActions(ct));

    static bool LooksLikeJson(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var t = s.TrimStart();
        if (!(t.StartsWith("{") || t.StartsWith("["))) return false;
        try { using var _ = JsonDocument.Parse(t); return true; } catch { return false; }
    }

    static string Friendly(string action, int status) =>
        new Dictionary<string, Dictionary<int,string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["enter"]   = new() { [400]="Maze name is required.", [404]="Maze not found.", [409]="Already in or already played this maze.", [412]="Register first." },
            ["move"]    = new() { [412]="Enter a maze first." },
            ["collect"] = new() { [403]="Not on a collection tile or hand is empty.", [412]="Enter a maze first." },
            ["exit"]    = new() { [403]="Not on an exit tile.", [412]="Enter a maze first." },
            ["status"]  = new() { [412]="Enter a maze first." },
            ["register"]= new() { [400]="Name must be 1–50 chars.", [409]="Already registered. Use 'forget' to re-register." },
            ["list"]    = new()
        }.TryGetValue(action, out var by) && by.TryGetValue(status, out var msg) ? msg : $"HTTP {status}";

    async Task<T> Invoke<T>(string action, Func<Task<T>> call)
    {
        try { return await call(); }
        catch (ApiException ex)
        {
            var noisy =
                ex.InnerException is JsonException ||
                ex.Message.StartsWith("Could not deserialize the response body stream as", StringComparison.Ordinal) ||
                ex.Message.StartsWith("Could not deserialize the response body string as", StringComparison.Ordinal) ||
                !LooksLikeJson(ex.Response);

            if (!noisy) throw;

            var msg = Friendly(action, (int)ex.StatusCode);
            throw new ApiException(msg, ex.StatusCode, ex.Response, ex.Headers, ex);
        }
    }
}
