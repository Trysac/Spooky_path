using System.Collections;
using System.Linq;
using NUnit.Framework;
using SpookyPath.Core;
using SpookyPath.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpookyPath.Tests
{
    /// <summary>The interactive pieces of the levels working in the real scene.</summary>
    public class WorldTests
    {
        GameDirector director;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            SceneManager.LoadScene("Game");
            yield return null;
            director = Object.FindAnyObjectByType<GameDirector>();
            director.UseMemoryStore();
            director.ScriptedInput = true;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pressing_both_plates_opens_the_hall_gate()
        {
            director.StartLevel(1);
            director.Invincible = true;
            yield return null;
            SwitchView[] plates = director.Level.Switches.Where(s => s.IsPlate).ToArray();
            Assert.AreEqual(2, plates.Length);

            director.PlacePlayer(plates[0].Position + Vector3.up * 0.2f, Vector3.forward);
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(plates[0].Active);
            Assert.IsFalse(director.Level.Gates[0].Open, "one plate is not enough");

            director.PlacePlayer(plates[1].Position + Vector3.up * 0.2f, Vector3.forward);
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(director.Level.Gates[0].Open);
        }

        [UnityTest]
        public IEnumerator A_key_unlocks_the_treasury_door_and_is_spent()
        {
            director.StartLevel(1);
            director.Invincible = true;
            yield return null;
            PickupView key = director.Level.Pickups.First(p => p.Cell.Kind == PlacementKind.Key);

            director.PlacePlayer(key.Position + Vector3.up * 0.2f, Vector3.forward);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, director.Puzzle.Keys);
            Assert.IsTrue(key.Taken);

            PassageView door = director.Level.Doors[0];
            director.PlacePlayer(door.Position + new Vector3(-2.5f, 0.2f, 0f), Vector3.right);
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(door.Open);
            Assert.AreEqual(0, director.Puzzle.Keys);
        }

        [UnityTest]
        public IEnumerator A_teleport_stone_carries_the_explorer_to_its_twin()
        {
            director.StartLevel(2);
            director.Invincible = true;
            yield return null;
            PadView from = director.Level.Pads[1];
            PadView to = director.Level.Pads[0];

            director.PlacePlayer(from.Position + Vector3.up * 0.2f, Vector3.forward);
            yield return new WaitForSeconds(0.3f);

            Vector3 here = director.Player.transform.position;
            Assert.Less(new Vector2(here.x - to.Position.x, here.z - to.Position.z).magnitude, 1.5f);
            yield return new WaitForSeconds(0.3f);
            here = director.Player.transform.position;
            Assert.Less(new Vector2(here.x - to.Position.x, here.z - to.Position.z).magnitude, 1.5f, "arriving does not bounce straight back");
        }

        [UnityTest]
        public IEnumerator A_moving_platform_carries_the_explorer_over_the_lava()
        {
            director.StartLevel(2);
            director.Invincible = true;
            yield return null;
            PlatformView platform = director.Level.Platforms[0];
            director.PlacePlayer(platform.Root.position + Vector3.up * 0.3f, Vector3.forward);
            Vector3 start = director.Player.transform.position;

            yield return new WaitForSeconds(3.5f);

            Vector3 deck = platform.Root.position;
            Vector3 here = director.Player.transform.position;
            Assert.Greater(Vector3.Distance(start, here), 2f, "the platform moved the explorer");
            Assert.Less(new Vector2(here.x - deck.x, here.z - deck.z).magnitude, 2.2f, "still standing on the deck");
            Assert.IsFalse(director.Player.IsDead);
        }

        [UnityTest]
        public IEnumerator Staff_strikes_destroy_a_skeleton_and_count_the_kill()
        {
            director.StartLevel(0);
            director.Invincible = true;
            yield return null;
            EnemyController skeleton = director.Enemies[0];
            director.PlacePlayer(skeleton.transform.position + new Vector3(0f, 0.2f, -1.6f), Vector3.forward);

            for (int i = 0; i < 6 && !skeleton.Brain.IsDead; i++)
            {
                director.Scripted.Strike = true;
                yield return new WaitForSeconds(Tuning.StrikeCooldown + 0.05f);
            }

            Assert.IsTrue(skeleton.Brain.IsDead);
            Assert.AreEqual(1, director.Run.Kills);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, director.Enemies.Count);
        }

        [UnityTest]
        public IEnumerator A_skeleton_that_sees_the_explorer_attacks_and_hurts()
        {
            director.StartLevel(0);
            yield return null;
            EnemyController skeleton = director.Enemies[0];
            director.PlacePlayer(skeleton.transform.position + new Vector3(0f, 0.2f, -3f), Vector3.forward);

            yield return new WaitForSeconds(2.5f);

            Assert.Less(director.Player.Vitals.Health, director.Player.Vitals.MaxHealth);
        }

        [UnityTest]
        public IEnumerator Casting_a_firebolt_spends_mana()
        {
            director.StartLevel(0);
            yield return new WaitForSeconds(0.2f);

            director.Scripted.Spell = (int)SpellKind.Firebolt;
            yield return null;
            yield return null;

            Assert.Less(director.Player.Vitals.Mana, director.Player.Vitals.MaxMana);
        }
    }
}
