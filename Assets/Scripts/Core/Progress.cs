using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SpookyPath.Core
{
    /// <summary>The tally of one attempt at one level: time, loot and kills, and its score.</summary>
    public sealed class LevelRun
    {
        public LevelRun(int level, float parSeconds)
        {
            Level = level;
            ParSeconds = parSeconds;
        }

        public int Level { get; }
        public float ParSeconds { get; }
        public float Elapsed { get; private set; }
        public int Coins { get; private set; }
        public int Candy { get; private set; }
        public int Relics { get; private set; }
        public int Kills { get; private set; }
        public int KillScore { get; private set; }
        public bool ArtefactDestroyed { get; private set; }

        public void Tick(float dt)
        {
            if (dt > 0f)
            {
                Elapsed += dt;
            }
        }

        public void AddCoin() => Coins++;
        public void AddCandy() => Candy++;
        public void AddRelic() => Relics++;
        public void DestroyArtefact() => ArtefactDestroyed = true;

        public void AddKill(EnemyKind kind)
        {
            Kills++;
            KillScore += EnemyStats.For(kind).Score;
        }

        /// <summary>Whole seconds finished under par (never negative).</summary>
        public int SecondsUnderPar => Math.Max(0, (int)Math.Floor(ParSeconds - Elapsed));

        public int Score =>
            Coins * Tuning.CoinScore +
            Candy * Tuning.CandyScore +
            Relics * Tuning.RelicScore +
            KillScore +
            (ArtefactDestroyed ? Tuning.ArtefactScore : 0) +
            SecondsUnderPar * Tuning.ParSecondScore;
    }

    /// <summary>
    /// Everything the game remembers between sessions: unlocked levels, best time and score per
    /// level, and volume settings. Serialized as plain "key=value" lines so a damaged or older save
    /// still loads with defaults for whatever it cannot read.
    /// </summary>
    public sealed class SaveData
    {
        public const int Version = 1;

        readonly float[] bestTimes;
        readonly int[] bestScores;

        public SaveData(int levelCount)
        {
            if (levelCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(levelCount));
            }
            LevelCount = levelCount;
            bestTimes = new float[levelCount];
            bestScores = new int[levelCount];
            UnlockedLevels = 1;
            MusicVolume = 0.7f;
            EffectsVolume = 0.9f;
        }

        public int LevelCount { get; }
        /// <summary>How many levels can be started from the menu (level 0 is always open).</summary>
        public int UnlockedLevels { get; private set; }
        public bool GameCompleted { get; private set; }
        public float MusicVolume { get; private set; }
        public float EffectsVolume { get; private set; }

        /// <summary>Best time for a level in seconds, or 0 when it was never finished.</summary>
        public float BestTime(int level) => bestTimes[level];
        public int BestScore(int level) => bestScores[level];

        public bool IsUnlocked(int level) => level >= 0 && level < UnlockedLevels;

        public float BestTotalTime
        {
            get
            {
                float total = 0f;
                foreach (float time in bestTimes)
                {
                    if (time <= 0f)
                    {
                        return 0f;
                    }
                    total += time;
                }
                return total;
            }
        }

        public readonly struct Record
        {
            public readonly bool NewBestTime;
            public readonly bool NewBestScore;
            public readonly bool UnlockedNext;

            public Record(bool newBestTime, bool newBestScore, bool unlockedNext)
            {
                NewBestTime = newBestTime;
                NewBestScore = newBestScore;
                UnlockedNext = unlockedNext;
            }
        }

        /// <summary>Stores a finished level: keeps the best time and score and unlocks the next level.</summary>
        public Record RecordFinish(int level, float seconds, int score)
        {
            if (level < 0 || level >= LevelCount)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }
            bool bestTime = seconds > 0f && (bestTimes[level] <= 0f || seconds < bestTimes[level]);
            if (bestTime)
            {
                bestTimes[level] = seconds;
            }
            bool bestScore = score > bestScores[level];
            if (bestScore)
            {
                bestScores[level] = score;
            }
            bool unlocked = false;
            if (level + 1 < LevelCount && UnlockedLevels < level + 2)
            {
                UnlockedLevels = level + 2;
                unlocked = true;
            }
            if (level == LevelCount - 1)
            {
                GameCompleted = true;
            }
            return new Record(bestTime, bestScore, unlocked);
        }

        public void SetVolumes(float music, float effects)
        {
            MusicVolume = Clamp01(music);
            EffectsVolume = Clamp01(effects);
        }

        public string Serialize()
        {
            var text = new StringBuilder();
            text.Append("version=").Append(Version).Append('\n');
            text.Append("unlocked=").Append(UnlockedLevels).Append('\n');
            text.Append("completed=").Append(GameCompleted ? 1 : 0).Append('\n');
            text.Append("music=").Append(Format(MusicVolume)).Append('\n');
            text.Append("effects=").Append(Format(EffectsVolume)).Append('\n');
            for (int i = 0; i < LevelCount; i++)
            {
                text.Append("time").Append(i).Append('=').Append(Format(bestTimes[i])).Append('\n');
                text.Append("score").Append(i).Append('=').Append(bestScores[i]).Append('\n');
            }
            return text.ToString();
        }

        /// <summary>Reads a save written by <see cref="Serialize"/>; unknown or broken entries are ignored.</summary>
        public static SaveData Parse(string text, int levelCount)
        {
            var data = new SaveData(levelCount);
            if (string.IsNullOrEmpty(text))
            {
                return data;
            }
            var values = new Dictionary<string, string>();
            foreach (string line in text.Split('\n'))
            {
                int equals = line.IndexOf('=');
                if (equals > 0)
                {
                    values[line.Substring(0, equals).Trim()] = line.Substring(equals + 1).Trim();
                }
            }
            if (TryInt(values, "unlocked", out int unlocked))
            {
                data.UnlockedLevels = Math.Max(1, Math.Min(levelCount, unlocked));
            }
            if (TryInt(values, "completed", out int completed))
            {
                data.GameCompleted = completed == 1;
            }
            float music = data.MusicVolume, effects = data.EffectsVolume;
            TryFloat(values, "music", ref music);
            TryFloat(values, "effects", ref effects);
            data.SetVolumes(music, effects);
            for (int i = 0; i < levelCount; i++)
            {
                float time = 0f;
                if (TryFloat(values, "time" + i, ref time) && time > 0f)
                {
                    data.bestTimes[i] = time;
                }
                if (TryInt(values, "score" + i, out int score) && score > 0)
                {
                    data.bestScores[i] = score;
                }
            }
            return data;
        }

        static bool TryInt(Dictionary<string, string> values, string key, out int value)
        {
            value = 0;
            return values.TryGetValue(key, out string raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        static bool TryFloat(Dictionary<string, string> values, string key, ref float value)
        {
            if (values.TryGetValue(key, out string raw) &&
                float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                !float.IsNaN(parsed) && !float.IsInfinity(parsed))
            {
                value = parsed;
                return true;
            }
            return false;
        }

        static string Format(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        static float Clamp01(float value) => float.IsNaN(value) ? 0f : Math.Max(0f, Math.Min(1f, value));

        /// <summary>Formats seconds as m:ss.t for the HUD and results.</summary>
        public static string FormatTime(float seconds)
        {
            if (seconds <= 0f)
            {
                return "--:--";
            }
            int tenths = (int)Math.Floor(seconds * 10f);
            int minutes = tenths / 600;
            int rest = tenths % 600;
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}.{2}", minutes, rest / 10, rest % 10);
        }
    }
}
