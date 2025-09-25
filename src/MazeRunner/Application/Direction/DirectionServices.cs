namespace MazeRunner.Application.Direction;

using HightechICT.Amazeing.Client.Rest;

public interface IDirectionParser
{
    Direction Parse(string token);
}

public sealed class DirectionParser(IEnumerable<IDirectionStrategy> strategies) : IDirectionParser
{
    readonly IReadOnlyList<IDirectionStrategy> _strategies = strategies.ToList();
    public Direction Parse(string token) => _strategies.First(s => s.MatchesToken(token)).Value;
}
