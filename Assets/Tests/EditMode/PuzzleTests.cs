using System.Linq;
using NUnit.Framework;

namespace SpookyPath.Core.Tests
{
    public class PuzzleTests
    {
        [Test]
        public void A_gate_opens_only_when_every_switch_on_its_channel_is_active()
        {
            var board = new PuzzleBoard(new[] { 0, 0, 1 }, new[] { 0, 1 }, 0);

            Assert.IsEmpty(board.Activate(0));
            Assert.IsFalse(board.IsGateOpen(0));
            Assert.AreEqual(1, board.Remaining(0));

            CollectionAssert.AreEqual(new[] { 0 }, board.Activate(1).ToArray());
            Assert.IsTrue(board.IsGateOpen(0));
            Assert.IsFalse(board.IsGateOpen(1), "another channel stays shut");
        }

        [Test]
        public void Switches_latch_and_activating_one_twice_changes_nothing()
        {
            var board = new PuzzleBoard(new[] { 0 }, new[] { 0 }, 0);
            board.Activate(0);

            Assert.IsTrue(board.IsActive(0));
            Assert.IsEmpty(board.Activate(0));
            Assert.IsTrue(board.IsGateOpen(0));
        }

        [Test]
        public void One_switch_can_open_several_gates_on_its_channel()
        {
            var board = new PuzzleBoard(new[] { 2 }, new[] { 2, 0, 2 }, 0);

            CollectionAssert.AreEqual(new[] { 0, 2 }, board.Activate(0).ToArray());
            Assert.IsFalse(board.IsGateOpen(1));
        }

        [Test]
        public void A_key_opens_one_locked_door_and_is_used_up()
        {
            var board = new PuzzleBoard(null, null, 2);

            Assert.IsFalse(board.TryUnlock(0), "no key yet");
            board.PickUpKey();
            Assert.AreEqual(1, board.Keys);
            Assert.IsTrue(board.TryUnlock(0));
            Assert.IsTrue(board.IsDoorUnlocked(0));
            Assert.AreEqual(0, board.Keys);
            Assert.IsFalse(board.TryUnlock(1));
            Assert.IsFalse(board.IsDoorUnlocked(1));
        }

        [Test]
        public void An_open_door_does_not_take_another_key()
        {
            var board = new PuzzleBoard(null, null, 1);
            board.PickUpKey();
            board.PickUpKey();
            board.TryUnlock(0);

            Assert.IsFalse(board.TryUnlock(0));
            Assert.AreEqual(1, board.Keys);
        }

        [Test]
        public void The_board_for_a_map_follows_its_switches_gates_and_doors()
        {
            LevelMap map = LevelMap.Parse(new[] { "PoloG", "kDE.G" }, new[] { 0, 1, 0 }, new[] { 0, 1 });
            PuzzleBoard board = PuzzleBoard.For(map);

            Assert.AreEqual(3, board.SwitchCount);
            Assert.AreEqual(2, board.GateCount);
            board.Activate(0);
            CollectionAssert.AreEqual(new[] { 0 }, board.Activate(2).ToArray());
            CollectionAssert.AreEqual(new[] { 1 }, board.Activate(1).ToArray());
            board.PickUpKey();
            Assert.IsTrue(board.TryUnlock(0));
        }

        [Test]
        public void The_artefact_cannot_be_harmed_while_a_ward_burns()
        {
            var artefact = new Artefact(2);

            Assert.IsTrue(artefact.Shielded);
            Assert.AreEqual(0f, artefact.TakeDamage(100f));
            Assert.AreEqual(Artefact.MaxHealth, artefact.Health);

            Assert.IsFalse(artefact.PurifyWard());
            Assert.IsTrue(artefact.Shielded);
            Assert.IsTrue(artefact.PurifyWard(), "the last ward drops the shield");
            Assert.IsFalse(artefact.Shielded);
            Assert.IsFalse(artefact.PurifyWard());
        }

        [Test]
        public void Once_exposed_the_artefact_takes_damage_until_it_shatters()
        {
            var artefact = new Artefact(1);
            artefact.PurifyWard();

            Assert.AreEqual(150f, artefact.TakeDamage(150f));
            Assert.AreEqual(Artefact.MaxHealth - 150f, artefact.Health);
            Assert.AreEqual(Artefact.MaxHealth - 150f, artefact.TakeDamage(1000f));
            Assert.IsTrue(artefact.Destroyed);
            Assert.AreEqual(0f, artefact.TakeDamage(10f));
        }

        [Test]
        public void The_artefact_summons_on_a_clock_that_speeds_up_once_exposed()
        {
            var artefact = new Artefact(1);

            Assert.AreEqual(0, artefact.Tick(Artefact.ShieldedSummonInterval - 0.1f, 0));
            Assert.AreEqual(1, artefact.Tick(0.2f, 0));

            artefact.PurifyWard();
            Assert.AreEqual(1, artefact.Tick(Artefact.ExposedSummonInterval, 0));
            Assert.AreEqual(0, artefact.Tick(Artefact.ExposedSummonInterval - 0.5f, 0));
            Assert.AreEqual(1, artefact.Tick(1f, 0));
        }

        [Test]
        public void The_artefact_never_raises_more_than_its_minion_cap_or_after_shattering()
        {
            var artefact = new Artefact(1);

            Assert.AreEqual(0, artefact.Tick(Artefact.ShieldedSummonInterval + 0.1f, Artefact.MaxMinions));

            artefact.PurifyWard();
            artefact.TakeDamage(Artefact.MaxHealth);
            Assert.AreEqual(0, artefact.Tick(100f, 0));
        }
    }
}
