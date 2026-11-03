using System;

namespace SpookyPath.Core
{
    public enum EnemyKind
    {
        /// <summary>A quick skeleton with a rusty blade.</summary>
        Minion,
        /// <summary>A slow, armoured skeleton that hits hard.</summary>
        Warrior,
        /// <summary>A skeleton mage that keeps its distance and throws cursed bolts.</summary>
        Mage,
    }

    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        /// <summary>Winding up a blow or a bolt; the strike lands when the wind-up ends.</summary>
        Attack,
        /// <summary>Lost the explorer and walks back to its post.</summary>
        Return,
        Dead,
    }

    public readonly struct EnemyStats
    {
        public readonly float Health;
        public readonly float WalkSpeed;
        public readonly float ChaseSpeed;
        public readonly float SightRange;
        public readonly float AttackRange;
        public readonly float Damage;
        public readonly float Windup;
        public readonly float Cooldown;
        public readonly bool Ranged;
        public readonly int Score;

        public EnemyStats(float health, float walkSpeed, float chaseSpeed, float sightRange, float attackRange,
            float damage, float windup, float cooldown, bool ranged, int score)
        {
            Health = health;
            WalkSpeed = walkSpeed;
            ChaseSpeed = chaseSpeed;
            SightRange = sightRange;
            AttackRange = attackRange;
            Damage = damage;
            Windup = windup;
            Cooldown = cooldown;
            Ranged = ranged;
            Score = score;
        }

        public static EnemyStats For(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Warrior:
                    return new EnemyStats(150f, 1.6f, 3.4f, 11f, 2.3f, 22f, 0.6f, 1.6f, false, 150);
                case EnemyKind.Mage:
                    return new EnemyStats(70f, 1.8f, 3.2f, 14f, 9f, 14f, 0.7f, 2.2f, true, 120);
                default:
                    return new EnemyStats(60f, 2f, 4.6f, 10f, 2f, 10f, 0.4f, 1.1f, false, 80);
            }
        }
    }

    /// <summary>What the brain wants the body to do this frame.</summary>
    public readonly struct EnemyIntent
    {
        public readonly Vec2 MoveTarget;
        public readonly float Speed;
        public readonly Vec2 LookAt;
        /// <summary>True on the frame a wind-up completes: apply the melee hit or launch the bolt.</summary>
        public readonly bool Strike;

        public EnemyIntent(Vec2 moveTarget, float speed, Vec2 lookAt, bool strike)
        {
            MoveTarget = moveTarget;
            Speed = speed;
            LookAt = lookAt;
            Strike = strike;
        }
    }

    /// <summary>
    /// A skeleton's perception and state machine. It patrols between its post and an optional
    /// waypoint, notices the explorer within sight range when there is a clear line of sight (or
    /// when hurt), chases, winds up and strikes in range, gives up after losing sight for
    /// <see cref="MemorySeconds"/> or when dragged too far from its post, and walks back.
    /// Mages stop at a distance and throw bolts instead of closing in.
    /// </summary>
    public sealed class EnemyBrain
    {
        public const float MemorySeconds = 3f;
        public const float LeashDistance = 26f;
        public const float PatrolPause = 1.2f;
        public const float StaggerSeconds = 0.35f;
        const float ArriveDistance = 0.3f;
        /// <summary>A strike still lands if the explorer stepped only this much past the attack range.</summary>
        public const float StrikeTolerance = 1.25f;

        readonly Vec2 post;
        readonly Vec2? waypoint;
        bool headingToWaypoint;
        float pause;
        float sinceSeen;
        float windup;
        float cooldown;
        float stagger;

        public EnemyBrain(EnemyKind kind, Vec2 post, Vec2? waypoint = null)
        {
            Kind = kind;
            Stats = EnemyStats.For(kind);
            Health = Stats.Health;
            this.post = post;
            this.waypoint = waypoint;
            Position = post;
            State = waypoint.HasValue ? EnemyState.Patrol : EnemyState.Idle;
            headingToWaypoint = waypoint.HasValue;
        }

        public EnemyKind Kind { get; }
        public EnemyStats Stats { get; }
        public EnemyState State { get; private set; }
        public float Health { get; private set; }
        public Vec2 Position { get; private set; }
        public bool IsDead => State == EnemyState.Dead;
        /// <summary>Seconds of slow left from Frost Nova; the body moves at half speed meanwhile.</summary>
        public float Chill { get; private set; }

        public Vec2 Post => post;

        public EnemyIntent Tick(float dt, Vec2 position, Vec2 player, bool playerVisible, bool playerAlive)
        {
            Position = position;
            if (IsDead)
            {
                return new EnemyIntent(position, 0f, position, false);
            }
            cooldown = Math.Max(0f, cooldown - dt);
            Chill = Math.Max(0f, Chill - dt);
            float slow = Chill > 0f ? 0.5f : 1f;
            if (stagger > 0f)
            {
                stagger = Math.Max(0f, stagger - dt);
                return new EnemyIntent(position, 0f, player, false);
            }

            float distance = Vec2.Distance(position, player);
            bool sees = playerAlive && playerVisible && distance <= Stats.SightRange;
            sinceSeen = sees ? 0f : sinceSeen + dt;

            switch (State)
            {
                case EnemyState.Idle:
                case EnemyState.Patrol:
                case EnemyState.Return:
                    if (sees)
                    {
                        State = EnemyState.Chase;
                        goto case EnemyState.Chase;
                    }
                    return Wander(dt, position, slow);

                case EnemyState.Chase:
                    if (!playerAlive || sinceSeen > MemorySeconds || Vec2.Distance(position, post) > LeashDistance)
                    {
                        State = EnemyState.Return;
                        return new EnemyIntent(post, Stats.WalkSpeed * slow, post, false);
                    }
                    if (distance <= Stats.AttackRange && sees)
                    {
                        if (cooldown <= 0f)
                        {
                            State = EnemyState.Attack;
                            windup = Stats.Windup;
                        }
                        return new EnemyIntent(position, 0f, player, false);
                    }
                    return new EnemyIntent(player, Stats.ChaseSpeed * slow, player, false);

                case EnemyState.Attack:
                    windup -= dt;
                    if (windup > 0f)
                    {
                        return new EnemyIntent(position, 0f, player, false);
                    }
                    cooldown = Stats.Cooldown;
                    State = EnemyState.Chase;
                    bool lands = playerAlive && (Stats.Ranged ? sees : distance <= Stats.AttackRange * StrikeTolerance);
                    return new EnemyIntent(position, 0f, player, lands);
            }
            return new EnemyIntent(position, 0f, player, false);
        }

        EnemyIntent Wander(float dt, Vec2 position, float slow)
        {
            if (State == EnemyState.Return)
            {
                if (Vec2.Distance(position, post) <= ArriveDistance)
                {
                    State = waypoint.HasValue ? EnemyState.Patrol : EnemyState.Idle;
                    headingToWaypoint = waypoint.HasValue;
                }
                return new EnemyIntent(post, Stats.WalkSpeed * slow, post, false);
            }
            if (!waypoint.HasValue)
            {
                return new EnemyIntent(position, 0f, position, false);
            }
            if (pause > 0f)
            {
                pause = Math.Max(0f, pause - dt);
                return new EnemyIntent(position, 0f, position, false);
            }
            Vec2 target = headingToWaypoint ? waypoint.Value : post;
            if (Vec2.Distance(position, target) <= ArriveDistance)
            {
                headingToWaypoint = !headingToWaypoint;
                pause = PatrolPause;
                return new EnemyIntent(position, 0f, position, false);
            }
            return new EnemyIntent(target, Stats.WalkSpeed * slow, target, false);
        }

        /// <summary>Applies damage. Returns true when this blow destroyed the skeleton.</summary>
        public bool TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return false;
            }
            Health = Math.Max(0f, Health - amount);
            if (Health <= 0f)
            {
                State = EnemyState.Dead;
                return true;
            }
            // Being hurt always alerts it, even from behind or out of sight.
            State = EnemyState.Chase;
            sinceSeen = 0f;
            windup = 0f;
            stagger = StaggerSeconds;
            return false;
        }

        public void Freeze(float seconds) => Chill = Math.Max(Chill, seconds);
    }
}
