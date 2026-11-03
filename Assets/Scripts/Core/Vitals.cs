using System;

namespace SpookyPath.Core
{
    public enum PotionKind
    {
        Health,
        Mana,
        Stamina,
        /// <summary>Raises every maximum and refills all three bars.</summary>
        Elixir,
    }

    /// <summary>
    /// The explorer's health, mana and stamina. Mana regenerates slowly; stamina drains while
    /// sprinting and, once empty, stays exhausted until it refills to <see cref="Tuning.StaminaRecoverThreshold"/>.
    /// A short grace period after each hit stops one enemy from landing several blows at once.
    /// </summary>
    public sealed class Vitals
    {
        float shieldTime;
        float graceTime;
        float staminaDelay;

        public Vitals(float maxHealth = Tuning.MaxHealth, float maxMana = Tuning.MaxMana, float maxStamina = Tuning.MaxStamina)
        {
            MaxHealth = maxHealth;
            MaxMana = maxMana;
            MaxStamina = maxStamina;
            health = maxHealth;
            mana = maxMana;
            stamina = maxStamina;
        }

        float health;
        float mana;
        float stamina;

        public float Health => health;
        public float MaxHealth { get; private set; }
        public float Mana => mana;
        public float MaxMana { get; private set; }
        public float Stamina => stamina;
        public float MaxStamina { get; private set; }

        public bool IsDead => Health <= 0f;
        public bool Shielded => shieldTime > 0f;
        public float ShieldTimeLeft => shieldTime;
        public bool Exhausted { get; private set; }

        /// <summary>True while the explorer has the stamina to sprint this frame.</summary>
        public bool CanSprint => !IsDead && !Exhausted && Stamina > 0f;

        /// <summary>Applies a hit and returns the damage actually taken (0 when shielded, in grace time or dead).</summary>
        public float TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead || Shielded || graceTime > 0f)
            {
                return 0f;
            }
            float taken = Math.Min(amount, health);
            health -= taken;
            graceTime = Tuning.HurtGrace;
            return taken;
        }

        /// <summary>Instant death (lava, falling into the abyss), ignoring the shield.</summary>
        public void Kill()
        {
            health = 0f;
            shieldTime = 0f;
        }

        public float Heal(float amount) => IsDead ? 0f : Raise(ref health, amount, MaxHealth);

        public bool TrySpendMana(float cost)
        {
            if (IsDead || cost > Mana)
            {
                return false;
            }
            mana -= cost;
            return true;
        }

        public float RestoreMana(float amount) => Raise(ref mana, amount, MaxMana);

        public float RestoreStamina(float amount)
        {
            float restored = Raise(ref stamina, amount, MaxStamina);
            if (Stamina >= MaxStamina * Tuning.StaminaRecoverThreshold)
            {
                Exhausted = false;
            }
            return restored;
        }

        public void RaiseShield(float seconds) => shieldTime = Math.Max(shieldTime, seconds);

        /// <summary>Applies a potion. Returns false (and the potion stays) when it would do nothing.</summary>
        public bool Drink(PotionKind kind)
        {
            if (IsDead)
            {
                return false;
            }
            switch (kind)
            {
                case PotionKind.Health:
                    return Heal(Tuning.PotionAmount) > 0f;
                case PotionKind.Mana:
                    return RestoreMana(Tuning.PotionAmount) > 0f;
                case PotionKind.Stamina:
                    return RestoreStamina(Tuning.PotionAmount) > 0f;
                case PotionKind.Elixir:
                    MaxHealth += Tuning.ElixirBonus;
                    MaxMana += Tuning.ElixirBonus;
                    MaxStamina += Tuning.ElixirBonus;
                    health = MaxHealth;
                    mana = MaxMana;
                    stamina = MaxStamina;
                    Exhausted = false;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Advances timers and regeneration. <paramref name="sprinting"/> drains stamina when the
        /// explorer is actually sprinting; otherwise stamina recovers after a short delay.
        /// </summary>
        public void Tick(float dt, bool sprinting)
        {
            if (dt <= 0f || IsDead)
            {
                return;
            }
            shieldTime = Math.Max(0f, shieldTime - dt);
            graceTime = Math.Max(0f, graceTime - dt);
            Raise(ref mana, Tuning.ManaRegen * dt, MaxMana);

            if (sprinting && CanSprint)
            {
                stamina = Math.Max(0f, stamina - Tuning.StaminaDrain * dt);
                staminaDelay = Tuning.StaminaRegenDelay;
                if (stamina <= 0f)
                {
                    Exhausted = true;
                }
                return;
            }
            if (staminaDelay > 0f)
            {
                staminaDelay = Math.Max(0f, staminaDelay - dt);
                return;
            }
            RestoreStamina(Tuning.StaminaRegen * dt);
        }

        static float Raise(ref float value, float amount, float max)
        {
            if (amount <= 0f || value >= max)
            {
                return 0f;
            }
            float before = value;
            value = Math.Min(max, value + amount);
            return value - before;
        }
    }
}
