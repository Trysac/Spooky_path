using System;
using System.Collections.Generic;

namespace SpookyPath.Core
{
    /// <summary>
    /// Levers, pressure plates, gates, keys and locked doors of one level. Switches latch once
    /// activated; a gate opens when every switch on its channel is active.
    /// </summary>
    public sealed class PuzzleBoard
    {
        readonly int[] switchChannels;
        readonly bool[] active;
        readonly int[] gateChannels;
        readonly bool[] open;
        readonly bool[] unlocked;

        public PuzzleBoard(IReadOnlyList<int> switchChannels, IReadOnlyList<int> gateChannels, int doors)
        {
            this.switchChannels = Copy(switchChannels);
            this.gateChannels = Copy(gateChannels);
            active = new bool[this.switchChannels.Length];
            open = new bool[this.gateChannels.Length];
            unlocked = new bool[Math.Max(0, doors)];
        }

        /// <summary>Builds the board for a parsed level, with switches and gates in reading order.</summary>
        public static PuzzleBoard For(LevelMap map)
        {
            var switches = new List<int>();
            var gates = new List<int>();
            foreach (Placement placement in map.Placements)
            {
                if (placement.Kind == PlacementKind.Lever || placement.Kind == PlacementKind.Plate)
                {
                    switches.Add(placement.Variant);
                }
                else if (placement.Kind == PlacementKind.Gate)
                {
                    gates.Add(placement.Variant);
                }
            }
            return new PuzzleBoard(switches, gates, map.All(PlacementKind.Door).Count);
        }

        public int Keys { get; private set; }
        public int SwitchCount => active.Length;
        public int GateCount => open.Length;

        public bool IsActive(int switchIndex) => active[switchIndex];
        public bool IsGateOpen(int gateIndex) => open[gateIndex];
        public bool IsDoorUnlocked(int doorIndex) => unlocked[doorIndex];

        /// <summary>
        /// Activates a switch. Returns the gates this opened (empty when the switch was already
        /// active or its channel still has inactive switches).
        /// </summary>
        public IReadOnlyList<int> Activate(int switchIndex)
        {
            var opened = new List<int>();
            if (active[switchIndex])
            {
                return opened;
            }
            active[switchIndex] = true;
            int channel = switchChannels[switchIndex];
            if (!ChannelComplete(channel))
            {
                return opened;
            }
            for (int gate = 0; gate < open.Length; gate++)
            {
                if (!open[gate] && gateChannels[gate] == channel)
                {
                    open[gate] = true;
                    opened.Add(gate);
                }
            }
            return opened;
        }

        /// <summary>How many switches on a channel are still waiting to be activated.</summary>
        public int Remaining(int channel)
        {
            int remaining = 0;
            for (int i = 0; i < active.Length; i++)
            {
                if (switchChannels[i] == channel && !active[i])
                {
                    remaining++;
                }
            }
            return remaining;
        }

        bool ChannelComplete(int channel) => Remaining(channel) == 0;

        public void PickUpKey() => Keys++;

        /// <summary>Spends a key on a locked door. False when the door is already open or there is no key.</summary>
        public bool TryUnlock(int doorIndex)
        {
            if (unlocked[doorIndex] || Keys <= 0)
            {
                return false;
            }
            Keys--;
            unlocked[doorIndex] = true;
            return true;
        }

        static int[] Copy(IReadOnlyList<int> source)
        {
            if (source == null)
            {
                return new int[0];
            }
            var copy = new int[source.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = source[i];
            }
            return copy;
        }
    }

    /// <summary>
    /// The cursed artefact at the bottom of the dungeon. While any ward still burns it cannot be
    /// harmed; it keeps summoning skeletons, faster once its shield falls, until it shatters.
    /// </summary>
    public sealed class Artefact
    {
        public const float MaxHealth = 400f;
        public const float ShieldedSummonInterval = 14f;
        public const float ExposedSummonInterval = 8f;
        public const int MaxMinions = 5;

        float summonTimer;

        public Artefact(int wards)
        {
            if (wards < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(wards), "The artefact needs at least one ward.");
            }
            Wards = wards;
            WardsRemaining = wards;
            Health = MaxHealth;
            summonTimer = ShieldedSummonInterval;
        }

        public int Wards { get; }
        public int WardsRemaining { get; private set; }
        public float Health { get; private set; }
        public bool Shielded => WardsRemaining > 0;
        public bool Destroyed => Health <= 0f;

        /// <summary>Purifies one ward. Returns true when that was the last one and the shield fell.</summary>
        public bool PurifyWard()
        {
            if (WardsRemaining == 0)
            {
                return false;
            }
            WardsRemaining--;
            if (WardsRemaining == 0)
            {
                summonTimer = Math.Min(summonTimer, ExposedSummonInterval);
                return true;
            }
            return false;
        }

        /// <summary>Damages the artefact; returns the damage dealt (0 while shielded).</summary>
        public float TakeDamage(float amount)
        {
            if (Shielded || Destroyed || amount <= 0f)
            {
                return 0f;
            }
            float dealt = Math.Min(amount, Health);
            Health -= dealt;
            return dealt;
        }

        /// <summary>
        /// Advances the summoning clock. Returns how many skeletons to raise now (0 or 1), never
        /// letting more than <see cref="MaxMinions"/> stand at once.
        /// </summary>
        public int Tick(float dt, int livingMinions)
        {
            if (Destroyed)
            {
                return 0;
            }
            summonTimer -= dt;
            if (summonTimer > 0f)
            {
                return 0;
            }
            summonTimer += Shielded ? ShieldedSummonInterval : ExposedSummonInterval;
            return livingMinions < MaxMinions ? 1 : 0;
        }
    }
}
