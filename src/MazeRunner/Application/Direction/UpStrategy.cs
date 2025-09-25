namespace MazeRunner.Application.Direction;

public sealed class UpStrategy : IDirectionStrategy
{
    public HightechICT.Amazeing.Client.Rest.Direction Value => HightechICT.Amazeing.Client.Rest.Direction.Up;
    public bool MatchesToken(string t) => new[] { "u", "up", "↑" }.Contains(t, StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> Aliases => new[] { "u", "up", "↑" };
}