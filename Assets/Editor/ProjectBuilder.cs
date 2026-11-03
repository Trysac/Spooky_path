using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpookyPath.Game;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace SpookyPath.EditorTools
{
    /// <summary>
    /// Generates the materials, textures, animator, art asset, icon, scene and player settings from
    /// code, and builds the players, so every generated asset can be recreated from text. Batch mode:
    /// <code>
    /// Unity -batchmode -quit -projectPath . -executeMethod SpookyPath.EditorTools.ProjectBuilder.SetUpProject
    /// Unity -batchmode -quit -projectPath . -executeMethod SpookyPath.EditorTools.ProjectBuilder.BuildWindows -buildOutput &lt;folder&gt;
    /// Unity -batchmode -quit -projectPath . -executeMethod SpookyPath.EditorTools.ProjectBuilder.BuildWebGL -buildOutput &lt;folder&gt;
    /// </code>
    /// </summary>
    public static class ProjectBuilder
    {
        const string ScenePath = "Assets/Scenes/Game.unity";
        const string ArtFolder = "Assets/Art";
        const string MaterialFolder = "Assets/Art/Materials";
        const string ArtPath = "Assets/Art/GameArt.asset";
        const string ControllerPath = "Assets/Art/Explorer.controller";
        const string IconPath = "Assets/Art/AppIcon.png";
        const string AudioFolder = "Assets/Audio";
        const string LayoutPath = "Assets/UI/Hud.uxml";
        const string ThemePath = "Assets/UI/HudTheme.tss";
        const string PanelSettingsPath = "Assets/UI/HudPanelSettings.asset";

        const string Remastered = "Assets/Models/DungeonRemastered/Assets/fbx/";
        const string Dungeon = "Assets/Models/Dungeon/Models/fbx/";
        const string Halloween = "Assets/Models/HalloweenBits/Assets/fbx(unity)/";
        const string Skeletons = "Assets/Models/Character_Skeletons/Models/characters/fbx/";
        const string Spooktober = "Assets/Models/Spooktober/Models/fbx/";
        const string Adventurers = "Assets/Models/Adventurers/Characters/fbx/";

        static readonly string[] LoopingClips = { "Idle", "Walking_A", "Running_A", "Jump_Idle" };

        static readonly (string State, string Clip, float Speed)[] ExplorerStates =
        {
            ("Idle", "Idle", 1f),
            ("Walk", "Walking_A", 1.1f),
            ("Run", "Running_A", 1.1f),
            ("Jump", "Jump_Idle", 1f),
            ("Strike", "2H_Melee_Attack_Chop", 2.2f),
            ("Cast", "Spellcast_Shoot", 1.8f),
            ("Hit", "Hit_A", 1.4f),
            ("Die", "Death_A", 1f),
            ("Interact", "Interact", 1.6f),
            ("Cheer", "Cheer", 1f),
        };

        [MenuItem("Spooky Path/Regenerate Project")]
        public static void SetUpProject()
        {
            Directory.CreateDirectory(MaterialFolder);
            ConfigureExplorerClips();
            GameArt art = CreateArt();
            PanelSettings panel = CreatePanelSettings();
            CreateScene(art, panel);
            ConfigurePlayer(CreateIcon());
            AssetDatabase.SaveAssets();
            Debug.Log("Spooky Path project set up.");
        }

        /// <summary>
        /// Enters play mode and lets ScreenshotScenarios capture the README screenshots, then exits.
        /// Run in batch mode without -quit: <c>-executeMethod ...CaptureScreenshots -screenshot &lt;folder&gt;</c>.
        /// </summary>
        public static void CaptureScreenshots()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "SpookyPath.exe");

        public static void BuildWebGL() => Build(BuildTarget.WebGL, string.Empty);

        // ---- Models and animation ----

        static GameObject Model(string path)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
            {
                throw new FileNotFoundException($"Missing model {path}");
            }
            return model;
        }

        static void ConfigureExplorerClips()
        {
            string path = Adventurers + "Mage.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            bool changed = importer.clipAnimations.Length == 0;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                bool loop = LoopingClips.Contains(clip.name);
                if (clip.loopTime != loop)
                {
                    clip.loopTime = loop;
                    changed = true;
                }
            }
            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        static RuntimeAnimatorController CreateExplorerController()
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(Adventurers + "Mage.fbx").OfType<AnimationClip>().ToArray();
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach ((string state, string clipName, float speed) in ExplorerStates)
            {
                AnimationClip clip = clips.FirstOrDefault(c => c.name == clipName);
                if (clip == null)
                {
                    throw new InvalidOperationException($"Mage.fbx has no clip {clipName}");
                }
                AnimatorState animatorState = machine.AddState(state);
                animatorState.motion = clip;
                animatorState.speed = speed;
                if (state == "Idle")
                {
                    machine.defaultState = animatorState;
                }
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // ---- Art ----

        static GameArt CreateArt()
        {
            var art = AssetDatabase.LoadAssetAtPath<GameArt>(ArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<GameArt>();
                AssetDatabase.CreateAsset(art, ArtPath);
            }

            art.Explorer = Model(Adventurers + "Mage.fbx");
            art.ExplorerAnimator = CreateExplorerController();
            art.Skeletons = new[]
            {
                Model(Skeletons + "character_skeleton_minion.fbx"),
                Model(Skeletons + "character_skeleton_warrior.fbx"),
                Model(Skeletons + "character_skeleton_mage.fbx"),
            };
            art.BrokenSkeletons = new[]
            {
                Model(Skeletons + "character_skeleton_minion_broken.fbx"),
                Model(Skeletons + "character_skeleton_warrior_broken.fbx"),
                Model(Skeletons + "character_skeleton_mage_broken.fbx"),
            };

            art.Floor = Model(Remastered + "floor_tile_large.fbx");
            art.FloorCracked = Model(Remastered + "floor_tile_large_rocks.fbx");
            art.Foundation = Model(Remastered + "floor_foundation_allsides.fbx");
            art.Wall = Model(Remastered + "wall.fbx");
            art.WallCracked = Model(Remastered + "wall_cracked.fbx");
            art.WallArched = Model(Remastered + "wall_arched.fbx");
            art.TorchMounted = Model(Remastered + "torch_mounted.fbx");
            art.Stairs = Model(Dungeon + "stairs_wide.fbx");
            art.DoorLeaf = Model(Dungeon + "door.fbx");
            art.GateLeaf = Model(Dungeon + "door_gate.fbx");
            art.Grate = Model(Remastered + "floor_tile_big_grate.fbx");
            art.Spikes = Model(Remastered + "floor_tile_big_spikes.fbx");
            art.Arch = Model(Halloween + "arch_gate.fbx");
            art.Gravestone = Model(Halloween + "gravestone.fbx");
            art.Shrine = Model(Halloween + "shrine_candles.fbx");
            art.ArtefactModel = Model(Dungeon + "artifact.fbx");
            art.Props = new[]
            {
                Model(Remastered + "barrel_large.fbx"),
                Model(Remastered + "box_large.fbx"),
                Model(Remastered + "crates_stacked.fbx"),
                Model(Remastered + "candle_triple.fbx"),
                Model(Halloween + "pumpkin_orange_jackolantern.fbx"),
                Model(Halloween + "skull_candle.fbx"),
                Model(Halloween + "coffin.fbx"),
                Model(Halloween + "bone_A.fbx"),
                Model(Remastered + "barrel_small_stack.fbx"),
                Model(Halloween + "pumpkin_yellow_small.fbx"),
            };

            art.Coin = Model(Remastered + "coin.fbx");
            art.Candy = Model(Spooktober + "candyCorn.fbx");
            art.Key = Model(Remastered + "key.fbx");
            art.Chest = Model(Remastered + "chest_gold.fbx");
            art.Potions = new[]
            {
                Model(Dungeon + "potionLarge_red.fbx"),
                Model(Dungeon + "potionLarge_blue.fbx"),
                Model(Dungeon + "potionLarge_green.fbx"),
                Model(Remastered + "bottle_A_labeled_green.fbx"),
            };

            Texture2D dot = SoftDot();
            art.Lava = LavaMaterial();
            art.Rock = Lit("Rock", "#17131c", 0.05f);
            art.Stone = Lit("Stone", "#6a6470", 0.15f);
            art.Iron = Lit("Iron", "#2b2e35", 0.55f, 0.6f);
            art.Rune = Glowing("Rune", "#3a1d55", "#7a2aa0", 1.2f);
            art.RuneLit = Glowing("RuneLit", "#7ff3ff", "#3fd8ff", 2.2f);
            art.Ember = Glowing("Ember", "#ffd27a", "#ff7a1a", 3f);
            art.Curse = Glowing("Curse", "#e3a0ff", "#b030ff", 3f);
            art.ShieldBubble = Particle("ShieldBubble", dot, true, new Color(0.22f, 0.16f, 0.3f, 0.3f), "Legacy Shaders/Particles/Additive");
            art.ShieldBubble.mainTexture = null;
            art.DarkAura = Particle("DarkAura", dot, false, new Color(0.15f, 0f, 0.2f, 0.6f), "Legacy Shaders/Particles/Alpha Blended");
            art.ParticleAdditive = Particle("ParticleAdditive", dot, true, new Color(0.5f, 0.5f, 0.5f, 0.5f), "Legacy Shaders/Particles/Additive");
            art.ParticleAlpha = Particle("ParticleAlpha", dot, false, new Color(0.5f, 0.5f, 0.5f, 0.5f), "Legacy Shaders/Particles/Alpha Blended");
            art.BlobShadow = Particle("BlobShadow", dot, false, new Color(0f, 0f, 0f, 0.5f), "Legacy Shaders/Particles/Alpha Blended");

            art.Cube = PrimitiveMesh(PrimitiveType.Cube);
            art.Sphere = PrimitiveMesh(PrimitiveType.Sphere);
            art.Cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
            art.Quad = PrimitiveMesh(PrimitiveType.Quad);

            art.TitleMusic = Clip("title_theme", true);
            art.DungeonMusic = Clip("dungeon_theme", true);
            art.FinalMusic = Clip("final_theme", true);
            var effects = new List<AudioClip>();
            foreach (string file in Directory.GetFiles(AudioFolder, "*.ogg"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!name.EndsWith("_theme"))
                {
                    AudioClip clip = Clip(name, false);
                    if (clip != null) effects.Add(clip);
                }
            }
            art.Effects = effects.ToArray();

            EditorUtility.SetDirty(art);
            return art;
        }

        static Mesh PrimitiveMesh(PrimitiveType type)
        {
            GameObject probe = GameObject.CreatePrimitive(type);
            Mesh mesh = probe.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(probe);
            return mesh;
        }

        static Material MaterialAt(string name, Shader shader)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            return material;
        }

        static Material Lit(string name, string color, float smoothness, float metallic = 0f)
        {
            Material material = MaterialAt(name, Shader.Find("Standard"));
            material.color = Hex(color);
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Glowing(string name, string color, string glow, float intensity)
        {
            Material material = Lit(name, color, 0.4f);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor("_EmissionColor", Hex(glow) * intensity);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Particle(string name, Texture2D texture, bool additive, Color tint, string shaderName)
        {
            Material material = MaterialAt(name, Shader.Find(shaderName));
            material.mainTexture = texture;
            material.SetColor("_TintColor", tint);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>A soft white disc for every particle, glow and blob shadow.</summary>
        static Texture2D SoftDot()
        {
            const string path = ArtFolder + "/SoftDot.png";
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha * (3f - 2f * alpha)));
                }
            }
            return SaveTexture(texture, path, TextureWrapMode.Clamp, true);
        }

        /// <summary>Molten rock: fractal noise through a black-red-orange-yellow ramp, with a matching glow map.</summary>
        static Material LavaMaterial()
        {
            const int size = 256;
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var glow = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = Fbm(x / (float)size, y / (float)size);
                    // Bright veins where the noise crosses the middle, dark crust elsewhere.
                    float vein = Mathf.Pow(1f - Mathf.Abs(n - 0.5f) * 2f, 5f);
                    float heat = Mathf.Clamp01(n * 0.6f + vein);
                    float bright = Mathf.Clamp01((heat - 0.35f) * 1.6f);
                    Color crust = Color.Lerp(Hex("#120403"), Hex("#3a0c05"), n);
                    Color hot = Color.Lerp(Hex("#c02a06"), Hex("#ffb347"), vein);
                    albedo.SetPixel(x, y, Color.Lerp(crust, hot, bright));
                    glow.SetPixel(x, y, Color.Lerp(Color.black, hot, bright * bright));
                }
            }
            Texture2D albedoAsset = SaveTexture(albedo, ArtFolder + "/Lava.png", TextureWrapMode.Repeat, false);
            Texture2D glowAsset = SaveTexture(glow, ArtFolder + "/LavaGlow.png", TextureWrapMode.Repeat, false);
            Material material = MaterialAt("Lava", Shader.Find("Standard"));
            material.color = Color.white;
            material.mainTexture = albedoAsset;
            material.SetFloat("_Glossiness", 0.3f);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetTexture("_EmissionMap", glowAsset);
            material.SetColor("_EmissionColor", Color.white * 0.9f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Tileable fractal value noise in [0, 1].</summary>
        static float Fbm(float u, float v)
        {
            float sum = 0f, amplitude = 0.5f;
            int period = 4;
            for (int octave = 0; octave < 5; octave++)
            {
                sum += amplitude * ValueNoise(u * period, v * period, period);
                amplitude *= 0.5f;
                period *= 2;
            }
            return Mathf.Clamp01(sum / 0.97f);
        }

        static float ValueNoise(float x, float y, int period)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Lattice(x0, y0, period), b = Lattice(x0 + 1, y0, period);
            float c = Lattice(x0, y0 + 1, period), d = Lattice(x0 + 1, y0 + 1, period);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Lattice(int x, int y, int period)
        {
            x = ((x % period) + period) % period;
            y = ((y % period) + period) % period;
            uint h = (uint)(x * 374761393 + y * 668265263 + period * 982451653);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 0xffff) / 65535f;
        }

        static Texture2D SaveTexture(Texture2D texture, string path, TextureWrapMode wrap, bool alpha)
        {
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = alpha;
            importer.wrapMode = wrap;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static AudioClip Clip(string name, bool music)
        {
            string path = $"{AudioFolder}/{name}.ogg";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Missing {path}: run python tools/audio/build.py first.");
                return null;
            }
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---- UI ----

        static PanelSettings CreatePanelSettings()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.clearColor = false;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        // ---- Scene ----

        static void CreateScene(GameArt art, PanelSettings panel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Color night = Hex("#0b0812");

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = night;
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 120f;
            cameraObject.AddComponent<AudioListener>();
            CameraRig rig = cameraObject.AddComponent<CameraRig>();

            var moonObject = new GameObject("Moonlight");
            var moon = moonObject.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = Hex("#8f9cff");
            moon.intensity = 0.45f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.7f;
            moonObject.transform.rotation = Quaternion.Euler(58f, -30f, 0f);

            RenderSettings.skybox = null;
            RenderSettings.sun = moon;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#3a3456");
            RenderSettings.ambientEquatorColor = Hex("#2a2236");
            RenderSettings.ambientGroundColor = Hex("#1a1020");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = night;
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 70f;

            var ui = new GameObject("HUD");
            var document = ui.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            Hud hud = ui.AddComponent<Hud>();

            var game = new GameObject("Game");
            var director = game.AddComponent<GameDirector>();
            var serialized = new SerializedObject(director);
            serialized.FindProperty("art").objectReferenceValue = art;
            serialized.FindProperty("hud").objectReferenceValue = hud;
            serialized.FindProperty("cameraRig").objectReferenceValue = rig;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer(Texture2D icon)
        {
            PlayerSettings.companyName = "Trysac";
            PlayerSettings.productName = "Spooky Path";
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            int[] sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any);
            var standaloneIcons = new Texture2D[sizes.Length];
            Array.Fill(standaloneIcons, icon);
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, standaloneIcons, IconKind.Any);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            // Static hosts such as GitHub Pages do not send Content-Encoding for .br/.gz files.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
        }

        // ---- Icon: a lantern-lit path winding up to a skull moon ----

        static Texture2D CreateIcon()
        {
            const int size = 1024;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = IconPixel(x + 0.5f - size / 2f, y + 0.5f - size / 2f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(IconPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        }

        static Color IconPixel(float x, float y)
        {
            const float aa = 1.5f;
            float tile = RoundedBox(x, y, 470f, 470f, 180f);
            Color color = Color.Lerp(Hex("#0d0a14"), Hex("#2c1640"), Mathf.Clamp01((y + 470f) / 940f));
            color.a = Coverage(-tile, aa);
            float inside = Coverage(-tile, aa);

            // A pale moon with a halo.
            float moon = Length(x - 150f, y - 200f) - 150f;
            color = Over(color, Hex("#ff9a3d"), Mathf.Clamp01(1f - moon / 140f) * 0.25f * inside);
            color = Over(color, Hex("#efe6cf"), Coverage(-moon, aa) * inside);
            // Skull face carved into the moon: two eyes and a nose.
            color = Over(color, Hex("#2c1640"), Coverage(-(Length(x - 95f, y - 220f) - 34f), aa) * inside);
            color = Over(color, Hex("#2c1640"), Coverage(-(Length(x - 205f, y - 220f) - 34f), aa) * inside);
            color = Over(color, Hex("#2c1640"), Coverage(-(RoundedBox(x - 150f, y - 150f, 14f, 22f, 8f)), aa) * inside);

            // A winding path of stepping stones from the bottom edge.
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                float px = Mathf.Sin(t * 5.5f) * 170f - 80f;
                float py = -420f + t * 470f;
                float stone = RoundedBox(x - px, y - py, 95f - i * 9f, 34f - i * 3f, 18f);
                color = Over(color, Hex("#050308"), Coverage(-(stone - 10f), aa) * inside);
                color = Over(color, Hex("#ff9a3d"), Coverage(-stone, aa) * inside);
            }

            // A lantern flame at the top of the path.
            float flame = Length(x + 170f, (y - 110f) * 0.7f) - 34f;
            color = Over(color, Hex("#ffd27a"), Mathf.Clamp01(1f - flame / 90f) * 0.5f * inside);
            color = Over(color, Hex("#ffd27a"), Coverage(-flame, aa) * inside);
            return color;
        }

        static float Length(float x, float y) => Mathf.Sqrt(x * x + y * y);

        static float RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            float qx = Mathf.Abs(x) - (halfWidth - radius);
            float qy = Mathf.Abs(y) - (halfHeight - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static float Coverage(float inside, float width) => Mathf.Clamp01(inside / width + 0.5f);

        static Color Over(Color below, Color above, float amount)
        {
            Color mixed = Color.Lerp(below, above, amount);
            mixed.a = below.a;
            return mixed;
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;

        // ---- Builds ----

        static void Build(BuildTarget target, string fileName)
        {
            string folder = Argument("-buildOutput");
            if (string.IsNullOrEmpty(folder))
            {
                throw new ArgumentException("Pass -buildOutput <folder> outside the repository.");
            }
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = target,
                locationPathName = string.IsNullOrEmpty(fileName) ? folder : Path.Combine(folder, fileName),
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Build {target}: {summary.result}, {summary.totalSize / (1024 * 1024f):0.0} MB, {summary.totalErrors} errors.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
