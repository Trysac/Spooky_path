using UnityEngine;

namespace SpookyPath.Game
{
    /// <summary>Where the save text lives: PlayerPrefs in the game, memory in tests and screenshots.</summary>
    public interface ISaveStore
    {
        string Load();
        void Store(string text);
    }

    public sealed class PlayerPrefsSaveStore : ISaveStore
    {
        const string Key = "spooky-path.save";

        public string Load() => PlayerPrefs.GetString(Key, string.Empty);

        public void Store(string text)
        {
            PlayerPrefs.SetString(Key, text);
            PlayerPrefs.Save();
        }
    }

    public sealed class MemorySaveStore : ISaveStore
    {
        public string Text = string.Empty;

        public string Load() => Text;

        public void Store(string text) => Text = text;
    }
}
