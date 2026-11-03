using NUnit.Framework;

namespace SpookyPath.Core.Tests
{
    public class VitalsTests
    {
        [Test]
        public void A_hit_lowers_health_and_reports_the_damage_taken()
        {
            var vitals = new Vitals();

            float taken = vitals.TakeDamage(30f);

            Assert.AreEqual(30f, taken);
            Assert.AreEqual(70f, vitals.Health);
            Assert.IsFalse(vitals.IsDead);
        }

        [Test]
        public void A_second_hit_inside_the_grace_time_is_ignored_but_lands_after_it()
        {
            var vitals = new Vitals();
            vitals.TakeDamage(10f);

            Assert.AreEqual(0f, vitals.TakeDamage(10f));
            Assert.AreEqual(90f, vitals.Health);

            vitals.Tick(Tuning.HurtGrace + 0.01f, false);
            Assert.AreEqual(10f, vitals.TakeDamage(10f));
            Assert.AreEqual(80f, vitals.Health);
        }

        [Test]
        public void Damage_never_takes_health_below_zero_and_kills()
        {
            var vitals = new Vitals();

            float taken = vitals.TakeDamage(250f);

            Assert.AreEqual(100f, taken);
            Assert.AreEqual(0f, vitals.Health);
            Assert.IsTrue(vitals.IsDead);
            Assert.AreEqual(0f, vitals.TakeDamage(5f));
        }

        [Test]
        public void The_shield_blocks_damage_until_it_expires()
        {
            var vitals = new Vitals();
            vitals.RaiseShield(2f);

            Assert.IsTrue(vitals.Shielded);
            Assert.AreEqual(0f, vitals.TakeDamage(50f));
            Assert.AreEqual(100f, vitals.Health);

            vitals.Tick(2.1f, false);
            Assert.IsFalse(vitals.Shielded);
            Assert.AreEqual(50f, vitals.TakeDamage(50f));
        }

        [Test]
        public void Lava_and_pits_kill_even_through_the_shield()
        {
            var vitals = new Vitals();
            vitals.RaiseShield(5f);

            vitals.Kill();

            Assert.IsTrue(vitals.IsDead);
            Assert.IsFalse(vitals.Shielded);
        }

        [Test]
        public void Sprinting_drains_stamina_at_a_steady_frame_rate()
        {
            // The 2023 build rounded the per-frame drain to an integer, so at 60 fps stamina never moved.
            var vitals = new Vitals();

            for (int frame = 0; frame < 60; frame++)
            {
                vitals.Tick(1f / 60f, true);
            }

            Assert.AreEqual(100f - Tuning.StaminaDrain, vitals.Stamina, 0.01f);
        }

        [Test]
        public void Stamina_recovers_after_a_short_delay_when_not_sprinting()
        {
            var vitals = new Vitals();
            vitals.Tick(1f, true);
            float drained = vitals.Stamina;

            vitals.Tick(Tuning.StaminaRegenDelay * 0.5f, false);
            Assert.AreEqual(drained, vitals.Stamina, 0.001f);

            vitals.Tick(Tuning.StaminaRegenDelay, false);
            vitals.Tick(1f, false);
            Assert.AreEqual(drained + Tuning.StaminaRegen, vitals.Stamina, 0.01f);
        }

        [Test]
        public void An_exhausted_explorer_cannot_sprint_until_a_quarter_of_the_stamina_is_back()
        {
            var vitals = new Vitals();
            for (int i = 0; i < 100 && vitals.Stamina > 0f; i++)
            {
                vitals.Tick(0.1f, true);
            }
            Assert.IsTrue(vitals.Exhausted);
            Assert.IsFalse(vitals.CanSprint);

            vitals.Tick(Tuning.StaminaRegenDelay, false);
            vitals.Tick(0.5f, false); // 11 stamina: still exhausted
            Assert.IsFalse(vitals.CanSprint);
            float before = vitals.Stamina;
            vitals.Tick(0.1f, true); // trying to sprint while exhausted does not drain it
            Assert.GreaterOrEqual(vitals.Stamina, before);

            vitals.Tick(Tuning.StaminaRegenDelay + 1f, false);
            Assert.IsFalse(vitals.Exhausted);
            Assert.IsTrue(vitals.CanSprint);
        }

        [Test]
        public void Mana_regenerates_slowly_up_to_the_maximum()
        {
            var vitals = new Vitals();
            Assert.IsTrue(vitals.TrySpendMana(50f));

            vitals.Tick(2f, false);
            Assert.AreEqual(50f + 2f * Tuning.ManaRegen, vitals.Mana, 0.001f);

            vitals.Tick(100f, false);
            Assert.AreEqual(vitals.MaxMana, vitals.Mana);
        }

        [Test]
        public void Spending_more_mana_than_available_fails_and_keeps_it()
        {
            var vitals = new Vitals();
            vitals.TrySpendMana(90f);

            Assert.IsFalse(vitals.TrySpendMana(20f));
            Assert.AreEqual(10f, vitals.Mana);
        }

        [Test]
        public void A_health_potion_heals_up_to_the_maximum_and_is_refused_at_full_health()
        {
            var vitals = new Vitals();
            Assert.IsFalse(vitals.Drink(PotionKind.Health));

            vitals.TakeDamage(20f);
            Assert.IsTrue(vitals.Drink(PotionKind.Health));
            Assert.AreEqual(100f, vitals.Health);
        }

        [Test]
        public void Mana_and_stamina_potions_restore_their_bar()
        {
            var vitals = new Vitals();
            vitals.TrySpendMana(60f);
            vitals.Tick(1f, true);

            Assert.IsTrue(vitals.Drink(PotionKind.Mana));
            Assert.IsTrue(vitals.Drink(PotionKind.Stamina));
            Assert.AreEqual(40f + Tuning.PotionAmount + Tuning.ManaRegen, vitals.Mana, 0.01f);
            Assert.AreEqual(100f, vitals.Stamina, 0.01f);
        }

        [Test]
        public void An_elixir_raises_every_maximum_and_refills_all_bars()
        {
            var vitals = new Vitals();
            vitals.TakeDamage(50f);
            vitals.TrySpendMana(50f);

            Assert.IsTrue(vitals.Drink(PotionKind.Elixir));

            Assert.AreEqual(100f + Tuning.ElixirBonus, vitals.MaxHealth);
            Assert.AreEqual(100f + Tuning.ElixirBonus, vitals.MaxMana);
            Assert.AreEqual(100f + Tuning.ElixirBonus, vitals.MaxStamina);
            Assert.AreEqual(vitals.MaxHealth, vitals.Health);
            Assert.AreEqual(vitals.MaxMana, vitals.Mana);
            Assert.AreEqual(vitals.MaxStamina, vitals.Stamina);
        }

        [Test]
        public void The_dead_cannot_drink_or_heal()
        {
            var vitals = new Vitals();
            vitals.Kill();

            Assert.IsFalse(vitals.Drink(PotionKind.Health));
            Assert.AreEqual(0f, vitals.Heal(50f));
            Assert.IsTrue(vitals.IsDead);
        }
    }
}
