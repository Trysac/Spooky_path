using System.Collections.Generic;
using SpookyPath.Core;
using UnityEngine;

namespace SpookyPath.Game
{
    public enum Phase
    {
        Title,
        Playing,
        Paused,
        LevelComplete,
        GameOver,
        Victory,
    }

    /// <summary>
    /// Runs the game: menus, building levels from their maps, the explorer, the skeletons, bolts,
    /// puzzles, hazards, the artefact, scoring and saving. Rules come from SpookyPath.Core; this
    /// class connects them to the scene, the HUD and the audio.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour
    {
        const int WallMask = 1 << 0;
        const float PickupRadius = 1.4f;
        const float UseRadius = 2.6f;
        const float ResultsDelay = 1.6f;

        [SerializeField] GameArt art;
        [SerializeField] Hud hud;
        [SerializeField] CameraRig cameraRig;

        readonly GameInput input = new GameInput();
        readonly List<EnemyController> enemies = new List<EnemyController>();
        readonly List<Projectile> projectiles = new List<Projectile>();
        AudioDirector audioDirector;
        ISaveStore store;
        Camera view;
        PlayerController player;
        LevelView level;
        PuzzleBoard puzzle;
        Artefact artefact;
        LevelRun run;
        float phaseTime;
        float levelTime;
        PadView padLock;
        bool resultsShown;
        HudScreen settingsReturn = HudScreen.Title;
        string deathCause;
        int lastGamepadState = -1;
        bool skipTick;
        int sessionScore;
        float sessionTime;

        public Phase Phase { get; private set; } = Phase.Title;
        public int LevelIndex { get; private set; }
        public PlayerController Player => player;
        public LevelView Level => level;
        public IReadOnlyList<EnemyController> Enemies => enemies;
        public LevelRun Run => run;
        public SaveData Save { get; private set; }
        public PuzzleBoard Puzzle => puzzle;
        public Artefact ArtefactState => artefact;
        public Hud Hud => hud;
        public CameraRig CameraRig => cameraRig;

        /// <summary>When true the explorer follows <see cref="Scripted"/> instead of the devices (tests, screenshots).</summary>
        public bool ScriptedInput { get; set; }
        public Commands Scripted = Commands.None;
        public bool Invincible { get; set; }

        void Awake()
        {
            Fx.Init(art);
            view = cameraRig.GetComponent<Camera>();
            audioDirector = gameObject.AddComponent<AudioDirector>();
            audioDirector.Init(art);
            UseStore(new PlayerPrefsSaveStore());

            hud.PlayClicked += () => StartLevel(0);
            hud.LevelsClicked += () => { hud.SetLevels(Save); hud.ShowScreen(HudScreen.Levels); };
            hud.SettingsClicked += OpenSettings;
            hud.QuitClicked += Application.Quit;
            hud.LevelChosen += StartLevel;
            hud.BackClicked += Back;
            hud.PauseClicked += () => { if (Phase == Phase.Playing) PauseGame(); };
            hud.ResumeClicked += Resume;
            hud.RestartClicked += RestartLevel;
            hud.TitleClicked += ToTitle;
            hud.NextClicked += NextLevel;
            hud.MusicChanged += step => ChangeVolume(step, true);
            hud.EffectsChanged += step => ChangeVolume(step, false);
            hud.ButtonPressed += () => audioDirector.Play("click", 0.6f, 0f);
        }

        void Start() => ToTitle();

        /// <summary>Swaps the save storage (tests and screenshots use memory so PlayerPrefs stay untouched).</summary>
        public void UseStore(ISaveStore newStore)
        {
            store = newStore;
            Save = SaveData.Parse(store.Load(), Levels.Count);
            audioDirector.SetVolumes(Save.MusicVolume, Save.EffectsVolume);
        }

        public void UseMemoryStore(string initial = "") => UseStore(new MemorySaveStore { Text = initial });

        // ---- Flow ----

        public void ToTitle()
        {
            Time.timeScale = 1f;
            ClearLevel();
            Phase = Phase.Title;
            BuildBackdrop();
            hud.ShowScreen(HudScreen.Title);
            hud.HideGameHud();
            hud.SetFade(0f);
            float total = Save.BestTotalTime;
            hud.SetTitleBest(Save.GameCompleted
                ? total > 0f ? $"The artefact is destroyed. Best descent: {SaveData.FormatTime(total)}" : "The artefact is destroyed."
                : Save.UnlockedLevels > 1 ? $"Deepest level reached: {Levels.All[Save.UnlockedLevels - 1].Name}" : string.Empty);
            audioDirector.PlayMusic(art.TitleMusic);
        }

        void BuildBackdrop()
        {
            level = new GameObject("Level").AddComponent<LevelView>();
            level.Build(art, Levels.All[Levels.Last]);
            cameraRig.LookAtPoint(level.ArtefactPosition + new Vector3(0f, 1f, -6f), new Vector3(9f, 9f, -16f));
        }

        public void StartLevel(int index)
        {
            if (index < 0 || index >= Levels.Count)
            {
                return;
            }
            Time.timeScale = 1f;
            ClearLevel();
            if (index == 0)
            {
                sessionScore = 0;
                sessionTime = 0f;
            }
            LevelIndex = index;
            LevelDefinition definition = Levels.All[index];
            level = new GameObject("Level").AddComponent<LevelView>();
            level.Build(art, definition);
            puzzle = PuzzleBoard.For(level.Map);
            artefact = level.Wards.Count > 0 ? new Artefact(level.Wards.Count) : null;
            run = new LevelRun(index, definition.ParSeconds);

            if (player == null)
            {
                player = new GameObject("Explorer").AddComponent<PlayerController>();
                player.Init(art);
                player.Struck += ResolveStrike;
                player.Cast += ResolveSpell;
                player.CastFailed += result => { if (result == CastResult.NotEnoughMana) { audioDirector.Play("denied", 0.5f); hud.Toast("Not enough mana", Fx.Hex("#8fb0ff"), 0.8f); } };
                player.Jumped += () => audioDirector.Play("jump", 0.45f);
                player.Landed += () => audioDirector.Play("land", 0.35f);
            }
            player.gameObject.SetActive(true);
            player.ResetState();
            player.Teleport(level.StartPosition, Vector3.forward);
            cameraRig.Follow(player.transform, true);

            foreach (Placement placement in level.Map.All(PlacementKind.Enemy))
            {
                Vector3 post = LevelView.World(placement.X, placement.Y);
                Vec2? waypoint = null;
                if (placement.Patrols)
                {
                    waypoint = LevelMap.CellCenter(level.Map.PatrolEndX(placement), placement.Y);
                }
                SpawnEnemy((EnemyKind)placement.Variant, post, waypoint);
            }

            Phase = Phase.Playing;
            phaseTime = 0f;
            levelTime = 0f;
            resultsShown = false;
            padLock = null;
            deathCause = null;
            skipTick = true;
            hud.ShowScreen(HudScreen.Playing);
            hud.SetLevel(definition.Name);
            hud.SetArtefact(artefact);
            hud.SetObjective(artefact != null ? "Purify the dark braziers, then destroy the artefact." : "Find the stairs leading down.");
            hud.Banner(definition.Name, 3f);
            hud.Toast(definition.Subtitle, Fx.Hex("#efe6cf"), 3.2f);
            hud.SetFade(1f);
            audioDirector.PlayMusic(index == Levels.Last ? art.FinalMusic : art.DungeonMusic);
        }

        public void RestartLevel() => StartLevel(LevelIndex);

        public void NextLevel()
        {
            // After the victory "next" means a new descent from the crypt gate.
            StartLevel(LevelIndex < Levels.Last ? LevelIndex + 1 : 0);
        }

        public void PauseGame()
        {
            if (Phase != Phase.Playing)
            {
                return;
            }
            Phase = Phase.Paused;
            Time.timeScale = 0f;
            hud.ShowScreen(HudScreen.Paused);
            audioDirector.Play("click", 0.6f, 0f);
        }

        public void Resume()
        {
            if (Phase != Phase.Paused)
            {
                return;
            }
            Phase = Phase.Playing;
            Time.timeScale = 1f;
            skipTick = true;
            hud.ShowScreen(HudScreen.Playing);
        }

        void OpenSettings()
        {
            settingsReturn = hud.Screen;
            hud.SetVolumes(Save.MusicVolume, Save.EffectsVolume);
            hud.ShowScreen(HudScreen.Settings);
        }

        void Back()
        {
            if (hud.Screen == HudScreen.Settings)
            {
                hud.ShowScreen(settingsReturn, settingsReturn == HudScreen.Paused ? 2 : 2);
                if (settingsReturn == HudScreen.Title)
                {
                    hud.HideGameHud();
                }
            }
            else if (hud.Screen == HudScreen.Levels)
            {
                hud.ShowScreen(HudScreen.Title, 1);
                hud.HideGameHud();
            }
        }

        void ChangeVolume(int step, bool music)
        {
            float current = music ? Save.MusicVolume : Save.EffectsVolume;
            float next = Mathf.Round((current + step * 0.1f) * 10f) / 10f;
            if (next > 1.001f) next = 0f;
            if (next < -0.001f) next = 1f;
            Save.SetVolumes(music ? next : Save.MusicVolume, music ? Save.EffectsVolume : next);
            store.Store(Save.Serialize());
            audioDirector.SetVolumes(Save.MusicVolume, Save.EffectsVolume);
            hud.SetVolumes(Save.MusicVolume, Save.EffectsVolume);
        }

        void ClearLevel()
        {
            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null) Destroy(enemy.gameObject);
            }
            enemies.Clear();
            foreach (Projectile projectile in projectiles)
            {
                if (projectile != null) Destroy(projectile.gameObject);
            }
            projectiles.Clear();
            if (level != null)
            {
                Destroy(level.gameObject);
                level = null;
            }
            foreach (ParticleSystem stray in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                // One-shot bursts and lava embers live at the scene root.
                if (stray.transform.parent == null) Destroy(stray.gameObject);
            }
            artefact = null;
            if (player != null)
            {
                player.gameObject.SetActive(false);
            }
        }

        public EnemyController SpawnEnemy(EnemyKind kind, Vector3 position, Vec2? waypoint = null)
        {
            var go = new GameObject($"Skeleton {kind}");
            go.transform.position = position + Vector3.up * 0.05f;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var enemy = go.AddComponent<EnemyController>();
            enemy.Init(art, new EnemyBrain(kind, LevelView.Flat(position), waypoint));
            enemies.Add(enemy);
            return enemy;
        }

        // ---- Frame ----

        void Update()
        {
            float dt = Time.deltaTime;
            phaseTime += Time.unscaledDeltaTime;
            bool gamepad = input.UsingGamepad;
            if ((gamepad ? 1 : 0) != lastGamepadState)
            {
                lastGamepadState = gamepad ? 1 : 0;
                hud.SetSpellKeys(gamepad);
            }

            switch (Phase)
            {
                case Phase.Title:
                case Phase.Paused:
                case Phase.LevelComplete:
                case Phase.GameOver:
                case Phase.Victory:
                    MenuInput();
                    break;
            }

            if (level != null)
            {
                level.Animate(Phase == Phase.Paused ? 0f : dt, Time.time);
            }

            if (Phase == Phase.Playing)
            {
                hud.SetFade(1f - phaseTime * 2f);
                if (!ScriptedInput && input.Pause)
                {
                    PauseGame();
                    return;
                }
                Commands commands = ScriptedInput ? Scripted : input.Read();
                ConsumeScripted();
                if (skipTick)
                {
                    // The press that started or resumed play must not also jump or strike.
                    commands.Jump = commands.Strike = commands.Interact = false;
                    commands.Spell = -1;
                    skipTick = false;
                }
                Play(dt, commands);
            }
            else if (Phase == Phase.LevelComplete || Phase == Phase.GameOver || Phase == Phase.Victory)
            {
                // The world keeps moving behind the results (skeletons still walk, lava still flows).
                Play(dt, Commands.None);
                if (!resultsShown && phaseTime >= ResultsDelay)
                {
                    resultsShown = true;
                    ShowResults();
                }
            }
        }

        void ConsumeScripted()
        {
            Scripted.Jump = false;
            Scripted.Strike = false;
            Scripted.Interact = false;
            Scripted.Spell = -1;
        }

        void MenuInput()
        {
            if (Phase == Phase.Paused && hud.Screen == HudScreen.Paused && input.Pause)
            {
                Resume();
                return;
            }
            if (Phase != Phase.Title && Phase != Phase.Paused && !resultsShown)
            {
                return;
            }
            hud.Navigate(input.NavigateVertical());
            if (hud.Screen == HudScreen.Results)
            {
                hud.Navigate(input.NavigateHorizontal());
            }
            else
            {
                hud.Adjust(input.NavigateHorizontal());
            }
            if (input.Confirm)
            {
                hud.ConfirmSelected();
            }
            else if (input.Back && (hud.Screen == HudScreen.Settings || hud.Screen == HudScreen.Levels))
            {
                Back();
            }
        }

        void Play(float dt, Commands commands)
        {
            if (player == null || !player.gameObject.activeSelf || level == null)
            {
                return;
            }
            bool alive = Phase == Phase.Playing && !player.IsDead;
            if (!alive)
            {
                commands = Commands.None;
            }
            else
            {
                AimAssist(commands);
            }

            PlatformView platform = level.PlatformUnder(player.transform.position);
            Vector3 carry = platform != null ? platform.Delta : Vector3.zero;
            player.Tick(dt, commands, carry);
            if (Invincible && player.Vitals.Health < player.Vitals.MaxHealth * 0.5f)
            {
                player.Vitals.Heal(player.Vitals.MaxHealth);
            }
            cameraRig.SetLead(player.Velocity);

            UpdateEnemies(dt);
            UpdateProjectiles(dt);
            if (Phase == Phase.Playing)
            {
                run.Tick(dt);
                levelTime += dt;
                if (!player.IsDead)
                {
                    Interactions(commands);
                    Hazards();
                }
                UpdateArtefact(dt);
                if (player.IsDead && Phase == Phase.Playing)
                {
                    GameOver(deathCause ?? "The skeletons got you.");
                }
            }
            UpdateHud();
        }

        void AimAssist(Commands commands)
        {
            if (commands.Strike)
            {
                Vector3? target = Nearest(player.transform.position, 4f, null);
                if (target.HasValue) player.FaceTowards(target.Value);
            }
            else if (commands.Spell == (int)SpellKind.Firebolt)
            {
                Vector3? target = Nearest(player.transform.position, 16f, player.Facing);
                if (target.HasValue) player.FaceTowards(target.Value);
            }
        }

        /// <summary>The closest living skeleton (or the exposed artefact) in range, optionally within a cone.</summary>
        Vector3? Nearest(Vector3 from, float range, Vector3? cone)
        {
            Vector3? best = null;
            float bestDistance = range;
            foreach (EnemyController enemy in enemies)
            {
                if (enemy.Brain.IsDead) continue;
                Consider(enemy.transform.position);
            }
            if (artefact != null && !artefact.Shielded && !artefact.Destroyed)
            {
                Consider(level.ArtefactPosition);
            }
            return best;

            void Consider(Vector3 position)
            {
                Vector3 to = position - from;
                to.y = 0f;
                float distance = to.magnitude;
                if (distance > bestDistance) return;
                if (cone.HasValue && distance > 0.1f && Vector3.Angle(cone.Value, to) > 35f) return;
                bestDistance = distance;
                best = position;
            }
        }

        // ---- Combat ----

        void ResolveStrike(Vector3 origin, Vector3 facing)
        {
            audioDirector.Play("swing", 0.7f);
            bool hit = false;
            foreach (EnemyController enemy in enemies)
            {
                if (enemy.Brain.IsDead) continue;
                if (Combat.InArc(LevelView.Flat(origin), LevelView.Flat(facing), LevelView.Flat(enemy.transform.position), Tuning.StrikeRange + 0.5f, Tuning.StrikeArcDegrees) &&
                    Mathf.Abs(enemy.transform.position.y - origin.y) < 1.5f)
                {
                    DamageEnemy(enemy, Tuning.StrikeDamage, origin);
                    hit = true;
                }
            }
            if (artefact != null && Vector3.Distance(Flat(origin), Flat(level.ArtefactPosition)) < Tuning.StrikeRange + 1.6f)
            {
                hit |= DamageArtefact(Tuning.StrikeDamage, origin + facing * 1.5f + Vector3.up * 1.5f);
            }
            if (hit)
            {
                cameraRig.Shake(0.25f);
            }
        }

        void ResolveSpell(SpellKind kind)
        {
            Vector3 position = player.transform.position;
            switch (kind)
            {
                case SpellKind.Firebolt:
                    projectiles.Add(Projectile.Launch(art, position + Vector3.up * 1.3f + player.Facing * 0.8f, player.Facing, SpellCatalog.Get(kind).Reach, SpellCatalog.Get(kind).Power, false));
                    audioDirector.Play("firebolt", 0.8f);
                    break;
                case SpellKind.FrostNova:
                    SpellInfo nova = SpellCatalog.Get(kind);
                    Fx.Ring(position, Fx.Hex("#9fe8ff"), nova.Reach);
                    Fx.Burst(position + Vector3.up, Fx.Hex("#dff8ff"), 30, 4f, 0.4f, 0.8f, -0.1f);
                    Fx.Flash(position + Vector3.up * 2f, Fx.Hex("#9fe8ff"), 3f, 10f, 0.4f);
                    audioDirector.Play("frost", 0.9f);
                    foreach (EnemyController enemy in enemies.ToArray())
                    {
                        if (enemy.Brain.IsDead || Vector3.Distance(Flat(enemy.transform.position), Flat(position)) > nova.Reach) continue;
                        enemy.Brain.Freeze(3f);
                        DamageEnemy(enemy, nova.Power, position);
                    }
                    if (artefact != null && Vector3.Distance(Flat(position), Flat(level.ArtefactPosition)) < nova.Reach + 1.5f)
                    {
                        DamageArtefact(nova.Power, level.ArtefactPosition + Vector3.up * 2.5f);
                    }
                    cameraRig.Shake(0.3f);
                    break;
                case SpellKind.Heal:
                    Fx.Motes(null, position + Vector3.up * 0.2f, Fx.Hex("#7dffb0"), 0.9f, 60f, false);
                    Fx.Burst(position + Vector3.up * 1.2f, Fx.Hex("#7dffb0"), 24, 2.5f, 0.35f, 1f, -0.4f);
                    audioDirector.Play("heal", 0.8f);
                    break;
                case SpellKind.Shield:
                    Fx.Burst(position + Vector3.up * 1.2f, Fx.Hex("#efe6cf"), 30, 3f, 0.3f, 0.6f, 0f);
                    audioDirector.Play("shield", 0.8f);
                    break;
            }
        }

        void DamageEnemy(EnemyController enemy, float damage, Vector3 from)
        {
            bool killed = enemy.Brain.TakeDamage(damage);
            Fx.Burst(enemy.transform.position + Vector3.up * 1.2f, Fx.Hex("#fff3d6"), 10, 4f, 0.25f, 0.35f, 0.6f);
            enemy.PlayHurt(from);
            audioDirector.Play("hit", 0.8f);
            if (killed)
            {
                run.AddKill(enemy.Brain.Kind);
                enemy.Shatter();
                audioDirector.Play("bones", 0.9f);
            }
        }

        bool DamageArtefact(float damage, Vector3 at)
        {
            if (artefact == null || artefact.Destroyed)
            {
                return false;
            }
            if (artefact.Shielded)
            {
                Fx.Burst(at, Fx.Hex("#b46bff"), 8, 2f, 0.3f, 0.3f, 0f);
                audioDirector.Play("denied", 0.6f);
                hud.Toast("The ward holds - purify the braziers!", Fx.Hex("#d678ff"), 1.4f);
                return false;
            }
            artefact.TakeDamage(damage);
            Fx.Burst(at, Fx.Hex("#ff4a8a"), 16, 5f, 0.35f, 0.5f, 0.2f);
            audioDirector.Play("artefact_hit", 0.9f);
            if (artefact.Destroyed)
            {
                DestroyArtefact();
            }
            return true;
        }

        void DestroyArtefact()
        {
            run.DestroyArtefact();
            Vector3 core = level.ArtefactCore.position;
            level.ArtefactCore.gameObject.SetActive(false);
            Fx.Burst(core, Fx.Hex("#ff4a8a"), 120, 12f, 0.6f, 1.4f, 0.3f);
            Fx.Burst(core, Fx.Hex("#ffffff"), 60, 8f, 0.4f, 1f, -0.2f);
            Fx.Ring(level.ArtefactPosition, Fx.Hex("#ffd6f6"), 14f, 120);
            Fx.Flash(core, Fx.Hex("#ffd6f6"), 8f, 30f, 1.2f);
            cameraRig.Shake(1.2f);
            audioDirector.Play("shatter", 1f, 0f);
            foreach (EnemyController enemy in enemies.ToArray())
            {
                if (!enemy.Brain.IsDead)
                {
                    enemy.Brain.TakeDamage(9999f);
                    enemy.Shatter();
                }
            }
            hud.Banner("The darkness lifts!", 4f);
            Finish(Phase.Victory);
        }

        void UpdateEnemies(float dt)
        {
            Vector3 playerPosition = player.transform.position;
            Vector3 playerEye = playerPosition + Vector3.up * 1.4f;
            Vec2 playerFlat = LevelView.Flat(playerPosition);
            bool playerAlive = !player.IsDead && Phase == Phase.Playing;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = enemies[i];
                if (enemy.Brain.IsDead)
                {
                    if (enemy.DeadFor > 0.2f)
                    {
                        Destroy(enemy.gameObject);
                        enemies.RemoveAt(i);
                    }
                    else
                    {
                        enemy.Drive(dt, default, level, view);
                    }
                    continue;
                }
                bool visible = Mathf.Abs(playerPosition.y - enemy.transform.position.y) < 3f &&
                               !Physics.Linecast(enemy.Eye, playerEye, WallMask, QueryTriggerInteraction.Ignore);
                EnemyIntent intent = enemy.Brain.Tick(dt, LevelView.Flat(enemy.transform.position), playerFlat, visible, playerAlive);
                if (intent.Strike)
                {
                    if (enemy.Brain.Stats.Ranged)
                    {
                        Vector3 from = enemy.Eye + enemy.transform.forward * 0.6f;
                        projectiles.Add(Projectile.Launch(art, from, playerEye - from, 11f, enemy.Brain.Stats.Damage, true));
                        audioDirector.Play("curse", 0.6f);
                    }
                    else
                    {
                        enemy.PlayStrike();
                        audioDirector.Play("enemy_swing", 0.6f);
                        HurtPlayer(enemy.Brain.Stats.Damage, "Cut down by a skeleton.");
                    }
                }
                enemy.Drive(dt, intent, level, view);
                if (enemy.transform.position.y < Tuning.FallDeathDepth || level.TileAt(enemy.transform.position) == Tile.Lava && enemy.transform.position.y < -0.3f)
                {
                    enemy.Brain.TakeDamage(9999f);
                    enemy.Shatter();
                }
            }
        }

        void HurtPlayer(float damage, string cause)
        {
            if (Invincible || player.IsDead)
            {
                return;
            }
            float taken = player.Hurt(damage);
            if (taken > 0f)
            {
                hud.Hurt(0.6f);
                cameraRig.Shake(0.35f);
                audioDirector.Play("player_hurt", 0.8f);
                Fx.Burst(player.transform.position + Vector3.up * 1.2f, Fx.Hex("#ff3b4e"), 10, 3f, 0.25f, 0.4f, 0.8f);
                if (player.IsDead)
                {
                    deathCause = cause;
                }
            }
            else if (player.Vitals.Shielded)
            {
                Fx.Burst(player.transform.position + Vector3.up * 1.2f, Fx.Hex("#efe6cf"), 8, 3f, 0.25f, 0.3f, 0f);
                audioDirector.Play("block", 0.6f);
            }
        }

        void UpdateProjectiles(float dt)
        {
            Vector3 playerCentre = player.transform.position + Vector3.up * 1f;
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Projectile bolt = projectiles[i];
                Vector3 from = bolt.transform.position;
                Vector3 to = from + bolt.Velocity * dt;
                bolt.Life -= dt;
                bool done = bolt.Life <= 0f;
                if (!done && Physics.Linecast(from, to, out RaycastHit wall, WallMask, QueryTriggerInteraction.Ignore))
                {
                    to = wall.point;
                    done = true;
                }
                if (!done && bolt.Hostile && Phase == Phase.Playing && Vector3.Distance(to, playerCentre) < 1f)
                {
                    HurtPlayer(bolt.Damage, "Cursed by a skeleton mage.");
                    done = true;
                }
                if (!done && !bolt.Hostile)
                {
                    foreach (EnemyController enemy in enemies)
                    {
                        if (enemy.Brain.IsDead) continue;
                        if (Vector3.Distance(to, enemy.transform.position + Vector3.up * 1f) < 1.2f)
                        {
                            DamageEnemy(enemy, bolt.Damage, from);
                            done = true;
                            break;
                        }
                    }
                    if (!done && artefact != null && !artefact.Destroyed && Vector3.Distance(to, level.ArtefactCore.position) < 2.4f)
                    {
                        DamageArtefact(bolt.Damage, to);
                        done = true;
                    }
                }
                bolt.transform.position = to;
                if (done)
                {
                    Fx.Burst(to, bolt.Colour, 14, 4f, 0.3f, 0.4f, 0.3f);
                    if (!bolt.Hostile) Fx.Flash(to, bolt.Colour, 2f, 6f, 0.2f);
                    Destroy(bolt.gameObject);
                    projectiles.RemoveAt(i);
                }
            }
        }

        // ---- World interactions ----

        void Interactions(Commands commands)
        {
            Vector3 p = player.transform.position;
            string prompt = null;
            bool use = commands.Interact;

            foreach (PickupView pickup in level.Pickups)
            {
                if (pickup.Taken || Distance2(p, pickup.Position) > PickupRadius || Mathf.Abs(p.y - pickup.Position.y) > 2f) continue;
                Collect(pickup);
            }

            for (int i = 0; i < level.Doors.Count; i++)
            {
                PassageView door = level.Doors[i];
                if (door.Open || Distance2(p, door.Position) > 3.2f) continue;
                if (puzzle.TryUnlock(i))
                {
                    level.OpenPassage(door);
                    audioDirector.Play("unlock", 0.9f);
                    hud.Toast("The door unlocks", Fx.Hex("#c8dcff"));
                }
                else
                {
                    prompt = "Locked - find a key";
                }
            }

            for (int i = 0; i < level.Switches.Count; i++)
            {
                SwitchView lever = level.Switches[i];
                if (lever.Active) continue;
                float distance = Distance2(p, lever.Position);
                if (lever.IsPlate)
                {
                    if (distance < 1.7f && player.Grounded)
                    {
                        Activate(i, lever);
                    }
                }
                else if (distance < UseRadius)
                {
                    prompt = input.UsingGamepad ? "Y  Pull the lever" : "E  Pull the lever";
                    if (use)
                    {
                        player.FaceTowards(lever.Position);
                        player.Interact();
                        Activate(i, lever);
                        use = false;
                    }
                }
            }

            foreach (WardView ward in level.Wards)
            {
                if (ward.Purified || Distance2(p, ward.Position) > UseRadius + 0.4f) continue;
                prompt = input.UsingGamepad ? "Y  Purify the brazier" : "E  Purify the brazier";
                if (use)
                {
                    use = false;
                    player.FaceTowards(ward.Position);
                    player.Interact();
                    level.PurifyWard(ward);
                    Fx.Burst(ward.Position + Vector3.up * 2.5f, Fx.Hex("#7ff3ff"), 40, 5f, 0.35f, 0.8f, -0.2f);
                    audioDirector.Play("purify", 1f, 0f);
                    if (artefact.PurifyWard())
                    {
                        level.ArtefactShield.SetActive(false);
                        Fx.Ring(level.ArtefactPosition, Fx.Hex("#d678ff"), 8f);
                        hud.Banner("The ward is broken!", 2.5f);
                        audioDirector.Play("ward_break", 1f, 0f);
                        cameraRig.Shake(0.6f);
                    }
                    else
                    {
                        hud.Toast($"{artefact.WardsRemaining} {(artefact.WardsRemaining == 1 ? "brazier still burns" : "braziers still burn")}", Fx.Hex("#7ff3ff"));
                    }
                }
            }

            UpdateTeleport(p);

            string signText = null;
            foreach (SignView sign in level.Signs)
            {
                if (Distance2(p, sign.Position) < 3.8f) signText = sign.Text;
            }
            hud.SetSign(signText);
            hud.SetPrompt(prompt);

            if (artefact == null && Distance2(p, level.ExitPosition) < 1.8f && Mathf.Abs(p.y) < 1.5f)
            {
                CompleteLevel();
            }
        }

        void Collect(PickupView pickup)
        {
            Vector3 at = pickup.Position + Vector3.up * 0.8f;
            switch (pickup.Cell.Kind)
            {
                case PlacementKind.Coin:
                    run.AddCoin();
                    audioDirector.Play("coin", 0.6f);
                    Fx.Burst(at, Fx.Hex("#ffd65c"), 10, 3f, 0.2f, 0.4f, 0.3f);
                    break;
                case PlacementKind.Candy:
                    run.AddCandy();
                    audioDirector.Play("candy", 0.7f);
                    Fx.Burst(at, Fx.Hex("#ff9a3d"), 12, 3f, 0.25f, 0.5f, 0.3f);
                    break;
                case PlacementKind.Key:
                    puzzle.PickUpKey();
                    audioDirector.Play("key", 0.9f, 0f);
                    hud.Toast("You found a key", Fx.Hex("#c8dcff"));
                    Fx.Burst(at, Fx.Hex("#c8dcff"), 16, 3f, 0.25f, 0.5f, 0f);
                    break;
                case PlacementKind.Relic:
                    run.AddRelic();
                    audioDirector.Play("treasure", 1f, 0f);
                    hud.Toast($"Treasure! +{Tuning.RelicScore}", Fx.Hex("#ffd65c"));
                    Fx.Burst(at, Fx.Hex("#ffd65c"), 50, 6f, 0.3f, 0.9f, 0.6f);
                    break;
                case PlacementKind.Potion:
                    var kind = (PotionKind)pickup.Cell.Variant;
                    if (!player.Vitals.Drink(kind))
                    {
                        return;
                    }
                    audioDirector.Play("potion", 0.8f);
                    hud.Toast(PotionText(kind), PotionColour(kind));
                    Fx.Motes(null, player.transform.position, PotionColour(kind), 0.8f, 50f, false);
                    break;
            }
            pickup.Taken = true;
            pickup.Root.SetActive(false);
        }

        static string PotionText(PotionKind kind)
        {
            switch (kind)
            {
                case PotionKind.Mana: return "Mana restored";
                case PotionKind.Stamina: return "Stamina restored";
                case PotionKind.Elixir: return "Elixir! Every limit rises";
                default: return "Health restored";
            }
        }

        static Color PotionColour(PotionKind kind)
        {
            switch (kind)
            {
                case PotionKind.Mana: return Fx.Hex("#8fb0ff");
                case PotionKind.Stamina: return Fx.Hex("#7dffb0");
                case PotionKind.Elixir: return Fx.Hex("#ffd65c");
                default: return Fx.Hex("#ff6b7a");
            }
        }

        void Activate(int index, SwitchView lever)
        {
            IReadOnlyList<int> opened = puzzle.Activate(index);
            level.SetSwitch(lever, true);
            audioDirector.Play(lever.IsPlate ? "plate" : "lever", 0.9f, 0f);
            if (opened.Count > 0)
            {
                foreach (int gate in opened)
                {
                    level.OpenPassage(level.Gates[gate]);
                }
                audioDirector.Play("gate", 0.9f, 0f);
                hud.Toast(opened.Count == 1 ? "A gate grinds open" : "The gates grind open", Fx.Hex("#efe6cf"));
                cameraRig.Shake(0.3f);
            }
            else
            {
                int remaining = puzzle.Remaining(lever.Cell.Variant);
                hud.Toast(remaining > 0 ? $"Something clicked... {remaining} more" : "Something clicked", Fx.Hex("#efe6cf"));
            }
        }

        void UpdateTeleport(Vector3 p)
        {
            if (padLock != null && Distance2(p, padLock.Position) > 2.2f)
            {
                padLock = null;
            }
            if (padLock != null)
            {
                return;
            }
            foreach (PadView pad in level.Pads)
            {
                if (Distance2(p, pad.Position) > 1.3f || Mathf.Abs(p.y) > 1.5f) continue;
                foreach (PadView twin in level.Pads)
                {
                    if (twin == pad || twin.Cell.Variant != pad.Cell.Variant) continue;
                    Fx.Burst(p + Vector3.up, Fx.Hex("#b46bff"), 30, 4f, 0.3f, 0.6f, -0.3f);
                    player.Teleport(twin.Position + Vector3.up * 0.1f, player.Facing);
                    Fx.Burst(twin.Position + Vector3.up, Fx.Hex("#b46bff"), 30, 4f, 0.3f, 0.6f, -0.3f);
                    cameraRig.Follow(player.transform, true);
                    audioDirector.Play("teleport", 0.9f, 0f);
                    padLock = twin;
                    return;
                }
            }
        }

        void Hazards()
        {
            Vector3 p = player.transform.position;
            foreach (SpikeView trap in level.SpikeTraps)
            {
                if (trap.Raised && Distance2(p, trap.Position) < 1.9f && p.y < 1.2f)
                {
                    HurtPlayer(20f, "Impaled on a spike trap.");
                }
            }
            if (Invincible)
            {
                return;
            }
            Tile under = level.TileAt(p);
            if (under == Tile.Lava && p.y < -0.35f && level.PlatformUnder(p + Vector3.up * 0.3f) == null)
            {
                deathCause = "Swallowed by the lava.";
                Fx.Burst(p, Fx.Hex("#ff8a2a"), 50, 6f, 0.5f, 1f, 0.4f);
                audioDirector.Play("burn", 1f, 0f);
                player.Die();
            }
            else if (p.y < Tuning.FallDeathDepth)
            {
                deathCause = "Lost in the abyss.";
                player.Die();
            }
        }

        void UpdateArtefact(float dt)
        {
            if (artefact == null || artefact.Destroyed)
            {
                return;
            }
            int summoned = 0;
            foreach (EnemyController enemy in enemies)
            {
                if (enemy.Summoned && !enemy.Brain.IsDead) summoned++;
            }
            if (artefact.Tick(dt, summoned) > 0)
            {
                Vector3? spot = SummonSpot();
                if (spot.HasValue)
                {
                    EnemyController minion = SpawnEnemy(EnemyKind.Minion, spot.Value);
                    minion.Summoned = true;
                    Fx.Burst(spot.Value + Vector3.up, Fx.Hex("#7a2aa0"), 30, 3f, 0.5f, 0.9f, -0.3f);
                    audioDirector.Play("summon", 0.8f, 0f);
                }
            }
        }

        Vector3? SummonSpot()
        {
            LevelMap.CellOf(LevelView.Flat(level.ArtefactPosition), out int ax, out int ay);
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int x = ax + Random.Range(-4, 5);
                int y = ay + Random.Range(1, 6);
                Vector3 spot = LevelView.World(x, y);
                if (level.Map.IsWalkable(x, y) && Distance2(spot, player.transform.position) > 5f && Distance2(spot, level.ArtefactPosition) > 3f)
                {
                    return spot;
                }
            }
            return null;
        }

        // ---- Endings ----

        public void CompleteLevel()
        {
            if (Phase != Phase.Playing)
            {
                return;
            }
            audioDirector.Play("level_complete", 1f, 0f);
            hud.Banner("Level complete", 2f);
            Finish(Phase.LevelComplete);
        }

        void Finish(Phase ending)
        {
            Phase = ending;
            phaseTime = 0f;
            player.Celebrate();
            hud.SetPrompt(null);
            hud.SetSign(null);
            sessionScore += run.Score;
            sessionTime += run.Elapsed;
            lastRecord = Save.RecordFinish(LevelIndex, run.Elapsed, run.Score);
            store.Store(Save.Serialize());
            if (ending == Phase.Victory)
            {
                audioDirector.StopMusic();
                audioDirector.Play("victory", 1f, 0f);
            }
        }

        SaveData.Record lastRecord;

        void GameOver(string cause)
        {
            Phase = Phase.GameOver;
            phaseTime = 0f;
            deathCause = cause;
            hud.SetPrompt(null);
            hud.SetSign(null);
            hud.Banner("You died", 2f);
            audioDirector.Play("game_over", 1f, 0f);
            cameraRig.Shake(0.5f);
        }

        void ShowResults()
        {
            LevelDefinition definition = Levels.All[LevelIndex];
            string time = $"Time  {SaveData.FormatTime(run.Elapsed)}   (par {SaveData.FormatTime(definition.ParSeconds)}, bonus +{run.TimeBonus:N0})";
            string best = $"Best  {SaveData.FormatTime(Save.BestTime(LevelIndex))}";
            string loot = $"Coins {run.Coins}   Candy {run.Candy}   Treasure {run.Relics}";
            string kills = $"Skeletons destroyed  {run.Kills}";
            switch (Phase)
            {
                case Phase.LevelComplete:
                    string record = lastRecord.NewBestTime ? "New best time!" : lastRecord.NewBestScore ? "New best score!" : string.Empty;
                    hud.ShowResults("Level complete", $"Next: {Levels.All[LevelIndex + 1].Name}",
                        new[] { time, best, loot, kills, $"Score  {run.Score:N0}" }, record, true, "Next level");
                    break;
                case Phase.Victory:
                    hud.ShowResults("The darkness lifts", "The artefact is shattered and the dungeon falls silent. The explorer climbs back into the moonlight.",
                        new[] { time, best, loot, kills, $"Score  {run.Score:N0}" },
                        sessionScore > run.Score ? $"Whole descent: {SaveData.FormatTime(sessionTime)} - {sessionScore:N0} points" : (lastRecord.NewBestTime ? "New best time!" : string.Empty),
                        true, "Play again", false);
                    break;
                case Phase.GameOver:
                    hud.ShowResults("You died", deathCause ?? string.Empty,
                        new[] { $"Time  {SaveData.FormatTime(run.Elapsed)}", string.Empty, loot, kills }, string.Empty, false, string.Empty);
                    break;
            }
            audioDirector.PlayMusic(art.TitleMusic);
        }

        // ---- HUD ----

        void UpdateHud()
        {
            if (run == null)
            {
                return;
            }
            hud.SetVitals(player.Vitals);
            hud.SetSpells(player.Spells, player.Vitals);
            hud.SetRun(run, puzzle.Keys);
            hud.SetArtefact(artefact);
        }

        static float Distance2(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // ---- Hooks for tests and screenshots ----

        public void PlacePlayer(Vector3 position, Vector3 facing)
        {
            player.Teleport(position, facing);
            cameraRig.Follow(player.transform, true);
        }

        public void KillPlayerNow(string cause = "Struck down.")
        {
            deathCause = cause;
            player.Die();
        }

        /// <summary>Walks the explorer onto the stairs (or shatters the artefact on the last level).</summary>
        public void FinishLevelNow()
        {
            if (artefact != null)
            {
                foreach (WardView ward in level.Wards)
                {
                    if (!ward.Purified)
                    {
                        level.PurifyWard(ward);
                        artefact.PurifyWard();
                    }
                }
                level.ArtefactShield.SetActive(false);
                DamageArtefact(Artefact.MaxHealth, level.ArtefactCore.position);
            }
            else
            {
                PlacePlayer(level.ExitPosition + Vector3.up * 0.1f, Vector3.forward);
                CompleteLevel();
            }
        }

        public void PurifyAllWardsNow()
        {
            foreach (WardView ward in level.Wards)
            {
                if (!ward.Purified)
                {
                    level.PurifyWard(ward);
                    if (artefact.PurifyWard())
                    {
                        level.ArtefactShield.SetActive(false);
                    }
                }
            }
        }
    }
}
