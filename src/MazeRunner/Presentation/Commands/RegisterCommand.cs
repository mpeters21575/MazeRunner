using HightechICT.Amazeing.Client.Rest;
using MazeRunner.Application;
using MazeRunner.Presentation.Errors;
using Spectre.Console;

namespace MazeRunner.Presentation.Commands;

public sealed class RegisterCommand(IMazeService api, IApiErrorHandler errors) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => ["register"];
    public string Usage => "register <name>";

    public async Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        try
        {
            var name = parts.Length > 1
                ? string.Join(" ", parts.Skip(1))
                : AnsiConsole.Prompt(new TextPrompt<string>("name: ").Validate(v => string.IsNullOrWhiteSpace(v)
                    ? ValidationResult.Error("required")
                    : ValidationResult.Success()));
            Render.Json(await api.RegisterAsync(name, ct));
        }
        catch (ApiException ex) when (errors.TryHandle("register", ex))
        {
        }
        catch (ApiException ex)
        {
            Render.ApiError(ex);
        }

        return true;
    }
}