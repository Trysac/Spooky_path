using System;
using System.Collections.Generic;

namespace SpookyPath.Core
{
    public enum Tile
    {
        /// <summary>A bottomless pit: falling in is fatal.</summary>
        Void,
        Floor,
        Wall,
        /// <summary>Molten rock: touching it is fatal.</summary>
        Lava,
    }

    public enum PlacementKind
    {
        PlayerStart,
        Exit,
        Artefact,
        /// <summary>A dark brazier that shields the artefact until the explorer purifies it.</summary>
        Ward,
        Enemy,
        Potion,
        Coin,
        Candy,
        /// <summary>A treasure chest worth a lot of points.</summary>
        Relic,
        Key,
        Door,
        Lever,
        Plate,
        Gate,
        Teleporter,
        /// <summary>One cell of a moving platform's track over lava.</summary>
        Platform,
        Spikes,
        Sign,
    }

    public readonly struct Placement
    {
        public readonly PlacementKind Kind;
        public readonly int X;
        public readonly int Y;
        /// <summary>Enemy kind, potion kind, puzzle channel, teleporter pair or sign number, by kind.</summary>
        public readonly int Variant;
        /// <summary>For enemies: true when it patrols instead of standing guard.</summary>
        public readonly bool Patrols;

        public Placement(PlacementKind kind, int x, int y, int variant = 0, bool patrols = false)
        {
            Kind = kind;
            X = x;
            Y = y;
            Variant = variant;
            Patrols = patrols;
        }

        public override string ToString() => $"{Kind}({X},{Y},{Variant})";
    }

    /// <summary>A straight run of track cells; the platform shuttles between its two ends.</summary>
    public readonly struct PlatformTrack
    {
        public readonly int StartX;
        public readonly int StartY;
        public readonly int EndX;
        public readonly int EndY;

        public PlatformTrack(int startX, int startY, int endX, int endY)
        {
            StartX = startX;
            StartY = startY;
            EndX = endX;
            EndY = endY;
        }

        public int Length => Math.Abs(EndX - StartX) + Math.Abs(EndY - StartY) + 1;
    }

    /// <summary>
    /// Parses a level drawn as text, one character per 4 m cell. Row 0 is the far (north) edge.
    /// <code>
    ///   ' ' pit       '#' wall      '.' floor     '~' lava
    ///   'P' start     'E' exit      'A' artefact  'w' ward
    ///   '1' '2' '3'   minion, warrior, mage standing guard
    ///   '4' '5' '6'   minion, warrior, mage on patrol (east-west)
    ///   'h' 'm' 's' 'x'  health, mana, stamina potion, elixir
    ///   'c' coin      'y' candy     'r' relic chest
    ///   'k' key       'D' locked door
    ///   'l' lever     'o' pressure plate      'G' gate
    ///   'T' teleporter (paired in reading order)
    ///   '=' moving platform track over lava   '^' spike trap
    ///   'i' sign (texts in reading order)
    /// </code>
    /// Levers, plates and gates use channel 0 unless the level lists channels for them in reading order.
    /// </summary>
    public sealed class LevelMap
    {
        public const int PatrolReach = 4;

        readonly Tile[,] tiles;
        readonly List<Placement> placements = new List<Placement>();
        readonly List<PlatformTrack> tracks = new List<PlatformTrack>();
        readonly List<string> errors = new List<string>();

        LevelMap(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new Tile[width, height];
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<Placement> Placements => placements;
        public IReadOnlyList<PlatformTrack> Tracks => tracks;
        /// <summary>Problems found while parsing; an empty list means the level is playable.</summary>
        public IReadOnlyList<string> Errors => errors;
        public bool IsValid => errors.Count == 0;

        public Placement Start
        {
            get
            {
                foreach (Placement placement in placements)
                {
                    if (placement.Kind == PlacementKind.PlayerStart)
                    {
                        return placement;
                    }
                }
                throw new InvalidOperationException("The level has no start.");
            }
        }

        public Tile At(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? Tile.Void : tiles[x, y];

        /// <summary>True for cells the explorer can stand on (floor, including every placement on floor).</summary>
        public bool IsWalkable(int x, int y) => At(x, y) == Tile.Floor;

        /// <summary>World position (x, z) of a cell's centre.</summary>
        public static Vec2 CellCenter(int x, int y) => new Vec2(x * Tuning.CellSize, -y * Tuning.CellSize);

        /// <summary>The cell containing a world position.</summary>
        public static void CellOf(Vec2 position, out int x, out int y)
        {
            x = (int)Math.Floor(position.X / Tuning.CellSize + 0.5f);
            y = (int)Math.Floor(-position.Y / Tuning.CellSize + 0.5f);
        }

        public List<Placement> All(PlacementKind kind)
        {
            var found = new List<Placement>();
            foreach (Placement placement in placements)
            {
                if (placement.Kind == kind)
                {
                    found.Add(placement);
                }
            }
            return found;
        }

        /// <summary>
        /// The far end of a patrolling enemy's beat: the furthest floor cell east of it, up to
        /// <see cref="PatrolReach"/> cells, or west when east is blocked.
        /// </summary>
        public int PatrolEndX(Placement enemy)
        {
            int east = Reach(enemy.X, enemy.Y, 1);
            return east != enemy.X ? east : Reach(enemy.X, enemy.Y, -1);
        }

        int Reach(int x, int y, int step)
        {
            int end = x;
            for (int i = 1; i <= PatrolReach && IsWalkable(x + i * step, y); i++)
            {
                end = x + i * step;
            }
            return end;
        }

        public static LevelMap Parse(IReadOnlyList<string> rows, IReadOnlyList<int> switchChannels = null, IReadOnlyList<int> gateChannels = null)
        {
            if (rows == null || rows.Count == 0)
            {
                throw new ArgumentException("A level needs at least one row.", nameof(rows));
            }
            int width = 0;
            foreach (string row in rows)
            {
                width = Math.Max(width, row.Length);
            }
            var map = new LevelMap(width, rows.Count);
            int switches = 0, gates = 0, teleporters = 0, signs = 0;
            for (int y = 0; y < rows.Count; y++)
            {
                string row = rows[y];
                for (int x = 0; x < width; x++)
                {
                    char c = x < row.Length ? row[x] : ' ';
                    map.Read(c, x, y, switchChannels, gateChannels, ref switches, ref gates, ref teleporters, ref signs);
                }
            }
            map.BuildTracks();
            map.Validate(switchChannels, gateChannels, switches, gates, teleporters);
            return map;
        }

        void Read(char c, int x, int y, IReadOnlyList<int> switchChannels, IReadOnlyList<int> gateChannels,
            ref int switches, ref int gates, ref int teleporters, ref int signs)
        {
            Tile tile = Tile.Floor;
            switch (c)
            {
                case ' ': tile = Tile.Void; break;
                case '#': tile = Tile.Wall; break;
                case '~': tile = Tile.Lava; break;
                case '.': break;
                case 'P': Add(PlacementKind.PlayerStart, x, y); break;
                case 'E': Add(PlacementKind.Exit, x, y); break;
                case 'A': Add(PlacementKind.Artefact, x, y); break;
                case 'w': Add(PlacementKind.Ward, x, y); break;
                case '1': case '2': case '3':
                    Add(PlacementKind.Enemy, x, y, c - '1');
                    break;
                case '4': case '5': case '6':
                    placements.Add(new Placement(PlacementKind.Enemy, x, y, c - '4', true));
                    break;
                case 'h': Add(PlacementKind.Potion, x, y, (int)PotionKind.Health); break;
                case 'm': Add(PlacementKind.Potion, x, y, (int)PotionKind.Mana); break;
                case 's': Add(PlacementKind.Potion, x, y, (int)PotionKind.Stamina); break;
                case 'x': Add(PlacementKind.Potion, x, y, (int)PotionKind.Elixir); break;
                case 'c': Add(PlacementKind.Coin, x, y); break;
                case 'y': Add(PlacementKind.Candy, x, y); break;
                case 'r': Add(PlacementKind.Relic, x, y); break;
                case 'k': Add(PlacementKind.Key, x, y); break;
                case 'D': Add(PlacementKind.Door, x, y); break;
                case 'l':
                    Add(PlacementKind.Lever, x, y, Channel(switchChannels, switches++));
                    break;
                case 'o':
                    Add(PlacementKind.Plate, x, y, Channel(switchChannels, switches++));
                    break;
                case 'G':
                    Add(PlacementKind.Gate, x, y, Channel(gateChannels, gates++));
                    break;
                case 'T':
                    Add(PlacementKind.Teleporter, x, y, teleporters++ / 2);
                    break;
                case '=':
                    tile = Tile.Lava;
                    Add(PlacementKind.Platform, x, y);
                    break;
                case '^': Add(PlacementKind.Spikes, x, y); break;
                case 'i': Add(PlacementKind.Sign, x, y, signs++); break;
                default:
                    errors.Add($"Unknown character '{c}' at ({x},{y}).");
                    tile = Tile.Void;
                    break;
            }
            tiles[x, y] = tile;
        }

        void Add(PlacementKind kind, int x, int y, int variant = 0) => placements.Add(new Placement(kind, x, y, variant));

        static int Channel(IReadOnlyList<int> channels, int index) =>
            channels != null && index < channels.Count ? channels[index] : 0;

        void BuildTracks()
        {
            var seen = new bool[Width, Height];
            foreach (Placement cell in All(PlacementKind.Platform))
            {
                if (seen[cell.X, cell.Y])
                {
                    continue;
                }
                // Flood the connected track cells and check they form a straight line.
                var stack = new Stack<(int, int)>();
                stack.Push((cell.X, cell.Y));
                int minX = cell.X, maxX = cell.X, minY = cell.Y, maxY = cell.Y, count = 0;
                while (stack.Count > 0)
                {
                    (int x, int y) = stack.Pop();
                    if (x < 0 || y < 0 || x >= Width || y >= Height || seen[x, y] || !IsTrack(x, y))
                    {
                        continue;
                    }
                    seen[x, y] = true;
                    count++;
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                    stack.Push((x + 1, y));
                    stack.Push((x - 1, y));
                    stack.Push((x, y + 1));
                    stack.Push((x, y - 1));
                }
                if (minX != maxX && minY != maxY || count != (maxX - minX) + (maxY - minY) + 1)
                {
                    errors.Add($"The platform track at ({cell.X},{cell.Y}) is not a straight line.");
                    continue;
                }
                if (count < 2)
                {
                    errors.Add($"The platform track at ({cell.X},{cell.Y}) needs at least two cells.");
                    continue;
                }
                tracks.Add(new PlatformTrack(minX, minY, maxX, maxY));
            }
        }

        bool IsTrack(int x, int y)
        {
            foreach (Placement placement in placements)
            {
                if (placement.Kind == PlacementKind.Platform && placement.X == x && placement.Y == y)
                {
                    return true;
                }
            }
            return false;
        }

        void Validate(IReadOnlyList<int> switchChannels, IReadOnlyList<int> gateChannels, int switches, int gates, int teleporters)
        {
            int starts = All(PlacementKind.PlayerStart).Count;
            if (starts != 1)
            {
                errors.Add($"A level needs exactly one start 'P' (found {starts}).");
            }
            if (All(PlacementKind.Exit).Count == 0 && All(PlacementKind.Artefact).Count == 0)
            {
                errors.Add("A level needs an exit 'E' or the artefact 'A'.");
            }
            if (All(PlacementKind.Artefact).Count > 0 && All(PlacementKind.Ward).Count == 0)
            {
                errors.Add("The artefact needs at least one ward 'w'.");
            }
            if (teleporters % 2 != 0)
            {
                errors.Add("Teleporters 'T' must come in pairs.");
            }
            int keys = All(PlacementKind.Key).Count;
            int doors = All(PlacementKind.Door).Count;
            if (keys < doors)
            {
                errors.Add($"There are {doors} locked doors but only {keys} keys.");
            }
            if (switchChannels != null && switchChannels.Count != switches)
            {
                errors.Add($"The level lists {switchChannels.Count} switch channels for {switches} levers and plates.");
            }
            if (gateChannels != null && gateChannels.Count != gates)
            {
                errors.Add($"The level lists {gateChannels.Count} gate channels for {gates} gates.");
            }
            foreach (Placement gate in All(PlacementKind.Gate))
            {
                bool opened = false;
                foreach (Placement placement in placements)
                {
                    if ((placement.Kind == PlacementKind.Lever || placement.Kind == PlacementKind.Plate) && placement.Variant == gate.Variant)
                    {
                        opened = true;
                    }
                }
                if (!opened)
                {
                    errors.Add($"The gate at ({gate.X},{gate.Y}) has no lever or plate on channel {gate.Variant}.");
                }
            }
        }
    }
}
