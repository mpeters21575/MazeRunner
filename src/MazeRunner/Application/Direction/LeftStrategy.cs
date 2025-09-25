namespace MazeRunner.Application.Direction;

using HightechICT.Amazeing.Client.Rest;

public sealed class LeftStrategy : IDirectionStrategy
{
    public Direction Value => Direction.Left;
    public bool MatchesToken(string t) => new[] { "l", "left", "←" }.Contains(t, StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> Aliases => new[] { "l", "left", "←" };
}
