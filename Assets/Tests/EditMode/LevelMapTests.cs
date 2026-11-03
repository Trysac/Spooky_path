using System.Linq;
using NUnit.Framework;

namespace SpookyPath.Core.Tests
{
    public class LevelMapTests
    {
        static LevelMap Parse(params string[] rows) => LevelMap.Parse(rows);

        [Test]
        public void Characters_become_tiles_and_placements()
        {
            LevelMap map = Parse(
                "#####",
                "#E~ #",
                "#P1h#",
                "#####");

            Assert.IsTrue(map.IsValid, string.Join("; ", map.Errors));
            Assert.AreEqual(5, map.Width);
            Assert.AreEqual(4, map.Height);
            Assert.AreEqual(Tile.Wall, map.At(0, 0));
            Assert.AreEqual(Tile.Lava, map.At(2, 1));
            Assert.AreEqual(Tile.Void, map.At(3, 1));
            Assert.AreEqual(Tile.Floor, map.At(1, 2));
            Assert.AreEqual(1, map.Start.X);
            Assert.AreEqual(2, map.Start.Y);
            Placement enemy = map.All(PlacementKind.Enemy).Single();
            Assert.AreEqual((int)EnemyKind.Minion, enemy.Variant);
            Assert.IsFalse(enemy.Patrols);
            Assert.AreEqual((int)PotionKind.Health, map.All(PlacementKind.Potion).Single().Variant);
        }

        [Test]
        public void Outside_the_map_and_short_rows_are_pits()
        {
            LevelMap map = Parse("PE..", "..");

            Assert.AreEqual(Tile.Void, map.At(3, 1));
            Assert.AreEqual(Tile.Void, map.At(-1, 0));
            Assert.AreEqual(Tile.Void, map.At(0, 9));
        }

        [Test]
        public void A_level_needs_exactly_one_start()
        {
            Assert.IsFalse(Parse("..E").IsValid);
            LevelMap twice = Parse("PPE");
            Assert.IsFalse(twice.IsValid);
            StringAssert.Contains("exactly one start", twice.Errors[0]);
        }

        [Test]
        public void A_level_needs_an_exit_or_the_artefact_with_its_wards()
        {
            Assert.IsFalse(Parse("P..").IsValid);
            Assert.IsFalse(Parse("P.A").IsValid, "the artefact needs wards");
            Assert.IsTrue(Parse("PwA").IsValid);
        }

        [Test]
        public void Unknown_characters_are_reported()
        {
            LevelMap map = Parse("P?E");

            Assert.IsFalse(map.IsValid);
            StringAssert.Contains("'?'", map.Errors[0]);
        }

        [Test]
        public void Teleporters_pair_up_in_reading_order()
        {
            LevelMap map = Parse("PT.T", "T..T", "E...");

            Assert.IsTrue(map.IsValid);
            int[] pairs = map.All(PlacementKind.Teleporter).Select(t => t.Variant).ToArray();
            CollectionAssert.AreEqual(new[] { 0, 0, 1, 1 }, pairs);
            Assert.IsFalse(Parse("PTE").IsValid, "an odd teleporter");
        }

        [Test]
        public void Every_locked_door_needs_a_key()
        {
            Assert.IsFalse(Parse("PDE").IsValid);
            Assert.IsTrue(Parse("PkDE").IsValid);
        }

        [Test]
        public void Gates_need_a_lever_or_plate_on_their_channel()
        {
            Assert.IsFalse(Parse("PGE").IsValid);
            Assert.IsTrue(Parse("PlGE").IsValid);
            Assert.IsFalse(LevelMap.Parse(new[] { "PlGE" }, new[] { 1 }, new[] { 0 }).IsValid, "lever on another channel");
            Assert.IsFalse(LevelMap.Parse(new[] { "PlGE" }, new[] { 0, 1 }).IsValid, "channel count mismatch");
        }

        [Test]
        public void Gates_and_switches_take_their_channels_in_reading_order()
        {
            LevelMap map = LevelMap.Parse(new[] { "PloG.", "GE..." }, new[] { 2, 3 }, new[] { 3, 2 });

            Assert.IsTrue(map.IsValid, string.Join("; ", map.Errors));
            Assert.AreEqual(2, map.All(PlacementKind.Lever).Single().Variant);
            Assert.AreEqual(3, map.All(PlacementKind.Plate).Single().Variant);
            CollectionAssert.AreEqual(new[] { 3, 2 }, map.All(PlacementKind.Gate).Select(g => g.Variant).ToArray());
        }

        [Test]
        public void Platform_tracks_are_straight_runs_over_lava()
        {
            LevelMap map = Parse(
                "P===.E",
                ".....=",
                ".....=",
                "......");

            Assert.IsTrue(map.IsValid, string.Join("; ", map.Errors));
            Assert.AreEqual(2, map.Tracks.Count);
            PlatformTrack row = map.Tracks.Single(t => t.StartY == 0);
            Assert.AreEqual(1, row.StartX);
            Assert.AreEqual(3, row.EndX);
            Assert.AreEqual(3, row.Length);
            Assert.AreEqual(Tile.Lava, map.At(2, 0));
        }

        [Test]
        public void Bent_or_single_cell_tracks_are_rejected()
        {
            Assert.IsFalse(Parse("P==E", ".=..").IsValid, "bent");
            Assert.IsFalse(Parse("P.=.E").IsValid, "single cell");
        }

        [Test]
        public void A_patrol_walks_east_up_to_four_cells_or_west_when_east_is_blocked()
        {
            LevelMap map = Parse(
                "P4......E",
                "...4#....",
                "#.......#");

            Placement[] patrols = map.All(PlacementKind.Enemy).ToArray();
            Assert.IsTrue(patrols[0].Patrols);
            Assert.AreEqual(1 + LevelMap.PatrolReach, map.PatrolEndX(patrols[0]));
            Assert.AreEqual(0, map.PatrolEndX(patrols[1]));
        }

        [Test]
        public void Cells_and_world_positions_round_trip()
        {
            Vec2 centre = LevelMap.CellCenter(3, 5);
            Assert.AreEqual(new Vec2(3f * Tuning.CellSize, -5f * Tuning.CellSize), centre);

            LevelMap.CellOf(centre + new Vec2(1.9f, -1.9f), out int x, out int y);
            Assert.AreEqual(3, x);
            Assert.AreEqual(5, y);
        }

        [Test]
        public void Reachability_walks_jumps_one_cell_gaps_rides_tracks_and_teleports()
        {
            LevelMap map = Parse(
                "P. .~.==.T",
                "#########.",
                "T..  .E...");

            bool[,] reached = Reachability.From(map, map.Start.X, map.Start.Y);

            Assert.IsTrue(reached[3, 0], "jumped the pit");
            Assert.IsTrue(reached[5, 0], "jumped the lava");
            Assert.IsTrue(reached[8, 0], "rode the platform");
            Assert.IsTrue(reached[0, 2], "teleported");
            Assert.IsTrue(reached[6, 2], "walked round");
            Assert.IsFalse(reached[1, 1], "walls are solid");
        }

        [Test]
        public void Every_shipped_level_is_valid_and_can_be_finished()
        {
            Assert.AreEqual(4, Levels.Count);
            foreach (LevelDefinition level in Levels.All)
            {
                LevelMap map = level.Parse();
                Assert.IsTrue(map.IsValid, $"{level.Name}: {string.Join("; ", map.Errors)}");
                Assert.AreEqual(level.Signs.Count, map.All(PlacementKind.Sign).Count, $"{level.Name}: one text per sign");
                bool[,] reached = Reachability.From(map, map.Start.X, map.Start.Y);
                foreach (Placement placement in map.Placements)
                {
                    if (placement.Kind != PlacementKind.Platform)
                    {
                        Assert.IsTrue(reached[placement.X, placement.Y], $"{level.Name}: {placement} cannot be reached");
                    }
                }
            }
        }

        [Test]
        public void Only_the_last_level_holds_the_artefact()
        {
            for (int i = 0; i < Levels.Count; i++)
            {
                LevelMap map = Levels.All[i].Parse();
                bool last = i == Levels.Last;
                Assert.AreEqual(last ? 1 : 0, map.All(PlacementKind.Artefact).Count, Levels.All[i].Name);
                Assert.AreEqual(last ? 0 : 1, map.All(PlacementKind.Exit).Count, Levels.All[i].Name);
            }
        }
    }
}
