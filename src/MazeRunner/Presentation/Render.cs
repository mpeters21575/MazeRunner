using System.Text.Json;
using HightechICT.Amazeing.Client.Rest;
using Spectre.Console;

namespace MazeRunner.Presentation;

public static class Render
{
    public static void Json(object o)
    {
        var json = JsonSerializer.Serialize(o, new JsonSerializerOptions { WriteIndented = true });
        var escaped = Markup.Escape(json);
        AnsiConsole.Write(new Panel(escaped).Header("State").Border(BoxBorder.Rounded).Expand());
    }

    public static void Mazes(IEnumerable<MazeInfo> list)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Name");
        table.AddColumn("Potential rewards");
        table.AddColumn("Total tiles");
        foreach (var m in list)
            table.AddRow(m.Name, m.PotentialReward.ToString(), m.TotalTiles.ToString());
        AnsiConsole.Write(table);
    }

    public static void Map(string ascii)
    {
        var text = new Text(ascii);
        AnsiConsole.Write(new Panel(text).Header("Maze").Border(BoxBorder.Rounded).Expand());
    }

    public static void Info(string m) => AnsiConsole.MarkupLine($"[green]{Markup.Escape(m)}[/]");
    public static void Warn(string m) => AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(m)}[/]");
    public static void Error(string m) => AnsiConsole.MarkupLine($"[red]{Markup.Escape(m)}[/]");

    public static void ApiError(ApiException ex)
    {
        var message = Markup.Escape(ex.Message ?? "");
        var body = string.IsNullOrWhiteSpace(ex.Response) ? "(empty body)" : Markup.Escape(ex.Response);
        var panel = new Panel($"[red]HTTP {(int)ex.StatusCode}[/]\n{message}\n\n{body}")
            .Header("API Error").Border(BoxBorder.Rounded).Expand();
        
        AnsiConsole.Write(panel);
    }
}