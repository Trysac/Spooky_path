using NUnit.Framework;

namespace SpookyPath.Core.Tests
{
    public class SpellTests
    {
        [Test]
        public void Casting_spends_mana_and_starts_the_cooldown()
        {
            var vitals = new Vitals();
            var book = new SpellBook();
            SpellInfo firebolt = SpellCatalog.Get(SpellKind.Firebolt);

            Assert.AreEqual(CastResult.Cast, book.TryCast(SpellKind.Firebolt, vitals));
            Assert.AreEqual(100f - firebolt.ManaCost, vitals.Mana);
            Assert.AreEqual(1f, book.CooldownFraction(SpellKind.Firebolt));
            Assert.AreEqual(CastResult.Cooldown, book.TryCast(SpellKind.Firebolt, vitals));
            Assert.AreEqual(100f - firebolt.ManaCost, vitals.Mana);
        }

        [Test]
        public void The_cooldown_ends_after_its_duration()
        {
            var vitals = new Vitals();
            var book = new SpellBook();
            book.TryCast(SpellKind.FrostNova, vitals);

            book.Tick(SpellCatalog.Get(SpellKind.FrostNova).Cooldown * 0.5f);
            Assert.AreEqual(0.5f, book.CooldownFraction(SpellKind.FrostNova), 0.001f);

            book.Tick(SpellCatalog.Get(SpellKind.FrostNova).Cooldown);
            Assert.AreEqual(0f, book.CooldownFraction(SpellKind.FrostNova));
            Assert.AreEqual(CastResult.Cast, book.TryCast(SpellKind.FrostNova, vitals));
        }

        [Test]
        public void Without_enough_mana_nothing_is_spent_and_no_cooldown_starts()
        {
            var vitals = new Vitals();
            vitals.TrySpendMana(90f);
            var book = new SpellBook();

            Assert.AreEqual(CastResult.NotEnoughMana, book.TryCast(SpellKind.FrostNova, vitals));
            Assert.AreEqual(10f, vitals.Mana);
            Assert.AreEqual(0f, book.CooldownFraction(SpellKind.FrostNova));
        }

        [Test]
        public void Mend_restores_health()
        {
            var vitals = new Vitals();
            vitals.TakeDamage(60f);

            new SpellBook().TryCast(SpellKind.Heal, vitals);

            Assert.AreEqual(40f + SpellCatalog.Get(SpellKind.Heal).Power, vitals.Health);
        }

        [Test]
        public void Bone_ward_raises_a_shield_for_its_duration()
        {
            var vitals = new Vitals();

            new SpellBook().TryCast(SpellKind.Shield, vitals);

            Assert.IsTrue(vitals.Shielded);
            Assert.AreEqual(SpellCatalog.Get(SpellKind.Shield).Power, vitals.ShieldTimeLeft);
        }

        [Test]
        public void The_dead_cannot_cast()
        {
            var vitals = new Vitals();
            vitals.Kill();

            Assert.AreEqual(CastResult.Dead, new SpellBook().TryCast(SpellKind.Firebolt, vitals));
        }

        [Test]
        public void A_strike_hits_targets_in_front_within_range_only()
        {
            var origin = new Vec2(0f, 0f);
            var north = new Vec2(0f, 1f);

            Assert.IsTrue(Combat.InArc(origin, north, new Vec2(0f, 2f), 2.4f, 110f));
            Assert.IsTrue(Combat.InArc(origin, north, new Vec2(1.5f, 1.2f), 2.4f, 110f));
            Assert.IsFalse(Combat.InArc(origin, north, new Vec2(0f, 3f), 2.4f, 110f), "out of range");
            Assert.IsFalse(Combat.InArc(origin, north, new Vec2(2f, 0f), 2.4f, 110f), "beside, outside the arc");
            Assert.IsFalse(Combat.InArc(origin, north, new Vec2(0f, -1f), 2.4f, 110f), "behind");
        }
    }
}
