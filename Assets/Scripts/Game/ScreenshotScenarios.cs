using System;
using System.Collections;
using System.IO;
using SpookyPath.Core;
using UnityEngine;

namespace SpookyPath.Game
{
    /// <summary>
    /// Opt-in capture of the README screenshots:
    /// <c>SpookyPath.exe -screenshot &lt;folder&gt; -scenario title,tutorial,combat,puzzle,lava,artefact,pause,gameover,victory</c>.
    /// Without -screenshot nothing is created, so normal runs and WebGL builds are unaffected.
    /// The save lives in memory while capturing, so PlayerPrefs are never touched. The same hook
    /// runs inside the editor (ProjectBuilder.CaptureScreenshots), rendering the camera and the HUD
    /// off screen and blending them, for machines that cannot launch the built player.
    /// Adding <c>-frames N [-fps F]</c> records N frames per scenario at a fixed timestep instead of
    /// one shot, into <c>&lt;folder&gt;/&lt;scenario&gt;/frame_0000.png...</c>; the <c>gameplay</c>
    /// scenario has the explorer fight two skeletons while it records (for the README GIF).
    /// </summary>
    public sealed class ScreenshotScenarios : MonoBehaviour
    {
        const string DefaultScenarios = "title,tutorial,combat,puzzle,lava,artefact,pause,gameover,victory";

        const int Width = 1280;
        const int Height = 720;

        string folder;
        string[] scenarios;
        int frames;
        int fps = 15;
        RenderTexture sceneTarget;
        RenderTexture uiTarget;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartWhenRequested()
        {
            string target = Argument("-screenshot");
            if (string.IsNullOrEmpty(target))
            {
                return;
            }
            var host = new GameObject(nameof(ScreenshotScenarios)).AddComponent<ScreenshotScenarios>();
            host.folder = target;
            host.scenarios = (Argument("-scenario") ?? DefaultScenarios).Split(',');
            host.frames = int.TryParse(Argument("-frames"), out int frames) ? frames : 0;
            host.fps = int.TryParse(Argument("-fps"), out int fps) && fps > 0 ? fps : 15;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            var director = FindAnyObjectByType<GameDirector>();
            director.UseMemoryStore("unlocked=4\nmusic=0\neffects=0\ntime0=71.4\ntime1=152.8\n");
            UnityEngine.Random.InitState(7);
            if (Application.isEditor)
            {
                RenderOffscreen(director);
            }
            yield return new WaitForSecondsRealtime(1f);
            foreach (string raw in scenarios)
            {
                string scenario = raw.Trim();
                yield return Stage(director, scenario);
                if (frames > 0)
                {
                    yield return Record(scenario);
                    Time.timeScale = 1f;
                    continue;
                }
                Texture2D shot;
                if (Application.isEditor)
                {
                    yield return null;
                    shot = CaptureOffscreen();
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                    shot = ScreenCapture.CaptureScreenshotAsTexture();
                }
                File.WriteAllBytes(Path.Combine(folder, scenario + ".png"), shot.EncodeToPNG());
                Destroy(shot);
                Debug.Log($"Saved screenshot {scenario}.png");
                Time.timeScale = 1f;
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }

        /// <summary>Points the camera and the HUD panel at render textures of a fixed size.</summary>
        void RenderOffscreen(GameDirector director)
        {
            sceneTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            uiTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            director.CameraRig.GetComponent<Camera>().targetTexture = sceneTarget;
            var document = director.Hud.GetComponent<UnityEngine.UIElements.UIDocument>();
            UnityEngine.UIElements.PanelSettings panel = Instantiate(document.panelSettings);
            panel.targetTexture = uiTarget;
            panel.clearColor = true;
            panel.colorClearValue = Color.clear;
            document.panelSettings = panel;
        }

        /// <summary>Blends the HUD over the 3D view, like the screen would show them.</summary>
        Texture2D CaptureOffscreen()
        {
            Color[] scene = Read(sceneTarget);
            Color[] ui = Read(uiTarget);
            for (int i = 0; i < scene.Length; i++)
            {
                Color over = ui[i];
                Color result = Color.Lerp(scene[i], new Color(over.r, over.g, over.b), over.a);
                result.a = 1f;
                scene[i] = result;
            }
            var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            shot.SetPixels(scene);
            shot.Apply();
            return shot;
        }

        static Color[] Read(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            Color[] pixels = texture.GetPixels();
            Destroy(texture);
            return pixels;
        }

        static IEnumerator Stage(GameDirector director, string scenario)
        {
            director.ScriptedInput = true;
            director.Scripted = Commands.None;
            director.Invincible = false;
            switch (scenario)
            {
                case "title":
                    director.ToTitle();
                    yield return new WaitForSecondsRealtime(2.5f);
                    break;

                case "tutorial":
                    director.StartLevel(0);
                    director.Invincible = true;
                    director.PlacePlayer(LevelView.World(7, 16) + new Vector3(2f, 0.1f, 0.4f), Vector3.left);
                    yield return new WaitForSecondsRealtime(3.6f);
                    break;

                case "combat":
                    director.StartLevel(1);
                    director.Invincible = true;
                    director.PlacePlayer(LevelView.World(13, 9), Vector3.forward);
                    director.SpawnEnemy(EnemyKind.Minion, LevelView.World(11, 7));
                    director.SpawnEnemy(EnemyKind.Mage, LevelView.World(16, 6));
                    yield return new WaitForSecondsRealtime(3.4f);
                    director.Scripted.Spell = (int)SpellKind.FrostNova;
                    yield return new WaitForSecondsRealtime(0.5f);
                    director.Scripted.Spell = (int)SpellKind.Firebolt;
                    yield return new WaitForSecondsRealtime(0.12f);
                    break;

                case "puzzle":
                    director.StartLevel(0);
                    director.Invincible = true;
                    director.Puzzle.PickUpKey();
                    director.PlacePlayer(LevelView.World(4, 6) + new Vector3(1.8f, 0.1f, -0.8f), Vector3.left);
                    yield return new WaitForSecondsRealtime(3.6f);
                    break;

                case "lava":
                    director.StartLevel(2);
                    director.Invincible = true;
                    director.PlacePlayer(LevelView.World(6, 12), Vector3.forward);
                    yield return new WaitForSecondsRealtime(0.3f);
                    PlatformView platform = director.Level.Platforms[0];
                    director.PlacePlayer(platform.Root.position + Vector3.up * 0.2f, Vector3.forward);
                    yield return new WaitForSecondsRealtime(3.6f);
                    break;

                case "artefact":
                    director.StartLevel(3);
                    director.Invincible = true;
                    director.Level.PurifyWard(director.Level.Wards[0]);
                    director.ArtefactState.PurifyWard();
                    director.PlacePlayer(LevelView.World(14, 5), Vector3.forward);
                    director.SpawnEnemy(EnemyKind.Minion, LevelView.World(12, 4));
                    director.SpawnEnemy(EnemyKind.Warrior, LevelView.World(16, 5));
                    yield return new WaitForSecondsRealtime(3.5f);
                    director.Scripted.Spell = (int)SpellKind.Firebolt;
                    yield return new WaitForSecondsRealtime(0.15f);
                    break;

                case "gameplay":
                    director.StartLevel(1);
                    director.Invincible = true;
                    director.PlacePlayer(LevelView.World(13, 9), Vector3.forward);
                    director.SpawnEnemy(EnemyKind.Minion, LevelView.World(11, 7));
                    director.SpawnEnemy(EnemyKind.Warrior, LevelView.World(16, 7));
                    yield return new WaitForSecondsRealtime(3f);
                    director.StartCoroutine(Fight(director));
                    break;

                case "pause":
                    director.StartLevel(1);
                    yield return new WaitForSecondsRealtime(3.5f);
                    director.PauseGame();
                    yield return new WaitForSecondsRealtime(0.4f);
                    break;

                case "gameover":
                    director.StartLevel(2);
                    yield return new WaitForSecondsRealtime(3.2f);
                    director.KillPlayerNow("Swallowed by the lava.");
                    yield return new WaitForSecondsRealtime(3.2f);
                    break;

                case "victory":
                    director.StartLevel(3);
                    director.Invincible = true;
                    director.PlacePlayer(LevelView.World(14, 6), Vector3.forward);
                    yield return new WaitForSecondsRealtime(3.2f);
                    director.FinishLevelNow();
                    yield return new WaitForSecondsRealtime(3.4f);
                    break;

                default:
                    Debug.LogWarning($"Unknown screenshot scenario '{scenario}'.");
                    break;
            }
        }

        /// <summary>Writes the given number of frames at a fixed timestep (Time.captureFramerate).</summary>
        IEnumerator Record(string scenario)
        {
            string target = Path.Combine(folder, scenario);
            Directory.CreateDirectory(target);
            Time.captureFramerate = fps;
            for (int i = 0; i < frames; i++)
            {
                Texture2D shot;
                if (Application.isEditor)
                {
                    yield return null;
                    shot = CaptureOffscreen();
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                    shot = ScreenCapture.CaptureScreenshotAsTexture();
                }
                File.WriteAllBytes(Path.Combine(target, $"frame_{i:0000}.png"), shot.EncodeToPNG());
                Destroy(shot);
            }
            Time.captureFramerate = 0;
            Debug.Log($"Saved {frames} frames of {scenario}");
        }

        /// <summary>
        /// Plays the explorer for the recording: walks up to the nearest skeleton and strikes it with
        /// the staff, opening with a Frost Nova when two are close and a Firebolt from range.
        /// </summary>
        static IEnumerator Fight(GameDirector director)
        {
            float strikeWait = 0f;
            float boltWait = 0.4f;
            bool novaUsed = false;
            while (director.ScriptedInput && director.Phase == Phase.Playing)
            {
                float dt = Time.deltaTime;
                strikeWait -= dt;
                boltWait -= dt;
                Vector3 me = director.Player.transform.position;
                EnemyController target = null;
                float best = float.MaxValue;
                int close = 0;
                foreach (EnemyController enemy in director.Enemies)
                {
                    if (enemy.Brain.IsDead) continue;
                    float distance = Vector3.Distance(Flat(enemy.transform.position), Flat(me));
                    if (distance < 3.5f) close++;
                    if (distance < best)
                    {
                        best = distance;
                        target = enemy;
                    }
                }
                if (target == null)
                {
                    director.Scripted.Move = Vector2.zero;
                    yield return null;
                    continue;
                }
                Vector3 to = Flat(target.transform.position) - Flat(me);
                Vector2 toward = new Vector2(to.x, to.z).normalized;
                if (!novaUsed && close >= 2)
                {
                    director.Scripted.Spell = (int)SpellKind.FrostNova;
                    novaUsed = true;
                }
                else if (best > 4f && boltWait <= 0f)
                {
                    // Face the skeleton for a frame, then throw the bolt.
                    director.Scripted.Move = toward * 0.2f;
                    director.Scripted.Spell = (int)SpellKind.Firebolt;
                    boltWait = 1.6f;
                }
                else if (best > 1.7f)
                {
                    director.Scripted.Move = toward;
                }
                else
                {
                    director.Scripted.Move = toward * 0.05f;
                    if (strikeWait <= 0f)
                    {
                        director.Scripted.Strike = true;
                        strikeWait = 0.45f;
                    }
                }
                yield return null;
            }
            director.Scripted.Move = Vector2.zero;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static IEnumerator Walk(GameDirector director, Vector2 direction, float seconds)
        {
            director.Scripted.Move = direction;
            yield return new WaitForSecondsRealtime(seconds);
            director.Scripted.Move = Vector2.zero;
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
