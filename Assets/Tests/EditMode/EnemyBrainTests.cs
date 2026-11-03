using NUnit.Framework;

namespace SpookyPath.Core.Tests
{
    public class EnemyBrainTests
    {
        static readonly Vec2 Post = new Vec2(0f, 0f);

        [Test]
        public void A_guard_without_a_waypoint_stands_still()
        {
            var brain = new EnemyBrain(EnemyKind.Minion, Post);

            EnemyIntent intent = brain.Tick(0.1f, Post, new Vec2(30f, 0f), true, true);

            Assert.AreEqual(EnemyState.Idle, brain.State);
            Assert.AreEqual(0f, intent.Speed);
        }

        [Test]
        public void It_chases_the_explorer_seen_within_sight_range()
        {
            var brain = new EnemyBrain(EnemyKind.Minion, Post);
            var player = new Vec2(6f, 0f);

            EnemyIntent intent = brain.Tick(0.1f, Post, player, true, true);

            Assert.AreEqual(EnemyState.Chase, brain.State);
            Assert.AreEqual(player, intent.MoveTarget);
            Assert.AreEqual(brain.Stats.ChaseSpeed, intent.Speed);
        }

        [Test]
        public void It_does_not_notice_the_explorer_behind_a_wall_or_out_of_range()
        {
            var brain = new EnemyBrain(EnemyKind.Minion, Post);

            brain.Tick(0.1f, Post, new Vec2(5f, 0f), false, true);
            Assert.AreEqual(EnemyState.Idle, brain.State, "no line of sight");

            brain.Tick(0.1f, Post, new Vec2(brain.Stats.SightRange + 1f, 0f), true, true);
            Assert.AreEqual(EnemyState.Idle, brain.State, "too far");
        }

        [Test]
        public void In_range_it_winds_up_and_the_strike_lands_when_the_windup_ends()
        {
            var brain = new EnemyBrain(EnemyKind.Minion, Post);
            var player = new Vec2(1.5f, 0f);

            brain.Tick(0.1f, Post, player, true, true);
            Assert.AreEqual(EnemyState.Attack, brain.State);

            EnemyIntent early = brain.Tick(brain.Stats.Windup * 0.5f, Post, player, true, true);
            Assert.IsFalse(early.Strike);
            Assert.AreEqual(0f, early.Speed);

            EnemyIntent strike = brain.Tick(brain.Stats.Windup, Post, player, true, true);
            Assert.IsTrue(strike.Strike);
            Assert.AreEqual(EnemyState.Chase, brain.State);
        }

        [Test]
        public void After_a_strike_it_waits_for_the_cooldown_before_winding_up_again()
        {
            var brain = new EnemyBrain(EnemyKind.Minion, Post);
            var player = new Vec2(1.5f, 0f);
            brain.Tick(0.1f, Post, player, true, true);
            brain.Tick(brain.Stats.Windup + 0.01f, Post, player, true, true);

            brain.Tick(0.1f, Post, player, true, true);
            Assert.AreEqual(EnemyState.Chase, brain.State);

            brain.Tick(brain.Stats.Cooldown, Post, player, true, true);
            Assert.AreEqual(EnemyState.Attack, brain.State);
        }

        [Test]
        public void A_melee_strike_misses_when_the_explorer_stepped_away_during_the_windup()
        {
            var brain = new EnemyBrain(EnemyKind.Warrior, Post);
            brain.Tick(0.1f, Post, new Vec2(2f, 0f), true, true);

            EnemyIntent strike = brain.Tick(brain.Stats.Windup + 0.01f, Post, new Vec2(6f, 0f), true, true);

            Assert.IsFalse(strike.Strike);
        }

        [Test]
        public void A_mage_attacks_from_a_distance()
        {
            var brain = new EnemyBrain(EnemyKind.Mage, Post);
            var player = new Vec2(8f, 0f);

            brain.Tick(0.1f, Post, player, true, true);

            Assert.IsTrue(brain.Stats.Ranged);
            Assert.AreEqual(EnemyState.Attack, brain.State);
            Assert.IsTrue(brain.Tick(brain.Stats.Windup + 0.01f, Post, new Vec2(9f, 0f), true, true).Strike);
        }

        [Test]
        public void It_gives_up_after_losing_sight_for_a_while_and_walks_back_to_its_post()
        {
            var brain = new EnemyBrain(EnemyKind.Minion, Post);
            var away = new Vec2(5f, 0f);
            brain.Tick(0.1f, Post, new Vec2(8f, 0f), true, true);

            brain.Tick(EnemyBrain.MemorySeconds * 0.5f, away, new Vec2(12f, 0f), false, true);
            Assert.AreEqual(EnemyState.Chase, brain.State);

            EnemyIntent intent = brain.Tick(EnemyBrain.MemorySeconds, away, new Vec2(12f, 0f), false, true);
            Assert.AreEqual(EnemyState.Return, brain.State);
            Assert.AreEqual(Post, intent.MoveTarget);

            brain.Tick(0.1f, Post, new Vec2(30f, 0f), false, true);
            Assert.AreEqual(EnemyState.Idle, brain.State);
        }

        [Test]
        public void It_will_not_be_dragged_further_than_its_leash()
        {
            var brain = new EnemyBrain(EnemyKind.Minion, Post);
            var far = new Vec2(EnemyBrain.LeashDistance + 1f, 0f);
            brain.Tick(0.1f, Post, new Vec2(5f, 0f), true, true);

            brain.Tick(0.1f, far, far + new Vec2(4f, 0f), true, true);

            Assert.AreEqual(EnemyState.Return, brain.State);
        }

        [Test]
        public void It_stops_chasing_a_dead_explorer()
        {
            var brain = new EnemyBrain(EnemyKind.Minion, Post);
            brain.Tick(0.1f, Post, new Vec2(5f, 0f), true, true);

            brain.Tick(0.1f, Post, new Vec2(5f, 0f), true, false);

            Assert.AreEqual(EnemyState.Return, brain.State);
        }

        [Test]
        public void A_patrol_walks_to_its_waypoint_pauses_and_walks_back()
        {
            var waypoint = new Vec2(8f, 0f);
            var brain = new EnemyBrain(EnemyKind.Minion, Post, waypoint);

            EnemyIntent outward = brain.Tick(0.1f, Post, new Vec2(0f, 50f), false, true);
            Assert.AreEqual(EnemyState.Patrol, brain.State);
            Assert.AreEqual(waypoint, outward.MoveTarget);
            Assert.AreEqual(brain.Stats.WalkSpeed, outward.Speed);

            EnemyIntent arrived = brain.Tick(0.1f, waypoint, new Vec2(0f, 50f), false, true);
            Assert.AreEqual(0f, arrived.Speed);

            brain.Tick(EnemyBrain.PatrolPause + 0.01f, waypoint, new Vec2(0f, 50f), false, true);
            EnemyIntent back = brain.Tick(0.1f, waypoint, new Vec2(0f, 50f), false, true);
            Assert.AreEqual(Post, back.MoveTarget);
        }

        [Test]
        public void Being_hurt_alerts_it_and_staggers_it_briefly()
        {
            var brain = new EnemyBrain(EnemyKind.Warrior, Post);

            bool killed = brain.TakeDamage(20f);

            Assert.IsFalse(killed);
            Assert.AreEqual(brain.Stats.Health - 20f, brain.Health);
            Assert.AreEqual(EnemyState.Chase, brain.State);
            Assert.AreEqual(0f, brain.Tick(0.1f, Post, new Vec2(3f, 0f), true, true).Speed, "staggered");
            brain.Tick(EnemyBrain.StaggerSeconds, Post, new Vec2(5f, 0f), true, true);
            Assert.AreEqual(brain.Stats.ChaseSpeed, brain.Tick(0.1f, Post, new Vec2(5f, 0f), true, true).Speed);
        }

        [Test]
        public void The_killing_blow_reports_the_kill_once_and_the_dead_do_nothing()
        {
            var brain = new EnemyBrain(EnemyKind.Minion, Post);

            Assert.IsTrue(brain.TakeDamage(brain.Stats.Health));
            Assert.IsTrue(brain.IsDead);
            Assert.IsFalse(brain.TakeDamage(10f));
            EnemyIntent intent = brain.Tick(0.1f, Post, new Vec2(1f, 0f), true, true);
            Assert.IsFalse(intent.Strike);
            Assert.AreEqual(0f, intent.Speed);
        }

        [Test]
        public void Frost_halves_its_speed_until_it_thaws()
        {
            var brain = new EnemyBrain(EnemyKind.Minion, Post);
            brain.Freeze(2f);

            float chilled = brain.Tick(0.1f, Post, new Vec2(6f, 0f), true, true).Speed;
            Assert.AreEqual(brain.Stats.ChaseSpeed * 0.5f, chilled, 0.001f);

            brain.Tick(2f, Post, new Vec2(6f, 0f), true, true);
            Assert.AreEqual(brain.Stats.ChaseSpeed, brain.Tick(0.1f, Post, new Vec2(6f, 0f), true, true).Speed, 0.001f);
        }
    }
}
