using System;
using System.Collections.Generic;
using SpookyPath.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpookyPath.Game
{
    public enum HudScreen
    {
        Title,
        Levels,
        Playing,
        Paused,
        Settings,
        Results,
    }

    /// <summary>
    /// Binds the UI Toolkit layout (Hud.uxml): the in-game HUD and every menu. Menus are driven by
    /// a highlighted entry that keys, the d-pad and the mouse all move, so the game is fully
    /// playable with a gamepad. Buttons never take focus; the director reads the devices.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class Hud : MonoBehaviour
    {
        VisualElement root;
        VisualElement hurt;
        VisualElement fade;
        VisualElement playHud;
        VisualElement healthFill, manaFill, staminaFill;
        VisualElement staminaBar;
        Label healthText, manaText, staminaText;
        Label levelName, timer, coins, candy, keys, score, objective;
        VisualElement boss, bossFill;
        Label bossStatus;
        readonly VisualElement[] spellSlots = new VisualElement[SpellCatalog.Count];
        readonly VisualElement[] spellCooldowns = new VisualElement[SpellCatalog.Count];
        Label prompt, sign, banner, toast, titleBest;
        Label resultTitle, resultSubtitle, resultTime, resultBest, resultLoot, resultKills, resultScore, resultRecord;
        Button resultNext;
        Button resultRetry;
        Button musicButton, effectsButton;
        readonly Button[] levelButtons = new Button[4];
        readonly Dictionary<HudScreen, VisualElement> screens = new Dictionary<HudScreen, VisualElement>();
        readonly Dictionary<HudScreen, List<Button>> menus = new Dictionary<HudScreen, List<Button>>();
        readonly Dictionary<Button, Action> actions = new Dictionary<Button, Action>();
        float bannerTime, toastTime, hurtLevel;

        public HudScreen Screen { get; private set; }
        public int Selected { get; private set; }

        public event Action PlayClicked;
        public event Action LevelsClicked;
        public event Action SettingsClicked;
        public event Action QuitClicked;
        public event Action<int> LevelChosen;
        public event Action BackClicked;
        public event Action PauseClicked;
        public event Action ResumeClicked;
        public event Action RestartClicked;
        public event Action TitleClicked;
        public event Action NextClicked;
        public event Action<int> MusicChanged;
        public event Action<int> EffectsChanged;
        public event Action ButtonPressed;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            hurt = root.Q("hurt");
            fade = root.Q("fade");
            playHud = root.Q("play-hud");
            healthFill = root.Q("health-fill");
            manaFill = root.Q("mana-fill");
            staminaFill = root.Q("stamina-fill");
            staminaBar = staminaFill.parent;
            healthText = root.Q<Label>("health-text");
            manaText = root.Q<Label>("mana-text");
            staminaText = root.Q<Label>("stamina-text");
            levelName = root.Q<Label>("level-name");
            timer = root.Q<Label>("timer");
            coins = root.Q<Label>("coins");
            candy = root.Q<Label>("candy");
            keys = root.Q<Label>("keys");
            score = root.Q<Label>("score");
            objective = root.Q<Label>("objective");
            boss = root.Q("boss");
            bossFill = root.Q("boss-fill");
            bossStatus = root.Q<Label>("boss-status");
            for (int i = 0; i < spellSlots.Length; i++)
            {
                spellSlots[i] = root.Q($"spell-{i}");
                spellCooldowns[i] = spellSlots[i].Q(className: "spell__cooldown");
            }
            prompt = root.Q<Label>("prompt");
            sign = root.Q<Label>("sign");
            banner = root.Q<Label>("banner");
            toast = root.Q<Label>("toast");
            titleBest = root.Q<Label>("title-best");
            resultTitle = root.Q<Label>("result-title");
            resultSubtitle = root.Q<Label>("result-subtitle");
            resultTime = root.Q<Label>("result-time");
            resultBest = root.Q<Label>("result-best");
            resultLoot = root.Q<Label>("result-loot");
            resultKills = root.Q<Label>("result-kills");
            resultScore = root.Q<Label>("result-score");
            resultRecord = root.Q<Label>("result-record");

            screens[HudScreen.Title] = root.Q("title");
            screens[HudScreen.Levels] = root.Q("levels");
            screens[HudScreen.Paused] = root.Q("pause");
            screens[HudScreen.Settings] = root.Q("settings");
            screens[HudScreen.Results] = root.Q("results");

            Menu(HudScreen.Title, ("play-button", () => PlayClicked?.Invoke()), ("levels-button", () => LevelsClicked?.Invoke()),
                ("settings-button", () => SettingsClicked?.Invoke()), ("quit-button", () => QuitClicked?.Invoke()));
            var levelEntries = new List<(string, Action)>();
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int index = i;
                levelEntries.Add(($"level-{i}", () => LevelChosen?.Invoke(index)));
            }
            levelEntries.Add(("levels-back", () => BackClicked?.Invoke()));
            Menu(HudScreen.Levels, levelEntries.ToArray());
            for (int i = 0; i < levelButtons.Length; i++)
            {
                levelButtons[i] = root.Q<Button>($"level-{i}");
            }
            Menu(HudScreen.Paused, ("resume-button", () => ResumeClicked?.Invoke()), ("restart-button", () => RestartClicked?.Invoke()),
                ("pause-settings-button", () => SettingsClicked?.Invoke()), ("pause-title-button", () => TitleClicked?.Invoke()));
            Menu(HudScreen.Settings, ("music-button", () => MusicChanged?.Invoke(1)), ("effects-button", () => EffectsChanged?.Invoke(1)),
                ("settings-back", () => BackClicked?.Invoke()));
            musicButton = root.Q<Button>("music-button");
            effectsButton = root.Q<Button>("effects-button");
            Menu(HudScreen.Results, ("result-next", () => NextClicked?.Invoke()), ("result-retry", () => RestartClicked?.Invoke()),
                ("result-title-button", () => TitleClicked?.Invoke()));
            resultNext = root.Q<Button>("result-next");
            resultRetry = root.Q<Button>("result-retry");

            Button pauseButton = root.Q<Button>("pause-button");
            pauseButton.focusable = false;
            pauseButton.clicked += () => PauseClicked?.Invoke();
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                root.Q<Button>("quit-button").style.display = DisplayStyle.None;
                menus[HudScreen.Title].RemoveAt(menus[HudScreen.Title].Count - 1);
            }
            ShowScreen(HudScreen.Title);
        }

        void Menu(HudScreen screen, params (string name, Action action)[] entries)
        {
            var list = new List<Button>();
            foreach ((string name, Action action) in entries)
            {
                Button button = root.Q<Button>(name);
                button.focusable = false;
                actions[button] = action;
                button.clicked += () => Activate(button);
                button.RegisterCallback<PointerEnterEvent>(_ => Highlight(list.IndexOf(button)));
                list.Add(button);
            }
            menus[screen] = list;
        }

        void Activate(Button button)
        {
            if (!button.enabledSelf)
            {
                return;
            }
            ButtonPressed?.Invoke();
            actions[button]();
        }

        public void ShowScreen(HudScreen screen, int selected = 0)
        {
            Screen = screen;
            foreach (KeyValuePair<HudScreen, VisualElement> pair in screens)
            {
                pair.Value.style.display = pair.Key == screen ? DisplayStyle.Flex : DisplayStyle.None;
            }
            bool inGame = screen == HudScreen.Playing || screen == HudScreen.Paused || screen == HudScreen.Results || (screen == HudScreen.Settings && playHud.style.display == DisplayStyle.Flex);
            playHud.style.display = inGame ? DisplayStyle.Flex : DisplayStyle.None;
            if (screen != HudScreen.Playing)
            {
                SetPrompt(null);
                SetSign(null);
            }
            Highlight(selected);
        }

        /// <summary>Hides the in-game HUD (title screen) regardless of the last screen.</summary>
        public void HideGameHud() => playHud.style.display = DisplayStyle.None;

        void Highlight(int index)
        {
            if (!menus.TryGetValue(Screen, out List<Button> list) || list.Count == 0)
            {
                return;
            }
            Selected = Mathf.Clamp(index, 0, list.Count - 1);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].EnableInClassList("menu__button--selected", i == Selected);
            }
        }

        public void Navigate(int step)
        {
            if (step == 0 || !menus.TryGetValue(Screen, out List<Button> list) || list.Count == 0)
            {
                return;
            }
            int index = Selected;
            for (int tries = 0; tries < list.Count; tries++)
            {
                index = (index + step + list.Count) % list.Count;
                if (list[index].enabledSelf && list[index].resolvedStyle.display != DisplayStyle.None)
                {
                    break;
                }
            }
            Highlight(index);
        }

        public void ConfirmSelected()
        {
            if (menus.TryGetValue(Screen, out List<Button> list) && Selected < list.Count)
            {
                Activate(list[Selected]);
            }
        }

        /// <summary>Left/right on a volume entry of the settings menu.</summary>
        public void Adjust(int step)
        {
            if (Screen != HudScreen.Settings || step == 0)
            {
                return;
            }
            if (Selected == 0) MusicChanged?.Invoke(step);
            else if (Selected == 1) EffectsChanged?.Invoke(step);
        }

        // ---- In-game HUD ----

        public void SetVitals(Vitals vitals)
        {
            Fill(healthFill, healthText, vitals.Health, vitals.MaxHealth);
            Fill(manaFill, manaText, vitals.Mana, vitals.MaxMana);
            Fill(staminaFill, staminaText, vitals.Stamina, vitals.MaxStamina);
            staminaBar.EnableInClassList("bar--exhausted", vitals.Exhausted);
        }

        static void Fill(VisualElement fill, Label text, float value, float max)
        {
            fill.style.width = Length.Percent(100f * Mathf.Clamp01(value / Mathf.Max(1f, max)));
            text.text = $"{Mathf.CeilToInt(value)} / {Mathf.RoundToInt(max)}";
        }

        public void SetSpells(SpellBook book, Vitals vitals)
        {
            for (int i = 0; i < spellSlots.Length; i++)
            {
                var kind = (SpellKind)i;
                spellCooldowns[i].style.height = Length.Percent(100f * book.CooldownFraction(kind));
                spellSlots[i].EnableInClassList("spell--unaffordable", vitals.Mana < SpellCatalog.Get(kind).ManaCost);
            }
        }

        public void SetSpellKeys(bool gamepad)
        {
            string[] labels = gamepad ? new[] { "LB", "RB", "LT", "RT" } : new[] { "1", "2", "3", "4" };
            for (int i = 0; i < spellSlots.Length; i++)
            {
                spellSlots[i].Q<Label>(className: "spell__key").text = labels[i];
            }
        }

        public void SetLevel(string name) => levelName.text = name;

        public void SetRun(LevelRun run, int keyCount)
        {
            timer.text = SaveData.FormatTime(Mathf.Max(0.01f, run.Elapsed));
            coins.text = $"Coins {run.Coins}";
            candy.text = $"Candy {run.Candy}";
            keys.text = $"Keys {keyCount}";
            score.text = $"Score {run.LootScore:N0}";
        }

        public void SetObjective(string text) => objective.text = text ?? string.Empty;

        public void SetArtefact(Artefact artefact)
        {
            boss.style.display = artefact != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (artefact == null)
            {
                return;
            }
            bossFill.style.width = Length.Percent(100f * artefact.Health / Artefact.MaxHealth);
            bossFill.EnableInClassList("boss__fill--shielded", artefact.Shielded);
            bossStatus.text = artefact.Shielded
                ? $"Warded - {artefact.WardsRemaining} dark {(artefact.WardsRemaining == 1 ? "brazier burns" : "braziers burn")}"
                : "The ward is broken - strike it down!";
        }

        public void SetPrompt(string text)
        {
            prompt.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
            prompt.text = text ?? string.Empty;
        }

        public void SetSign(string text)
        {
            sign.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
            sign.text = text ?? string.Empty;
        }

        public void Banner(string text, float seconds = 2.4f)
        {
            banner.text = text;
            bannerTime = seconds;
        }

        public void Toast(string text, Color colour, float seconds = 1.6f)
        {
            toast.text = text;
            toast.style.color = colour;
            toastTime = seconds;
        }

        public void Hurt(float amount) => hurtLevel = Mathf.Max(hurtLevel, amount);

        public void SetFade(float amount) => fade.style.opacity = Mathf.Clamp01(amount);

        public void SetTitleBest(string text) => titleBest.text = text;

        public void SetLevels(SaveData save)
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                bool open = i < Levels.Count && save.IsUnlocked(i);
                string name = i < Levels.Count ? Levels.All[i].Name : "?";
                string best = save.BestTime(i) > 0f ? $"   best {SaveData.FormatTime(save.BestTime(i))}" : string.Empty;
                levelButtons[i].text = open ? $"{i}. {name}{best}" : $"{i}. Locked";
                levelButtons[i].SetEnabled(open);
                levelButtons[i].EnableInClassList("menu__button--locked", !open);
            }
        }

        public void SetVolumes(float music, float effects)
        {
            musicButton.text = $"Music  {Mathf.RoundToInt(music * 100f)}%";
            effectsButton.text = $"Effects  {Mathf.RoundToInt(effects * 100f)}%";
        }

        public void ShowResults(string title, string subtitle, string[] lines, string record, bool canGoNext, string nextLabel, bool canRetry = true)
        {
            bannerTime = 0f;
            toastTime = 0f;
            resultTitle.text = title;
            resultSubtitle.text = subtitle;
            Label[] slots = { resultTime, resultBest, resultLoot, resultKills, resultScore };
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].text = i < lines.Length ? lines[i] : string.Empty;
                slots[i].style.display = i < lines.Length && !string.IsNullOrEmpty(lines[i]) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            resultRecord.text = record ?? string.Empty;
            resultNext.style.display = canGoNext ? DisplayStyle.Flex : DisplayStyle.None;
            resultNext.text = nextLabel;
            resultRetry.style.display = canRetry ? DisplayStyle.Flex : DisplayStyle.None;
            ShowScreen(HudScreen.Results, canGoNext ? 0 : 1);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bannerTime -= dt;
            banner.style.opacity = Mathf.Clamp01(bannerTime * 2f);
            toastTime -= dt;
            toast.style.opacity = Mathf.Clamp01(toastTime * 3f);
            hurtLevel = Mathf.MoveTowards(hurtLevel, 0f, dt * 1.6f);
            hurt.style.opacity = hurtLevel;
        }
    }
}
