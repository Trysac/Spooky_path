using NUnit.Framework;

namespace SpookyPath.Core.Tests
{
    public class ProgressTests
    {
        [Test]
        public void The_score_adds_loot_kills_the_artefact_and_seconds_under_par()
        {
            var run = new LevelRun(3, 100f);
            run.AddCoin();
            run.AddCoin();
            run.AddCandy();
            run.AddRelic();
            run.AddKill(EnemyKind.Warrior);
            run.DestroyArtefact();
            run.Tick(60.4f);

            Assert.AreEqual(39, run.SecondsUnderPar);
            int expected = 2 * Tuning.CoinScore + Tuning.CandyScore + Tuning.RelicScore +
                           EnemyStats.For(EnemyKind.Warrior).Score + Tuning.ArtefactScore + 39 * Tuning.ParSecondScore;
            Assert.AreEqual(expected, run.Score);
            Assert.AreEqual(1, run.Kills);
        }

        [Test]
        public void The_loot_score_leaves_out_the_time_bonus_that_shrinks_while_playing()
        {
            var run = new LevelRun(0, 90f);
            run.AddCoin();
            run.Tick(10f);

            Assert.AreEqual(Tuning.CoinScore, run.LootScore);
            Assert.AreEqual(80 * Tuning.ParSecondScore, run.TimeBonus);
            Assert.AreEqual(run.LootScore + run.TimeBonus, run.Score);
        }

        [Test]
        public void Finishing_over_par_earns_no_time_bonus()
        {
            var run = new LevelRun(0, 30f);
            run.Tick(45f);

            Assert.AreEqual(0, run.SecondsUnderPar);
            Assert.AreEqual(0, run.Score);
        }

        [Test]
        public void A_new_save_opens_only_the_first_level()
        {
            var save = new SaveData(4);

            Assert.IsTrue(save.IsUnlocked(0));
            Assert.IsFalse(save.IsUnlocked(1));
            Assert.AreEqual(0f, save.BestTime(0));
            Assert.IsFalse(save.GameCompleted);
        }

        [Test]
        public void Finishing_a_level_keeps_the_best_time_and_score_and_unlocks_the_next()
        {
            var save = new SaveData(4);

            SaveData.Record first = save.RecordFinish(0, 80f, 500);
            Assert.IsTrue(first.NewBestTime);
            Assert.IsTrue(first.NewBestScore);
            Assert.IsTrue(first.UnlockedNext);
            Assert.IsTrue(save.IsUnlocked(1));

            SaveData.Record slower = save.RecordFinish(0, 95f, 700);
            Assert.IsFalse(slower.NewBestTime);
            Assert.IsTrue(slower.NewBestScore);
            Assert.IsFalse(slower.UnlockedNext);
            Assert.AreEqual(80f, save.BestTime(0));
            Assert.AreEqual(700, save.BestScore(0));
            Assert.AreEqual(2, save.UnlockedLevels);
        }

        [Test]
        public void Replaying_an_early_level_never_locks_later_ones()
        {
            var save = new SaveData(4);
            save.RecordFinish(0, 50f, 1);
            save.RecordFinish(1, 50f, 1);
            save.RecordFinish(2, 50f, 1);

            save.RecordFinish(0, 40f, 1);

            Assert.AreEqual(4, save.UnlockedLevels);
        }

        [Test]
        public void Finishing_the_last_level_completes_the_game_and_sums_the_best_times()
        {
            var save = new SaveData(2);
            Assert.AreEqual(0f, save.BestTotalTime, "no total until every level is done");
            save.RecordFinish(0, 30f, 1);
            save.RecordFinish(1, 45.5f, 1);

            Assert.IsTrue(save.GameCompleted);
            Assert.AreEqual(75.5f, save.BestTotalTime, 0.001f);
            Assert.AreEqual(2, save.UnlockedLevels);
        }

        [Test]
        public void The_save_survives_a_round_trip()
        {
            var save = new SaveData(4);
            save.RecordFinish(0, 61.25f, 900);
            save.RecordFinish(1, 120.5f, 1500);
            save.SetVolumes(0.25f, 0.5f);

            SaveData loaded = SaveData.Parse(save.Serialize(), 4);

            Assert.AreEqual(3, loaded.UnlockedLevels);
            Assert.AreEqual(61.25f, loaded.BestTime(0), 0.001f);
            Assert.AreEqual(120.5f, loaded.BestTime(1), 0.001f);
            Assert.AreEqual(1500, loaded.BestScore(1));
            Assert.AreEqual(0f, loaded.BestTime(2));
            Assert.AreEqual(0.25f, loaded.MusicVolume, 0.001f);
            Assert.AreEqual(0.5f, loaded.EffectsVolume, 0.001f);
            Assert.IsFalse(loaded.GameCompleted);
        }

        [Test]
        public void A_missing_or_damaged_save_loads_defaults_for_what_it_cannot_read()
        {
            SaveData empty = SaveData.Parse(null, 4);
            Assert.AreEqual(1, empty.UnlockedLevels);

            SaveData damaged = SaveData.Parse("unlocked=99\nmusic=loud\ntime0=-5\ntime1=12.5\nscore2=abc\ngarbage", 4);
            Assert.AreEqual(4, damaged.UnlockedLevels, "clamped to the level count");
            Assert.AreEqual(new SaveData(4).MusicVolume, damaged.MusicVolume);
            Assert.AreEqual(0f, damaged.BestTime(0));
            Assert.AreEqual(12.5f, damaged.BestTime(1), 0.001f);
            Assert.AreEqual(0, damaged.BestScore(2));
        }

        [Test]
        public void Volumes_are_clamped_between_zero_and_one()
        {
            var save = new SaveData(1);
            save.SetVolumes(-1f, 3f);

            Assert.AreEqual(0f, save.MusicVolume);
            Assert.AreEqual(1f, save.EffectsVolume);
        }

        [Test]
        public void Times_are_shown_as_minutes_seconds_and_tenths()
        {
            Assert.AreEqual("--:--", SaveData.FormatTime(0f));
            Assert.AreEqual("0:05.2", SaveData.FormatTime(5.25f));
            Assert.AreEqual("2:03.9", SaveData.FormatTime(123.99f));
        }
    }
}
