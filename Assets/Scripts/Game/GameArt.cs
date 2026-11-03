using UnityEngine;

namespace SpookyPath.Game
{
    /// <summary>
    /// Every model, material and sound the game builds levels from. Created and filled by
    /// ProjectBuilder (Spooky Path > Regenerate Project) so the scene holds one reference.
    /// Models are KayKit packs by Kay Lousberg (CC0); materials and sounds are generated.
    /// </summary>
    public sealed class GameArt : ScriptableObject
    {
        [Header("Characters")]
        public GameObject Explorer;
        public RuntimeAnimatorController ExplorerAnimator;
        /// <summary>Minion, warrior and mage skeletons (static parts animated in code).</summary>
        public GameObject[] Skeletons = new GameObject[3];
        public GameObject[] BrokenSkeletons = new GameObject[3];

        [Header("Dungeon")]
        public GameObject Floor;
        public GameObject FloorCracked;
        public GameObject Foundation;
        public GameObject Wall;
        public GameObject WallCracked;
        public GameObject WallArched;
        public GameObject TorchMounted;
        public GameObject Stairs;
        public GameObject DoorLeaf;
        public GameObject GateLeaf;
        public GameObject Grate;
        public GameObject Spikes;
        public GameObject Arch;
        public GameObject Gravestone;
        public GameObject Shrine;
        public GameObject ArtefactModel;
        public GameObject[] Props = new GameObject[0];

        [Header("Pickups")]
        public GameObject Coin;
        public GameObject Candy;
        public GameObject Key;
        public GameObject Chest;
        /// <summary>Health, mana, stamina and elixir bottles, in <see cref="SpookyPath.Core.PotionKind"/> order.</summary>
        public GameObject[] Potions = new GameObject[4];

        [Header("Materials")]
        public Material Lava;
        public Material Rock;
        public Material Stone;
        public Material Iron;
        public Material Rune;
        public Material RuneLit;
        public Material Ember;
        public Material Curse;
        public Material ShieldBubble;
        public Material DarkAura;
        public Material ParticleAdditive;
        public Material ParticleAlpha;
        public Material BlobShadow;

        [Header("Meshes")]
        public Mesh Cube;
        public Mesh Sphere;
        public Mesh Cylinder;
        public Mesh Quad;

        [Header("Audio")]
        public AudioClip TitleMusic;
        public AudioClip DungeonMusic;
        public AudioClip FinalMusic;
        public AudioClip[] Effects = new AudioClip[0];
    }
}
