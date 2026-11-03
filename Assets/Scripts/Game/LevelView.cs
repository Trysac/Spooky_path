using System.Collections.Generic;
using SpookyPath.Core;
using UnityEngine;

namespace SpookyPath.Game
{
    public sealed class PickupView
    {
        public Placement Cell;
        public GameObject Root;
        public Vector3 Position;
        public bool Taken;
    }

    public sealed class PassageView
    {
        public Placement Cell;
        public Transform[] Leaves;
        public float[] ClosedYaw;
        public float[] OpenYaw;
        public Collider Blocker;
        public bool Open;
        public float Progress;
        public Vector3 Position;
    }

    public sealed class SwitchView
    {
        public Placement Cell;
        public bool IsPlate;
        public Transform Handle;
        public Renderer Rune;
        public Renderer Knob;
        public Vector3 Position;
        public bool Active;
    }

    public sealed class PadView
    {
        public Placement Cell;
        public Vector3 Position;
    }

    public sealed class PlatformView
    {
        public Transform Root;
        public Vector3 From;
        public Vector3 To;
        public float Phase;
        public Vector3 Delta;
        public Collider Collider;
    }

    public sealed class SpikeView
    {
        public Transform Blades;
        public Vector3 Position;
        public float Offset;
        public bool Raised;
    }

    public sealed class SignView
    {
        public Vector3 Position;
        public string Text;
    }

    public sealed class WardView
    {
        public Vector3 Position;
        public ParticleSystem Flame;
        public Light Light;
        public bool Purified;
    }

    /// <summary>
    /// Builds a level from its text map out of KayKit pieces: floor tiles on stone foundations,
    /// walls only where they face a walkable cell (low on the side facing the camera so they
    /// never hide the explorer), lava, torches, props and every interactive piece. Holds the
    /// views the director updates.
    /// </summary>
    public sealed class LevelView : MonoBehaviour
    {
        public const float LavaHeight = -0.9f;
        const float WallHeight = 4f;
        const float LowWallHeight = 1.1f;
        const float PlatformSpeed = 3.2f;
        const float PlatformPause = 0.9f;
        const float SpikePeriod = 3f;

        GameArt art;
        public LevelMap Map { get; private set; }
        public LevelDefinition Definition { get; private set; }
        public readonly List<PickupView> Pickups = new List<PickupView>();
        public readonly List<PassageView> Doors = new List<PassageView>();
        public readonly List<PassageView> Gates = new List<PassageView>();
        public readonly List<SwitchView> Switches = new List<SwitchView>();
        public readonly List<PadView> Pads = new List<PadView>();
        public readonly List<PlatformView> Platforms = new List<PlatformView>();
        public readonly List<SpikeView> SpikeTraps = new List<SpikeView>();
        public readonly List<SignView> Signs = new List<SignView>();
        public readonly List<WardView> Wards = new List<WardView>();
        public Transform ArtefactCore { get; private set; }
        public GameObject ArtefactShield { get; private set; }
        public Vector3 ArtefactPosition { get; private set; }
        public Vector3 ExitPosition { get; private set; }
        public Vector3 StartPosition { get; private set; }
        public Bounds Extent { get; private set; }

        Transform geometry;

        public static Vector3 World(int x, int y, float height = 0f)
        {
            Vec2 centre = LevelMap.CellCenter(x, y);
            return new Vector3(centre.X, height, centre.Y);
        }

        public static Vec2 Flat(Vector3 position) => new Vec2(position.x, position.z);

        public void Build(GameArt gameArt, LevelDefinition definition)
        {
            art = gameArt;
            Definition = definition;
            Map = definition.Parse();
            geometry = new GameObject("Geometry").transform;
            geometry.SetParent(transform, false);

            BuildGround();
            BuildWalls();
            BuildLava();
            BuildPlacements();
            BuildProps();
            Extent = new Bounds(World(Map.Width / 2, Map.Height / 2), new Vector3(Map.Width * Tuning.CellSize, 8f, Map.Height * Tuning.CellSize));
        }

        int Hash(int x, int y) => ((x * 73856093) ^ (y * 19349663) ^ (Definition.Name.Length * 83492791)) & 0x7fffffff;

        GameObject Place(GameObject prefab, Vector3 position, float yaw, Vector3 scale, Transform parent = null)
        {
            if (prefab == null)
            {
                return new GameObject("Missing");
            }
            GameObject go = Instantiate(prefab, parent != null ? parent : geometry);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = scale;
            foreach (Collider collider in go.GetComponentsInChildren<Collider>())
            {
                Destroy(collider);
            }
            return go;
        }

        BoxCollider Box(string name, Vector3 centre, Vector3 size, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : geometry, false);
            go.transform.localPosition = centre;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            return box;
        }

        // ---- Ground ----

        void BuildGround()
        {
            for (int y = 0; y < Map.Height; y++)
            {
                int runStart = -1;
                for (int x = 0; x <= Map.Width; x++)
                {
                    bool floor = x < Map.Width && Map.IsWalkable(x, y);
                    if (floor && runStart < 0)
                    {
                        runStart = x;
                    }
                    if (!floor && runStart >= 0)
                    {
                        // One collider per run of floor cells keeps the physics scene small.
                        float length = (x - runStart) * Tuning.CellSize;
                        Vector3 centre = (World(runStart, y) + World(x - 1, y)) * 0.5f + Vector3.down * 1f;
                        Box("Ground", centre, new Vector3(length, 2f, Tuning.CellSize));
                        runStart = -1;
                    }
                    if (!floor)
                    {
                        continue;
                    }
                    Vector3 cell = World(x, y);
                    bool special = HasPlacement(x, y, PlacementKind.Exit) || HasPlacement(x, y, PlacementKind.Spikes);
                    if (!special)
                    {
                        GameObject tile = Hash(x, y) % 6 == 0 && art.FloorCracked != null ? art.FloorCracked : art.Floor;
                        Place(tile, cell, (Hash(x, y) % 4) * 90f, Vector3.one);
                    }
                    Place(art.Foundation, cell + Vector3.down * 2.05f, 0f, new Vector3(1.82f, 1f, 1.82f));
                }
            }
        }

        bool HasPlacement(int x, int y, PlacementKind kind)
        {
            foreach (Placement placement in Map.Placements)
            {
                if (placement.Kind == kind && placement.X == x && placement.Y == y)
                {
                    return true;
                }
            }
            return false;
        }

        // ---- Walls ----

        void BuildWalls()
        {
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { -1, 1, 0, 0 };
            for (int y = 0; y < Map.Height; y++)
            {
                for (int x = 0; x < Map.Width; x++)
                {
                    if (Map.At(x, y) != Tile.Wall)
                    {
                        continue;
                    }
                    bool facesFloor = false;
                    bool low = false;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dx[d], ny = y + dy[d];
                        if (!Map.IsWalkable(nx, ny))
                        {
                            continue;
                        }
                        facesFloor = true;
                        // The floor is north of this wall: the wall stands between it and the camera.
                        bool towardCamera = dy[d] == -1;
                        low |= towardCamera;
                        Vector3 normal = new Vector3(dx[d], 0f, -dy[d]);
                        Vector3 edge = World(x, y) + normal * (Tuning.CellSize * 0.5f);
                        float yaw = dx[d] != 0 ? 90f : 0f;
                        GameObject segment = Hash(x + d, y) % 5 == 0 && art.WallCracked != null ? art.WallCracked
                            : Hash(x, y + d) % 7 == 0 && art.WallArched != null ? art.WallArched : art.Wall;
                        float height = towardCamera ? LowWallHeight / WallHeight : 1f;
                        Place(towardCamera ? art.Wall : segment, edge, yaw, new Vector3(1f, height, 1f));
                        if (!towardCamera && Hash(x * 3 + d, y * 5) % 4 == 0)
                        {
                            AddTorch(edge + normal * 0.5f, normal);
                        }
                    }
                    if (!facesFloor)
                    {
                        continue;
                    }
                    float top = low ? LowWallHeight : WallHeight;
                    Fx.Shape("Cap", art.Cube, art.Rock, geometry, World(x, y, top + 0.05f), new Vector3(Tuning.CellSize + 0.6f, 0.1f, Tuning.CellSize + 0.6f));
                    Box("Wall", World(x, y, 3f), new Vector3(Tuning.CellSize, 6f, Tuning.CellSize));
                }
            }
        }

        void AddTorch(Vector3 position, Vector3 normal)
        {
            GameObject torch = Place(art.TorchMounted, position + Vector3.up * 2.3f, Quaternion.LookRotation(normal).eulerAngles.y, Vector3.one * 1.3f);
            Vector3 flame = position + Vector3.up * 3.05f + normal * 0.45f;
            Fx.Flame(geometry, flame, Fx.Hex("#ffb347"), Fx.Hex("#ff4a12"), 0.8f);
            var lightObject = new GameObject("Torch light");
            lightObject.transform.SetParent(torch.transform.parent, false);
            lightObject.transform.localPosition = flame + normal * 0.6f;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Fx.Hex("#ff9a4a");
            light.range = 10f;
            light.intensity = 1.5f;
            light.shadows = LightShadows.None;
            lightObject.AddComponent<Flicker>();
        }

        // ---- Lava ----

        void BuildLava()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            int count = 0;
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            for (int y = 0; y < Map.Height; y++)
            {
                for (int x = 0; x < Map.Width; x++)
                {
                    if (Map.At(x, y) != Tile.Lava)
                    {
                        continue;
                    }
                    Vector3 c = World(x, y, LavaHeight);
                    float h = Tuning.CellSize * 0.5f;
                    int start = vertices.Count;
                    vertices.Add(c + new Vector3(-h, 0f, -h));
                    vertices.Add(c + new Vector3(-h, 0f, h));
                    vertices.Add(c + new Vector3(h, 0f, h));
                    vertices.Add(c + new Vector3(h, 0f, -h));
                    foreach (Vector3 v in vertices.GetRange(start, 4))
                    {
                        uvs.Add(new Vector2(v.x, v.z) / 8f);
                    }
                    triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                    min = Vector3.Min(min, c);
                    max = Vector3.Max(max, c);
                    count++;
                }
            }
            if (count == 0)
            {
                return;
            }
            var mesh = new Mesh { name = "Lava" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            var lava = new GameObject("Lava");
            lava.transform.SetParent(geometry, false);
            lava.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = lava.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = art.Lava;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lava.AddComponent<LavaFlow>();
            Vector3 centre = (min + max) * 0.5f;
            Vector3 size = max - min + Vector3.one * Tuning.CellSize;
            Fx.Embers(centre, new Vector2(size.x, size.z));
            // A warm glow from below so paths over lava read against it.
            var glow = new GameObject("Lava glow");
            glow.transform.SetParent(geometry, false);
            glow.transform.localPosition = centre + Vector3.up * 6f;
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Fx.Hex("#ff5a1a");
            light.range = Mathf.Max(size.x, size.z) * 0.8f;
            light.intensity = 0.9f;
            light.shadows = LightShadows.None;
        }

        // ---- Interactive pieces ----

        void BuildPlacements()
        {
            foreach (Placement p in Map.Placements)
            {
                Vector3 cell = World(p.X, p.Y);
                switch (p.Kind)
                {
                    case PlacementKind.PlayerStart:
                        StartPosition = cell + Vector3.up * 0.1f;
                        break;
                    case PlacementKind.Exit:
                        BuildExit(cell);
                        break;
                    case PlacementKind.Coin:
                        AddPickup(p, art.Coin, cell, 2.6f, 0.9f);
                        break;
                    case PlacementKind.Candy:
                        AddPickup(p, art.Candy, cell, 1.5f, 0.6f);
                        break;
                    case PlacementKind.Key:
                        AddPickup(p, art.Key, cell, 2f, 1f);
                        break;
                    case PlacementKind.Relic:
                        AddPickup(p, art.Chest, cell, 1.2f, 0f);
                        break;
                    case PlacementKind.Potion:
                        AddPickup(p, art.Potions[p.Variant], cell, p.Variant == (int)PotionKind.Elixir ? 1.3f : 0.9f, 0.4f);
                        break;
                    case PlacementKind.Door:
                        Doors.Add(BuildPassage(p, art.DoorLeaf, "Door"));
                        break;
                    case PlacementKind.Gate:
                        Gates.Add(BuildPassage(p, art.GateLeaf, "Gate"));
                        break;
                    case PlacementKind.Lever:
                        Switches.Add(BuildLever(p, cell));
                        break;
                    case PlacementKind.Plate:
                        Switches.Add(BuildPlate(p, cell));
                        break;
                    case PlacementKind.Teleporter:
                        BuildPad(p, cell);
                        break;
                    case PlacementKind.Spikes:
                        BuildSpikes(cell);
                        break;
                    case PlacementKind.Sign:
                        Place(art.Gravestone, cell + new Vector3(0f, 0f, 1.3f), 180f, Vector3.one * 1.2f);
                        Signs.Add(new SignView { Position = cell + new Vector3(0f, 0f, 1.3f), Text = p.Variant < Definition.Signs.Count ? Definition.Signs[p.Variant] : string.Empty });
                        break;
                    case PlacementKind.Ward:
                        BuildWard(cell);
                        break;
                    case PlacementKind.Artefact:
                        BuildArtefact(cell);
                        break;
                }
            }
            foreach (PlatformTrack track in Map.Tracks)
            {
                BuildPlatform(track);
            }
        }

        void AddPickup(Placement p, GameObject model, Vector3 cell, float scale, float hover)
        {
            var root = new GameObject(p.Kind.ToString());
            root.transform.SetParent(transform, false);
            root.transform.localPosition = cell;
            GameObject body = Place(model, new Vector3(0f, hover, 0f), p.Kind == PlacementKind.Relic ? 180f : 0f, Vector3.one * scale, root.transform);
            if (p.Kind != PlacementKind.Relic)
            {
                var spin = body.AddComponent<Spin>();
                spin.Hover = hover;
            }
            Fx.BlobShadow(root.transform, 0.6f);
            Pickups.Add(new PickupView { Cell = p, Root = root, Position = cell });
        }

        PassageView BuildPassage(Placement p, GameObject leafModel, string name)
        {
            Vector3 cell = World(p.X, p.Y);
            // Walls left and right mean the way runs north-south and the leaves span east-west.
            bool spansX = Map.At(p.X - 1, p.Y) == Tile.Wall || Map.At(p.X + 1, p.Y) == Tile.Wall;
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            root.localPosition = cell;
            root.localRotation = Quaternion.Euler(0f, spansX ? 0f : 90f, 0f);
            var view = new PassageView { Cell = p, Position = cell, Leaves = new Transform[2], ClosedYaw = new float[2], OpenYaw = new float[2] };
            for (int side = 0; side < 2; side++)
            {
                var hinge = new GameObject("Hinge").transform;
                hinge.SetParent(root, false);
                hinge.localPosition = new Vector3(side == 0 ? -2f : 2f, 0f, 0f);
                view.ClosedYaw[side] = side == 0 ? 180f : 0f;
                view.OpenYaw[side] = side == 0 ? 90f : 90f;
                hinge.localRotation = Quaternion.Euler(0f, view.ClosedYaw[side], 0f);
                Place(leafModel, Vector3.zero, 0f, new Vector3(1f, 1.6f, 1f), hinge);
                view.Leaves[side] = hinge;
            }
            view.Blocker = Box(name + " blocker", new Vector3(0f, 2.5f, 0f), new Vector3(Tuning.CellSize, 5f, 0.8f), root);
            return view;
        }

        SwitchView BuildLever(Placement p, Vector3 cell)
        {
            var root = new GameObject("Lever").transform;
            root.SetParent(transform, false);
            root.localPosition = cell;
            Fx.Shape("Base", art.Cube, art.Stone, root, new Vector3(0f, 0.35f, 0f), new Vector3(1.1f, 0.7f, 0.8f));
            var handle = new GameObject("Handle").transform;
            handle.SetParent(root, false);
            handle.localPosition = new Vector3(0f, 0.7f, 0f);
            handle.localRotation = Quaternion.Euler(0f, 0f, 40f);
            Fx.Shape("Shaft", art.Cylinder, art.Iron, handle, new Vector3(0f, 0.55f, 0f), new Vector3(0.14f, 0.55f, 0.14f));
            GameObject knob = Fx.Shape("Knob", art.Sphere, art.Rune, handle, new Vector3(0f, 1.15f, 0f), Vector3.one * 0.32f);
            GameObject rune = Fx.Shape("Rune", art.Cylinder, art.Rune, root, new Vector3(0f, 0.03f, 0f), new Vector3(2f, 0.02f, 2f));
            box(root);
            return new SwitchView { Cell = p, Handle = handle, Knob = knob.GetComponent<Renderer>(), Rune = rune.GetComponent<Renderer>(), Position = cell };

            void box(Transform parent) => Box("Lever blocker", new Vector3(0f, 0.6f, 0f), new Vector3(1.1f, 1.2f, 0.8f), parent);
        }

        SwitchView BuildPlate(Placement p, Vector3 cell)
        {
            var root = new GameObject("Plate").transform;
            root.SetParent(transform, false);
            root.localPosition = cell;
            Fx.Shape("Rim", art.Cylinder, art.Stone, root, new Vector3(0f, 0.06f, 0f), new Vector3(2.6f, 0.06f, 2.6f));
            GameObject rune = Fx.Shape("Rune", art.Cylinder, art.Rune, root, new Vector3(0f, 0.1f, 0f), new Vector3(2f, 0.06f, 2f));
            return new SwitchView { Cell = p, IsPlate = true, Handle = rune.transform, Rune = rune.GetComponent<Renderer>(), Position = cell };
        }

        void BuildPad(Placement p, Vector3 cell)
        {
            var root = new GameObject("Teleporter").transform;
            root.SetParent(transform, false);
            root.localPosition = cell;
            Place(art.Arch, Vector3.zero, 0f, Vector3.one * 0.95f, root);
            Fx.Shape("Pad", art.Cylinder, art.RuneLit, root, new Vector3(0f, 0.05f, 0f), new Vector3(2.4f, 0.04f, 2.4f));
            Fx.Swirl(root, new Vector3(0f, 0.2f, 0f), Fx.Hex("#b46bff"), 1.1f);
            var light = new GameObject("Glow").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            light.type = LightType.Point;
            light.color = Fx.Hex("#a060ff");
            light.range = 6f;
            light.intensity = 1.6f;
            Pads.Add(new PadView { Cell = p, Position = cell });
        }

        void BuildPlatform(PlatformTrack track)
        {
            var root = new GameObject("Platform").transform;
            root.SetParent(transform, false);
            Vector3 from = World(track.StartX, track.StartY);
            Vector3 to = World(track.EndX, track.EndY);
            root.localPosition = from;
            Place(art.Floor, Vector3.zero, 0f, Vector3.one, root);
            Place(art.Foundation, new Vector3(0f, -0.9f, 0f), 0f, new Vector3(1.75f, 0.45f, 1.75f), root);
            Fx.Shape("Rune", art.Cylinder, art.RuneLit, root, new Vector3(0f, 0.06f, 0f), new Vector3(0.8f, 0.01f, 0.8f));
            BoxCollider collider = Box("Platform collider", new Vector3(0f, -0.5f, 0f), new Vector3(Tuning.CellSize, 1f, Tuning.CellSize), root);
            Platforms.Add(new PlatformView { Root = root, From = from, To = to, Collider = collider });
        }

        void BuildSpikes(Vector3 cell)
        {
            Place(art.Grate, cell, 0f, Vector3.one);
            GameObject blades = Place(art.Spikes, cell + Vector3.down * 2.2f, 0f, new Vector3(0.95f, 1f, 0.95f));
            SpikeTraps.Add(new SpikeView { Blades = blades.transform, Position = cell, Offset = (cell.x * 0.37f + cell.z * 0.11f) % SpikePeriod });
        }

        void BuildExit(Vector3 cell)
        {
            ExitPosition = cell;
            Place(art.Stairs, cell + new Vector3(0f, -4.7f, 1.2f), 180f, new Vector3(0.85f, 1f, 1f));
            Fx.Motes(transform, cell + Vector3.up * 0.2f, Fx.Hex("#9fe8ff"), 1.6f, 14f, true);
            var light = new GameObject("Exit light").AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.transform.localPosition = cell + Vector3.up * 1.2f;
            light.type = LightType.Point;
            light.color = Fx.Hex("#7fd8ff");
            light.range = 7f;
            light.intensity = 1.8f;
        }

        void BuildWard(Vector3 cell)
        {
            var root = new GameObject("Ward").transform;
            root.SetParent(transform, false);
            root.localPosition = cell;
            Place(art.Shrine, Vector3.zero, 180f, Vector3.one * 1.3f, root);
            ParticleSystem flame = Fx.Flame(root, new Vector3(0f, 2.5f, 0f), Fx.Hex("#b14dff"), Fx.Hex("#30004a"), 1.6f);
            var light = new GameObject("Ward light").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = new Vector3(0f, 3f, 0f);
            light.type = LightType.Point;
            light.color = Fx.Hex("#a040ff");
            light.range = 8f;
            light.intensity = 2f;
            Box("Ward blocker", new Vector3(0f, 1f, 0f), new Vector3(1.3f, 2f, 1.3f), root);
            Wards.Add(new WardView { Position = cell, Flame = flame, Light = light });
        }

        void BuildArtefact(Vector3 cell)
        {
            ArtefactPosition = cell;
            var root = new GameObject("Artefact").transform;
            root.SetParent(transform, false);
            root.localPosition = cell;
            Place(art.Foundation, Vector3.zero, 45f, new Vector3(1.1f, 0.6f, 1.1f), root);
            var core = new GameObject("Core").transform;
            core.SetParent(root, false);
            core.localPosition = new Vector3(0f, 2.6f, 0f);
            Place(art.ArtefactModel, new Vector3(0f, -1.2f, 0f), 0f, Vector3.one * 3.2f, core);
            core.gameObject.AddComponent<Spin>().Hover = 2.6f;
            ArtefactCore = core;
            ArtefactShield = Fx.Shape("Shield", art.Sphere, art.ShieldBubble, root, new Vector3(0f, 2.4f, 0f), Vector3.one * 4.6f);
            Fx.Swirl(root, new Vector3(0f, 0.4f, 0f), Fx.Hex("#5a1a7a"), 2.6f, false);
            Fx.Swirl(root, new Vector3(0f, 0.4f, 0f), Fx.Hex("#d23cff"), 2f);
            var light = new GameObject("Artefact light").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = new Vector3(0f, 3f, 0f);
            light.type = LightType.Point;
            light.color = Fx.Hex("#ff2a5a");
            light.range = 12f;
            light.intensity = 2.4f;
            var blocker = new GameObject("Artefact blocker");
            blocker.transform.SetParent(root, false);
            var capsule = blocker.AddComponent<CapsuleCollider>();
            capsule.radius = 1.4f;
            capsule.height = 5f;
            capsule.center = new Vector3(0f, 2.5f, 0f);
        }

        // ---- Props ----

        void BuildProps()
        {
            if (art.Props == null || art.Props.Length == 0)
            {
                return;
            }
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { -1, 1, 0, 0 };
            for (int y = 0; y < Map.Height; y++)
            {
                for (int x = 0; x < Map.Width; x++)
                {
                    if (!Map.IsWalkable(x, y) || OccupiedOrNear(x, y) || Hash(x, y) % 5 != 0)
                    {
                        continue;
                    }
                    for (int d = 0; d < 4; d++)
                    {
                        // Only against a north, east or west wall, never on a corridor's only lane.
                        if (dy[d] == 1 || Map.At(x + dx[d], y + dy[d]) != Tile.Wall || Map.At(x - dx[d], y - dy[d]) == Tile.Wall)
                        {
                            continue;
                        }
                        Vector3 toward = new Vector3(dx[d], 0f, -dy[d]);
                        Vector3 side = Vector3.Cross(Vector3.up, toward) * ((Hash(y, x) % 3 - 1) * 1.1f);
                        GameObject prop = art.Props[Hash(x * 7, y * 13) % art.Props.Length];
                        Place(prop, World(x, y) + toward * 1.3f + side, Hash(x, y * 3) % 360, Vector3.one);
                        break;
                    }
                }
            }
        }

        bool OccupiedOrNear(int x, int y)
        {
            foreach (Placement p in Map.Placements)
            {
                if (Mathf.Abs(p.X - x) <= (p.Kind == PlacementKind.Platform ? 0 : 1) && Mathf.Abs(p.Y - y) <= (p.Kind == PlacementKind.Platform ? 0 : 1) &&
                    (p.Kind != PlacementKind.Coin && p.Kind != PlacementKind.Candy || p.X == x && p.Y == y))
                {
                    return true;
                }
            }
            return false;
        }

        // ---- Per-frame animation of the moving pieces ----

        public void Animate(float dt, float time)
        {
            foreach (PlatformView platform in Platforms)
            {
                float length = Vector3.Distance(platform.From, platform.To);
                float travel = length / PlatformSpeed;
                float cycle = 2f * (travel + PlatformPause);
                platform.Phase = (platform.Phase + dt) % cycle;
                float t = platform.Phase;
                float along;
                if (t < PlatformPause) along = 0f;
                else if (t < PlatformPause + travel) along = Mathf.SmoothStep(0f, 1f, (t - PlatformPause) / travel);
                else if (t < 2f * PlatformPause + travel) along = 1f;
                else along = 1f - Mathf.SmoothStep(0f, 1f, (t - 2f * PlatformPause - travel) / travel);
                Vector3 before = platform.Root.localPosition;
                platform.Root.localPosition = Vector3.Lerp(platform.From, platform.To, along);
                platform.Delta = platform.Root.localPosition - before;
            }
            foreach (SpikeView trap in SpikeTraps)
            {
                float t = (time + trap.Offset) % SpikePeriod;
                // Down, a warning rattle, then up for a second.
                float raise = t < 1.6f ? 0f : t < 1.75f ? (t - 1.6f) / 0.15f : t < 2.6f ? 1f : t < 2.8f ? 1f - (t - 2.6f) / 0.2f : 0f;
                float rattle = t > 1.2f && t < 1.6f ? Mathf.Sin(time * 60f) * 0.05f : 0f;
                trap.Raised = raise > 0.6f;
                trap.Blades.localPosition = trap.Position + Vector3.up * (Mathf.Lerp(-2.2f, -0.85f, raise) + rattle);
            }
            foreach (PassageView passage in Doors)
            {
                AnimatePassage(passage, dt);
            }
            foreach (PassageView passage in Gates)
            {
                AnimatePassage(passage, dt);
            }
            if (ArtefactCore != null)
            {
                ArtefactCore.localRotation = Quaternion.Euler(0f, time * 40f, 0f);
            }
        }

        static void AnimatePassage(PassageView passage, float dt)
        {
            float target = passage.Open ? 1f : 0f;
            if (Mathf.Approximately(passage.Progress, target))
            {
                return;
            }
            passage.Progress = Mathf.MoveTowards(passage.Progress, target, dt * 1.4f);
            float eased = Mathf.SmoothStep(0f, 1f, passage.Progress);
            for (int side = 0; side < passage.Leaves.Length; side++)
            {
                float yaw = Mathf.LerpAngle(passage.ClosedYaw[side], passage.ClosedYaw[side] + (side == 0 ? -100f : 100f), eased);
                passage.Leaves[side].localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        public void SetSwitch(SwitchView view, bool active)
        {
            view.Active = active;
            view.Rune.sharedMaterial = active ? art.RuneLit : art.Rune;
            if (view.Knob != null)
            {
                view.Knob.sharedMaterial = active ? art.RuneLit : art.Rune;
            }
            if (view.IsPlate)
            {
                view.Handle.localPosition = new Vector3(0f, active ? 0.05f : 0.1f, 0f);
            }
            else
            {
                view.Handle.localRotation = Quaternion.Euler(0f, 0f, active ? -40f : 40f);
            }
        }

        public void OpenPassage(PassageView passage)
        {
            passage.Open = true;
            passage.Blocker.enabled = false;
        }

        public void PurifyWard(WardView ward)
        {
            ward.Purified = true;
            ParticleSystem.MainModule main = ward.Flame.main;
            main.startColor = Fx.Hex("#7ff3ff");
            ParticleSystem.ColorOverLifetimeModule colour = ward.Flame.colorOverLifetime;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Fx.Hex("#c8fbff"), 0f), new GradientColorKey(Fx.Hex("#2a9fd0"), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;
            ward.Light.color = Fx.Hex("#7fe3ff");
        }

        /// <summary>The platform whose deck is under a point, if any.</summary>
        public PlatformView PlatformUnder(Vector3 position)
        {
            foreach (PlatformView platform in Platforms)
            {
                Vector3 p = platform.Root.localPosition;
                if (Mathf.Abs(position.x - p.x) <= 2.1f && Mathf.Abs(position.z - p.z) <= 2.1f && position.y > -0.6f && position.y < 0.6f)
                {
                    return platform;
                }
            }
            return null;
        }

        public Tile TileAt(Vector3 position)
        {
            LevelMap.CellOf(Flat(position), out int x, out int y);
            return Map.At(x, y);
        }
    }

    /// <summary>Spins and bobs a pickup.</summary>
    public sealed class Spin : MonoBehaviour
    {
        public float Hover;
        public float Speed = 90f;

        void Update()
        {
            float t = Time.time;
            transform.localRotation = Quaternion.Euler(0f, t * Speed, 0f);
            Vector3 p = transform.localPosition;
            transform.localPosition = new Vector3(p.x, Hover + Mathf.Sin(t * 2.2f + p.x) * 0.12f, p.z);
        }
    }

    /// <summary>Gives a torch light a living flicker.</summary>
    public sealed class Flicker : MonoBehaviour
    {
        Light lit;
        float baseIntensity;
        float seed;

        void Start()
        {
            lit = GetComponent<Light>();
            baseIntensity = lit.intensity;
            seed = Random.value * 10f;
        }

        void Update()
        {
            float t = Time.time * 7f + seed;
            lit.intensity = baseIntensity * (0.85f + 0.15f * Mathf.PerlinNoise(t, seed));
        }
    }

    /// <summary>Scrolls the lava texture.</summary>
    public sealed class LavaFlow : MonoBehaviour
    {
        Material material;

        void Start() => material = GetComponent<Renderer>().material;

        void Update()
        {
            float t = Time.time;
            material.mainTextureOffset = new Vector2(t * 0.02f, Mathf.Sin(t * 0.3f) * 0.05f);
            material.SetTextureOffset("_EmissionMap", new Vector2(-t * 0.015f, t * 0.01f));
        }
    }
}
