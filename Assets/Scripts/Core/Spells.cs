using System;

namespace SpookyPath.Core
{
    public enum SpellKind
    {
        /// <summary>A fast bolt of fire that hits the first enemy in its path.</summary>
        Firebolt,
        /// <summary>A ring of frost that damages and slows every enemy around the explorer.</summary>
        FrostNova,
        /// <summary>Restores health.</summary>
        Heal,
        /// <summary>A ward that blocks all damage for a few seconds.</summary>
        Shield,
    }

    public enum CastResult
    {
        Cast,
        NotEnoughMana,
        Cooldown,
        Dead,
    }

    public readonly struct SpellInfo
    {
        public readonly SpellKind Kind;
        public readonly string Name;
        public readonly float ManaCost;
        public readonly float Cooldown;
        /// <summary>Damage for attack spells, health restored for Heal, seconds for Shield.</summary>
        public readonly float Power;
        /// <summary>Area radius (Frost Nova) or projectile speed (Firebolt).</summary>
        public readonly float Reach;

        public SpellInfo(SpellKind kind, string name, float manaCost, float cooldown, float power, float reach)
        {
            Kind = kind;
            Name = name;
            ManaCost = manaCost;
            Cooldown = cooldown;
            Power = power;
            Reach = reach;
        }
    }

    /// <summary>The explorer's four spells (keys 1-4), replacing the seven hard-wired attacks of the 2023 build.</summary>
    public static class SpellCatalog
    {
        public const int Count = 4;

        static readonly SpellInfo[] Spells =
        {
            new SpellInfo(SpellKind.Firebolt, "Firebolt", 12f, 0.45f, 40f, 18f),
            new SpellInfo(SpellKind.FrostNova, "Frost Nova", 30f, 4f, 30f, 6f),
            new SpellInfo(SpellKind.Heal, "Mend", 35f, 6f, 45f, 0f),
            new SpellInfo(SpellKind.Shield, "Bone Ward", 25f, 10f, 4f, 0f),
        };

        public static SpellInfo Get(SpellKind kind) => Spells[(int)kind];
    }

    /// <summary>Cooldowns and mana checks for casting; the game spawns the effects when a cast succeeds.</summary>
    public sealed class SpellBook
    {
        readonly float[] cooldowns = new float[SpellCatalog.Count];

        public CastResult TryCast(SpellKind kind, Vitals vitals)
        {
            if (vitals == null)
            {
                throw new ArgumentNullException(nameof(vitals));
            }
            if (vitals.IsDead)
            {
                return CastResult.Dead;
            }
            SpellInfo spell = SpellCatalog.Get(kind);
            if (cooldowns[(int)kind] > 0f)
            {
                return CastResult.Cooldown;
            }
            if (!vitals.TrySpendMana(spell.ManaCost))
            {
                return CastResult.NotEnoughMana;
            }
            cooldowns[(int)kind] = spell.Cooldown;
            switch (kind)
            {
                case SpellKind.Heal:
                    vitals.Heal(spell.Power);
                    break;
                case SpellKind.Shield:
                    vitals.RaiseShield(spell.Power);
                    break;
            }
            return CastResult.Cast;
        }

        /// <summary>0 when the spell is ready, up to 1 right after casting.</summary>
        public float CooldownFraction(SpellKind kind)
        {
            float total = SpellCatalog.Get(kind).Cooldown;
            return total <= 0f ? 0f : Math.Min(1f, cooldowns[(int)kind] / total);
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i] = Math.Max(0f, cooldowns[i] - dt);
            }
        }

        public void Reset() => Array.Clear(cooldowns, 0, cooldowns.Length);
    }

    public static class Combat
    {
        /// <summary>
        /// True when <paramref name="target"/> is within <paramref name="range"/> of <paramref name="origin"/>
        /// and inside the arc of <paramref name="arcDegrees"/> centred on <paramref name="facing"/>.
        /// </summary>
        public static bool InArc(Vec2 origin, Vec2 facing, Vec2 target, float range, float arcDegrees)
        {
            Vec2 to = target - origin;
            float distance = to.Length;
            if (distance > range)
            {
                return false;
            }
            if (distance < 1e-4f)
            {
                return true;
            }
            Vec2 forward = facing.Normalized;
            if (forward.LengthSquared < 1e-6f)
            {
                return true;
            }
            double cos = Vec2.Dot(forward, to * (1f / distance));
            double halfArc = arcDegrees * 0.5 * Math.PI / 180.0;
            return cos >= Math.Cos(halfArc) - 1e-6;
        }
    }
}
