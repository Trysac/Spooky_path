using System.Collections.Generic;

namespace SpookyPath.Core
{
    /// <summary>
    /// Which cells the explorer can reach from the start: walking between floor cells (doors and
    /// gates count as passable, since their keys and switches are checked separately), jumping a
    /// one-cell pit or lava gap, riding a platform track end to end and stepping through a
    /// teleporter to its twin. Used to prove every level can be finished.
    /// </summary>
    public static class Reachability
    {
        public static bool[,] From(LevelMap map, int startX, int startY)
        {
            var reached = new bool[map.Width, map.Height];
            if (!map.IsWalkable(startX, startY))
            {
                return reached;
            }
            List<Placement> teleporters = map.All(PlacementKind.Teleporter);
            var queue = new Queue<(int, int)>();
            reached[startX, startY] = true;
            queue.Enqueue((startX, startY));
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            while (queue.Count > 0)
            {
                (int x, int y) = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d], ny = y + dy[d];
                    if (map.IsWalkable(nx, ny))
                    {
                        Visit(nx, ny);
                    }
                    else if (IsTrack(map, nx, ny))
                    {
                        while (IsTrack(map, nx, ny))
                        {
                            nx += dx[d];
                            ny += dy[d];
                        }
                        if (map.IsWalkable(nx, ny))
                        {
                            Visit(nx, ny);
                        }
                    }
                    else if (map.At(nx, ny) != Tile.Wall && map.IsWalkable(x + 2 * dx[d], y + 2 * dy[d]))
                    {
                        Visit(x + 2 * dx[d], y + 2 * dy[d]);
                    }
                }
                foreach (Placement pad in teleporters)
                {
                    if (pad.X != x || pad.Y != y)
                    {
                        continue;
                    }
                    foreach (Placement twin in teleporters)
                    {
                        if (twin.Variant == pad.Variant && (twin.X != x || twin.Y != y))
                        {
                            Visit(twin.X, twin.Y);
                        }
                    }
                }
            }
            return reached;

            void Visit(int vx, int vy)
            {
                if (!reached[vx, vy])
                {
                    reached[vx, vy] = true;
                    queue.Enqueue((vx, vy));
                }
            }
        }

        static bool IsTrack(LevelMap map, int x, int y)
        {
            foreach (PlatformTrack track in map.Tracks)
            {
                if (x >= track.StartX && x <= track.EndX && y >= track.StartY && y <= track.EndY)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
