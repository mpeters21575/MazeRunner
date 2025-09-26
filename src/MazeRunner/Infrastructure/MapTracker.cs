using System.Text;
using System.Text.Json;
using HightechICT.Amazeing.Client.Rest;

namespace MazeRunner.Infrastructure;

public interface IMapTracker
{
    void Reset();
    void Enter();
    void Move(string directionKey);
    string RenderAscii();
    void LoadState();
    void SaveState();
}

public sealed class MapTracker : IMapTracker
{
    private readonly Dictionary<(int x,int y), Node> _nodes = new();
    private (int x,int y) _possition;
    private static readonly string StateFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mazerunner-state.json");

    public void LoadState()
    {
        try
        {
            if (!File.Exists(StateFile)) return;
            
            var json = File.ReadAllText(StateFile);
            var state = JsonSerializer.Deserialize<MapState>(json);
            if (state == null) return;

            _nodes.Clear();
            _possition = (state.PositionX, state.PositionY);
            
            foreach (var nodeData in state.Nodes)
            {
                var node = new Node
                {
                    IsStart = nodeData.IsStart
                };
                foreach (var direction in nodeData.Links)
                {
                    node.Links.Add((Direction)direction);
                }
                _nodes[(nodeData.X, nodeData.Y)] = node;
            }
        }
        catch
        {
            // If loading fails, start fresh
            Reset();
        }
    }

    public void SaveState()
    {
        try
        {
            var state = new MapState
            {
                PositionX = _possition.x,
                PositionY = _possition.y,
                Nodes = _nodes.Select(kvp => new NodeData
                {
                    X = kvp.Key.x,
                    Y = kvp.Key.y,
                    IsStart = kvp.Value.IsStart,
                    Links = kvp.Value.Links.Select(d => (int)d).ToList()
                }).ToList()
            };

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StateFile, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public void Reset()
    {
        _nodes.Clear();
        _possition = (0,0);
        SaveState();
    }

    public void Enter()
    {
        LoadState();
        // Only reset if we don't have any saved state
        if (_nodes.Count == 0)
        {
            var n = Get(_possition);
            n.IsStart = true;
            Set(_possition, n);
            SaveState();
        }
    }

    public void Move(string directionKey)
    {
        LoadState();
        
        var dir = Parse(directionKey);
        var delta = Delta(dir);
        var from = _possition;
        var to = (from.x + delta.xDirection, from.y + delta.yDirection);

        var fromNode = Get(from);
        var toNode = Get(to);

        fromNode.Links.Add(dir);
        toNode.Links.Add(Opposite(dir));

        Set(from, fromNode);
        Set(to, toNode);

        _possition = to;
        SaveState();
    }

    public string RenderAscii()
    {
        LoadState();
        
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
                var here = (x: x,y);
                var value = _nodes.TryGetValue(here, out var node) ? node : null;

                rowTiles.Append(Symbol(value, here == _possition));
                if (x < maxX)
                    rowTiles.Append(value is { } && value.Links.Contains(Direction.Right) ? "-" : " ");

                if (y > minY)
                {
                    var hasDown = value is { } && value.Links.Contains(Direction.Down);
                    rowLinks.Append(hasDown ? "|" : " ");
                    if (x < maxX) rowLinks.Append(" ");
                }
            }

            sb.AppendLine(rowTiles.ToString());
            if (y > minY) sb.AppendLine(rowLinks.ToString());
        }

        return sb.ToString();
    }

    private static Direction Parse(string key)
    {
        var value = key.Trim().ToLowerInvariant();
        if (new[] { "u", "up", "↑" }.Contains(value)) return Direction.Up;
        if (new[] { "r", "right", "→" }.Contains(value)) return Direction.Right;
        if (new[] { "d", "down", "↓" }.Contains(value)) return Direction.Down;
        if (new[] { "l", "left", "←" }.Contains(value)) return Direction.Left;
        throw new ArgumentException("invalid direction");
    }

    private static (int xDirection, int yDirection) Delta(Direction direction)
    {
        if (direction == Direction.Up) return (0, 1);
        if (direction == Direction.Right) return (1, 0);
        if (direction == Direction.Down) return (0,-1);
        return (-1,0);
    }

    private static Direction Opposite(Direction direction)
    {
        if (direction == Direction.Up) return Direction.Down;
        if (direction == Direction.Right) return Direction.Left;
        if (direction == Direction.Down) return Direction.Up;
        return Direction.Right;
    }

    private static string Symbol(Node? node, bool current)
    {
        if (current) return "@";
        if (node is null) return " ";
        if (node.IsStart) return "S";
        return "o";
    }

    private Node Get((int x,int y) coordinates)
    {
        if (_nodes.TryGetValue(coordinates, out var n)) return n;
        n = new Node();
        _nodes[coordinates] = n;
        return n;
    }

    private void Set((int x,int y) p, Node n) => _nodes[p] = n;

    private sealed class Node
    {
        public bool IsStart { get; set; }
        public HashSet<Direction> Links { get; } = new();
    }
}

// Data classes for JSON serialization
public class MapState
{
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public List<NodeData> Nodes { get; set; } = new();
}

public class NodeData
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsStart { get; set; }
    public List<int> Links { get; set; } = new();
}
