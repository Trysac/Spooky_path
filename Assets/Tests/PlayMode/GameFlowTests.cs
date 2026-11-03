using System.Collections;
using NUnit.Framework;
using SpookyPath.Core;
using SpookyPath.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpookyPath.Tests
{
    /// <summary>Scene flow in the real game scene: menu, levels, pause, death, completion, victory and saving.</summary>
    public class GameFlowTests
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
        public IEnumerator The_game_opens_on_the_title_menu()
        {
            Assert.AreEqual(Phase.Title, director.Phase);
            Assert.AreEqual(HudScreen.Title, director.Hud.Screen);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Starting_a_level_builds_it_and_places_the_explorer_on_the_start()
        {
            director.StartLevel(0);
            yield return null;

            Assert.AreEqual(Phase.Playing, director.Phase);
            Assert.AreEqual(HudScreen.Playing, director.Hud.Screen);
            Assert.AreEqual(0, director.LevelIndex);
            Assert.Less(Vector3.Distance(director.Player.transform.position, director.Level.StartPosition), 1f);
            Assert.AreEqual(1, director.Enemies.Count, "the tutorial has one skeleton");
            Assert.AreEqual(director.Level.Map.All(PlacementKind.Coin).Count + director.Level.Map.All(PlacementKind.Candy).Count +
                            director.Level.Map.All(PlacementKind.Potion).Count + director.Level.Map.All(PlacementKind.Key).Count,
                director.Level.Pickups.Count);
        }

        [UnityTest]
        public IEnumerator The_explorer_walks_where_the_commands_point()
        {
            director.StartLevel(0);
            yield return new WaitForSeconds(0.3f);
            Vector3 before = director.Player.transform.position;

            director.Scripted.Move = new Vector2(0f, 1f);
            yield return new WaitForSeconds(0.6f);
            director.Scripted.Move = Vector2.zero;

            Assert.Greater(director.Player.transform.position.z - before.z, 1.5f);
        }

        [UnityTest]
        public IEnumerator Pausing_freezes_time_and_resuming_continues()
        {
            director.StartLevel(0);
            yield return null;

            director.PauseGame();
            Assert.AreEqual(Phase.Paused, director.Phase);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(HudScreen.Paused, director.Hud.Screen);
            float elapsed = director.Run.Elapsed;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(elapsed, director.Run.Elapsed, "the level clock stops");

            director.Resume();
            Assert.AreEqual(Phase.Playing, director.Phase);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Dying_shows_the_game_over_results_and_retry_restarts_the_level()
        {
            director.StartLevel(1);
            yield return null;

            director.KillPlayerNow("Test death.");
            yield return null;
            Assert.AreEqual(Phase.GameOver, director.Phase);
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(HudScreen.Results, director.Hud.Screen);

            director.RestartLevel();
            yield return null;
            Assert.AreEqual(Phase.Playing, director.Phase);
            Assert.AreEqual(1, director.LevelIndex);
            Assert.IsFalse(director.Player.IsDead);
        }

        [UnityTest]
        public IEnumerator Falling_into_lava_kills_the_explorer()
        {
            director.StartLevel(2);
            yield return null;
            director.PlacePlayer(LevelView.World(1, 5, 1f), Vector3.forward);

            yield return new WaitForSeconds(1.5f);

            Assert.IsTrue(director.Player.IsDead);
            Assert.AreEqual(Phase.GameOver, director.Phase);
        }

        [UnityTest]
        public IEnumerator Reaching_the_stairs_completes_the_level_saves_the_time_and_unlocks_the_next()
        {
            director.StartLevel(0);
            yield return new WaitForSeconds(0.5f);

            director.FinishLevelNow();
            yield return null;

            Assert.AreEqual(Phase.LevelComplete, director.Phase);
            Assert.Greater(director.Save.BestTime(0), 0f);
            Assert.IsTrue(director.Save.IsUnlocked(1));
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(HudScreen.Results, director.Hud.Screen);

            director.NextLevel();
            yield return null;
            Assert.AreEqual(1, director.LevelIndex);
            Assert.AreEqual(Phase.Playing, director.Phase);
        }

        [UnityTest]
        public IEnumerator Pulling_the_lever_opens_the_tutorial_gate()
        {
            director.StartLevel(0);
            yield return null;
            SwitchView lever = director.Level.Switches[0];
            director.PlacePlayer(lever.Position + new Vector3(1.2f, 0.1f, 0f), Vector3.left);
            yield return new WaitForSeconds(0.2f);

            director.Scripted.Interact = true;
            yield return null;
            yield return null;

            Assert.IsTrue(lever.Active);
            Assert.IsTrue(director.Puzzle.IsGateOpen(0));
            Assert.IsTrue(director.Level.Gates[0].Open);
        }

        [UnityTest]
        public IEnumerator The_artefact_stays_warded_until_every_brazier_is_purified_then_shatters_into_victory()
        {
            director.StartLevel(Levels.Last);
            yield return null;
            Assert.IsTrue(director.ArtefactState.Shielded);

            director.PurifyAllWardsNow();
            Assert.IsFalse(director.ArtefactState.Shielded);
            director.FinishLevelNow();
            yield return null;

            Assert.IsTrue(director.ArtefactState.Destroyed);
            Assert.AreEqual(Phase.Victory, director.Phase);
            Assert.IsTrue(director.Save.GameCompleted);
            Assert.Greater(director.Run.Score, Tuning.ArtefactScore - 1);
        }

        [UnityTest]
        public IEnumerator Quitting_to_the_title_returns_to_the_menu()
        {
            director.StartLevel(0);
            yield return null;
            director.PauseGame();

            director.ToTitle();
            yield return null;

            Assert.AreEqual(Phase.Title, director.Phase);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(HudScreen.Title, director.Hud.Screen);
        }
    }
}
