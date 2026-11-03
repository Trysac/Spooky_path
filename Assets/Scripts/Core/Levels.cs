using System.Collections.Generic;

namespace SpookyPath.Core
{
    /// <summary>One level: its map, story line, par time and the texts of its signs.</summary>
    public sealed class LevelDefinition
    {
        public LevelDefinition(string name, string subtitle, float parSeconds, string[] map, string[] signs,
            int[] switchChannels = null, int[] gateChannels = null)
        {
            Name = name;
            Subtitle = subtitle;
            ParSeconds = parSeconds;
            Map = map;
            Signs = signs;
            SwitchChannels = switchChannels;
            GateChannels = gateChannels;
        }

        public string Name { get; }
        public string Subtitle { get; }
        public float ParSeconds { get; }
        public IReadOnlyList<string> Map { get; }
        public IReadOnlyList<string> Signs { get; }
        public IReadOnlyList<int> SwitchChannels { get; }
        public IReadOnlyList<int> GateChannels { get; }

        public LevelMap Parse() => LevelMap.Parse(Map, SwitchChannels, GateChannels);
    }

    /// <summary>
    /// The descent: a tutorial and three levels, from the crypt gate down to the artefact. The
    /// layouts follow the 2023 build (a dungeon of narrow paths over lava, moving platforms,
    /// teleporters and skeleton guards) redrawn as text maps.
    /// </summary>
    public static class Levels
    {
        public static readonly IReadOnlyList<LevelDefinition> All = new[]
        {
            new LevelDefinition(
                "The Crypt Gate",
                "Tutorial: learn to move, fight and solve the dungeon's locks.",
                90f,
                new[]
                {
                    "#################",
                    "######..E..######",
                    "######.....######",
                    "######..i..######",
                    "########G########",
                    "###...........###",
                    "###.l.....i...###",
                    "###.....1...m.###",
                    "###.c.......c.###",
                    "########D########",
                    "######.....######",
                    "######.k.i.######",
                    "######..y..######",
                    "######.....######",
                    "######     ######",
                    "######.....######",
                    "######.i.h.######",
                    "######..P..######",
                    "#################",
                },
                new[]
                {
                    "Stairs lead deeper into the dungeon. Step onto them to descend.",
                    "Strike with Left Click / X. Cast spells with 1-4 / the shoulder buttons. Pull a lever with E / Y.",
                    "Keys open locked doors: walk into the door while carrying one.",
                    "Move with WASD / the left stick. Hold Shift / B to sprint. Space / A jumps - press it again in the air to double jump.",
                }),
            new LevelDefinition(
                "Halls of Bone",
                "Skeletons patrol the old halls. Press both plates to open the way down.",
                180f,
                new[]
                {
                    "###########################",
                    "###########..E..###########",
                    "###########.....###########",
                    "###########..i..###########",
                    "#############G#############",
                    "#####.................#####",
                    "#####.o.....5.......o.#####",
                    "#####.....y.....y.....#####",
                    "#####.c.c.........c.c.#####",
                    "#####......2...2......#####",
                    "#####.................#####",
                    "###########.....###########",
                    "#.....#####.....#####..r..#",
                    "#..k.......4........D.....#",
                    "#..1..#####.....#####..x..#",
                    "#.....#####.c.c.#####.....#",
                    "#######~~~~.....~~~~#######",
                    "#######~~~~.~~~.~~~~#######",
                    "#######~~~~.~~~.~~~~#######",
                    "#######~~~~.~y~.~~~~#######",
                    "#######~~~~.~~~.~~~~#######",
                    "###########..i..###########",
                    "###########.....###########",
                    "###########..P..###########",
                    "###########################",
                },
                new[]
                {
                    "The gate opens once every pressure plate in the hall has been pressed.",
                    "Bone sentries patrol ahead. Lure them out one at a time - and look for the treasury key.",
                }),
            new LevelDefinition(
                "The Ember Path",
                "A narrow path over a lake of lava. Ride the platforms and trust the teleport stones.",
                180f,
                new[]
                {
                    "#############################",
                    "##########.E.################",
                    "##########...################",
                    "##########.3.################",
                    "~~~~~~~~~~~.~~~~~~~~~~~~~~~~~",
                    "~~~~~~~~~~~.~~~~~~~~~~~~~~~~~",
                    "~~~.....T..^...~~~~~~~~~~~~~~",
                    "~~~.c.......y..~~~~~~~~~~~~~~",
                    "~~~.....3...h..~~~~~~~~~~~~~~",
                    "~~~~~~.~~~~~~~~~~~~~~~~~~~~~~",
                    "~~~~~~=~~~~~~~~~~~~~~~~~~~~~~",
                    "~~~~~~=~~~~~~~~~~~~~.....~~~~",
                    "~~~~~~=~~~~~~~~~~~~~..T..~~~~",
                    "~~~~~~.~~~~~~~~~~~~~.c.c.~~~~",
                    "~~~...4....~~~~~~~~~.....~~~~",
                    "~~~.c.....m~~~~~~~~~~~^~~~~~~",
                    "~~~.......====.c.====.^.c~~~~",
                    "~~~~~^~~~~~~~~~~~~~~~~^~~~~~~",
                    "~~~~~.~~~~~~~~~~~~~~~...~~~~~",
                    "~~~~~.~~~~~~~~~~~~~~~.3.~~~~~",
                    "~~~~~.~~~~~~~~~~~~~~~.y.~~~~~",
                    "~~~~...~~~~~~~~~~~~~~...~~~~~",
                    "~~~~.i.~~~~~~~~~~~~~~~.~~~~~~",
                    "~~~~.P.~~~~~~~~~~~~~~~.~~~~~~",
                    "~~~~...................~~~~~~",
                    "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                },
                new[]
                {
                    "Ride the moving platforms across the lava. Teleport stones carry you far. Mind the spike traps!",
                }),
            new LevelDefinition(
                "Heart of Darkness",
                "The artefact that feeds the darkness waits below. Destroy it.",
                240f,
                new[]
                {
                    "#############################",
                    "#############...#############",
                    "#############.A.#############",
                    "#############...#############",
                    "#########...........#########",
                    "#########.w.....1...#########",
                    "#########...........#########",
                    "#########...........#########",
                    "#########~~~.....~~~#########",
                    "####.....~~~.....~~~.....####",
                    "####.w...~~~..h..~~~..w2.####",
                    "####..2..~~~.....~~~..m..####",
                    "####.....===.....===.....####",
                    "####.....~~~.....~~~.....####",
                    "#########~~~.....~~~#########",
                    "#############...#############",
                    "#############.i.#############",
                    "#############.P.#############",
                    "#############################",
                },
                new[]
                {
                    "The artefact is warded. Purify all three dark braziers (E / Y), then shatter it.",
                }),
        };

        public static int Count => All.Count;
        public static int Last => All.Count - 1;
    }
}
