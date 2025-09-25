using System.Text;
using HightechICT.Amazeing.Client.Rest;

namespace MazeRunner.Infrastructure;

public interface IMapTracker
{
    void Reset();
    void Enter();
    void Move(string directionKey);
    string RenderAscii();
}

public sealed class MapTracker : IMapTracker
{
    readonly Dictionary<(int x,int y), Node> _nodes = new();
    (int x,int y) _pos;

    public void Reset()
    {
        _nodes.Clear();
        _pos = (0,0);
    }

    public void Enter()
    {
        Reset();
        var n = Get(_pos);
        n.IsStart = true;
        Set(_pos, n);
    }

    public void Move(string directionKey)
    {
        var dir = Parse(directionKey);
        var delta = Delta(dir);
        var from = _pos;
        var to = (from.x + delta.dx, from.y + delta.dy);

        var a = Get(from);
        var b = Get(to);

        a.Links.Add(dir);
        b.Links.Add(Opposite(dir));

        Set(from, a);
        Set(to, b);

        _pos = to;
    }

    public string RenderAscii()
    {
        if (_nodes.Count == 0) return "(map is empty)\n";

        var minX = _nodes.Keys.Min(k => k.x);
        var maxX = _nodes.Keys.Max(k => k.x);
        var minY = _nodes.Keys.Min(k => k.y);
        var maxY = _nodes.Keys.Max(k => k.y);

        var sb = new StringBuilder();
        for (var y = maxY; y >= minY; y--)
        {
            var rowTiles = new StringBuilder();
            var rowLinks = new StringBuilder();

            for (var x = minX; x <= maxX; x++)
            {
                var here = (x,y);
                var n = _nodes.TryGetValue(here, out var node) ? node : null;

                rowTiles.Append(Symbol(n, here == _pos));
                if (x < maxX)
                    rowTiles.Append(n is { } && n.Links.Contains(Direction.Right) ? "-" : " ");

                if (y > minY)
                {
                    var hasDown = n is { } && n.Links.Contains(Direction.Down);
                    rowLinks.Append(hasDown ? "|" : " ");
                    if (x < maxX) rowLinks.Append(" ");
                }
            }

            sb.AppendLine(rowTiles.ToString());
            if (y > minY) sb.AppendLine(rowLinks.ToString());
        }

        return sb.ToString();
    }

    static Direction Parse(string key)
    {
        var k = key.Trim().ToLowerInvariant();
        if (new[] { "u", "up", "↑" }.Contains(k)) return Direction.Up;
        if (new[] { "r", "right", "→" }.Contains(k)) return Direction.Right;
        if (new[] { "d", "down", "↓" }.Contains(k)) return Direction.Down;
        if (new[] { "l", "left", "←" }.Contains(k)) return Direction.Left;
        throw new ArgumentException("invalid direction");
    }

    static (int dx,int dy) Delta(Direction d)
    {
        if (d == Direction.Up) return (0, 1);
        if (d == Direction.Right) return (1, 0);
        if (d == Direction.Down) return (0,-1);
        return (-1,0);
    }

    static Direction Opposite(Direction d)
    {
        if (d == Direction.Up) return Direction.Down;
        if (d == Direction.Right) return Direction.Left;
        if (d == Direction.Down) return Direction.Up;
        return Direction.Right;
    }

    static string Symbol(Node? n, bool current)
    {
        if (current) return "@";
        if (n is null) return " ";
        if (n.IsStart) return "S";
        return "o";
    }

    Node Get((int x,int y) p)
    {
        if (_nodes.TryGetValue(p, out var n)) return n;
        n = new Node();
        _nodes[p] = n;
        return n;
    }

    void Set((int x,int y) p, Node n) => _nodes[p] = n;

    sealed class Node
    {
        public bool IsStart { get; set; }
        public HashSet<Direction> Links { get; } = new();
    }
}
