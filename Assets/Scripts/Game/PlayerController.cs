using System;
using SpookyPath.Core;
using UnityEngine;

namespace SpookyPath.Game
{
    /// <summary>
    /// The explorer's body: a CharacterController moved by the commands, the KayKit mage model
    /// and its animator. Rules (health, mana, stamina, spells) live in <see cref="Vitals"/> and
    /// <see cref="SpellBook"/>; the director resolves what strikes and spells hit.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        const float ActionLock = 0.35f;
        const float TurnSpeed = 14f;

        CharacterController body;
        Animator animator;
        Transform model;
        float verticalSpeed;
        int jumpsUsed;
        float actionTimer;
        float strikeCooldown;
        string currentState;
        Vector3 facing = Vector3.forward;
        GameObject shieldBubble;
        bool wasGrounded = true;

        public Vitals Vitals { get; private set; }
        public SpellBook Spells { get; } = new SpellBook();
        public Vector3 Facing => facing;
        public Vector3 Velocity { get; private set; }
        public bool Grounded => body != null && body.isGrounded;
        public bool IsDead => Vitals == null || Vitals.IsDead;
        public bool Sprinting { get; private set; }

        /// <summary>Raised with the strike's origin and facing; the director applies the damage.</summary>
        public event Action<Vector3, Vector3> Struck;
        /// <summary>Raised when a spell was cast (mana and cooldown already paid).</summary>
        public event Action<SpellKind> Cast;
        public event Action<CastResult> CastFailed;
        public event Action Jumped;
        public event Action Landed;

        public void Init(GameArt art)
        {
            body = GetComponent<CharacterController>();
            body.height = 1.9f;
            body.radius = 0.45f;
            body.center = new Vector3(0f, 0.95f, 0f);
            body.slopeLimit = 50f;
            body.stepOffset = 0.4f;
            body.skinWidth = 0.05f;
            gameObject.layer = 2; // Ignore Raycast: enemy sight lines only test walls.

            GameObject instance = Instantiate(art.Explorer, transform);
            model = instance.transform;
            model.localScale = Vector3.one * 0.82f;
            foreach (Transform part in model.GetComponentsInChildren<Transform>(true))
            {
                // Keep the staff and the closed spellbook; hide the duplicate props of the model.
                if (part.name == "Spellbook_open" || part.name == "1H_Wand")
                {
                    part.gameObject.SetActive(false);
                }
            }
            animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = art.ExplorerAnimator;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Fx.BlobShadow(transform, 0.8f);
            shieldBubble = Fx.Shape("Bone Ward", art.Sphere, art.ShieldBubble, transform, new Vector3(0f, 1.1f, 0f), Vector3.one * 2.6f);
            shieldBubble.SetActive(false);
            ResetState();
        }

        public void ResetState()
        {
            Vitals = new Vitals();
            Spells.Reset();
            verticalSpeed = 0f;
            jumpsUsed = 0;
            actionTimer = 0f;
            strikeCooldown = 0f;
            Velocity = Vector3.zero;
            currentState = null;
            Play("Idle", 0f);
        }

        public void Teleport(Vector3 position, Vector3 newFacing)
        {
            body.enabled = false;
            transform.position = position;
            body.enabled = true;
            verticalSpeed = 0f;
            if (newFacing.sqrMagnitude > 0.01f)
            {
                facing = newFacing.normalized;
                model.rotation = Quaternion.LookRotation(facing);
            }
        }

        public void Tick(float dt, Commands commands, Vector3 carry)
        {
            Vitals.Tick(dt, Sprinting);
            Spells.Tick(dt);
            shieldBubble.SetActive(Vitals.Shielded);
            if (Vitals.Shielded)
            {
                shieldBubble.transform.localScale = Vector3.one * (2.6f + Mathf.Sin(Time.time * 6f) * 0.06f);
            }
            if (IsDead)
            {
                Fall(dt, Vector3.zero, carry);
                return;
            }
            actionTimer = Mathf.Max(0f, actionTimer - dt);
            strikeCooldown = Mathf.Max(0f, strikeCooldown - dt);

            Vector3 wish = new Vector3(commands.Move.x, 0f, commands.Move.y);
            bool moving = wish.sqrMagnitude > 0.01f;
            Sprinting = moving && commands.Sprint && Vitals.CanSprint;
            float speed = Sprinting ? Tuning.SprintSpeed : Tuning.WalkSpeed;
            if (actionTimer > 0f)
            {
                speed *= 0.35f;
            }
            if (moving)
            {
                facing = Vector3.Slerp(facing, wish.normalized, 1f - Mathf.Exp(-TurnSpeed * dt));
            }

            if (body.isGrounded)
            {
                jumpsUsed = 0;
            }
            if (commands.Jump && jumpsUsed < Tuning.MaxJumps)
            {
                jumpsUsed++;
                verticalSpeed = Tuning.JumpSpeed * (jumpsUsed == 1 ? 1f : 0.85f);
                Jumped?.Invoke();
            }

            if (commands.Strike && strikeCooldown <= 0f)
            {
                strikeCooldown = Tuning.StrikeCooldown;
                actionTimer = ActionLock;
                Play("Strike", 0.05f, true);
                Struck?.Invoke(transform.position, facing);
            }
            else if (commands.Spell >= 0 && commands.Spell < SpellCatalog.Count)
            {
                var kind = (SpellKind)commands.Spell;
                CastResult result = Spells.TryCast(kind, Vitals);
                if (result == CastResult.Cast)
                {
                    actionTimer = ActionLock;
                    Play("Cast", 0.05f, true);
                    Cast?.Invoke(kind);
                }
                else
                {
                    CastFailed?.Invoke(result);
                }
            }

            Fall(dt, wish * speed, carry);
            model.rotation = Quaternion.LookRotation(facing);

            if (actionTimer <= 0f)
            {
                string state = !body.isGrounded && jumpsUsed > 0 ? "Jump" : !moving ? "Idle" : Sprinting ? "Run" : "Walk";
                Play(state, 0.12f);
            }
        }

        void Fall(float dt, Vector3 horizontal, Vector3 carry)
        {
            verticalSpeed -= Tuning.Gravity * dt;
            if (body.isGrounded && verticalSpeed < -2f)
            {
                verticalSpeed = -2f;
            }
            Vector3 motion = (horizontal + Vector3.up * verticalSpeed) * dt + carry;
            body.Move(motion);
            Velocity = horizontal;
            if (body.isGrounded && !wasGrounded)
            {
                Landed?.Invoke();
            }
            wasGrounded = body.isGrounded;
        }

        /// <summary>Applies an enemy hit. Returns the damage taken.</summary>
        public float Hurt(float damage)
        {
            float taken = Vitals.TakeDamage(damage);
            if (taken > 0f)
            {
                if (Vitals.IsDead)
                {
                    Die();
                }
                else if (actionTimer <= 0f)
                {
                    Play("Hit", 0.05f, true);
                    actionTimer = 0.25f;
                }
            }
            return taken;
        }

        /// <summary>Lava, pits and finishing blows: plays the death.</summary>
        public void Die()
        {
            if (!Vitals.IsDead)
            {
                Vitals.Kill();
            }
            Play("Die", 0.08f, true);
        }

        /// <summary>Turns to a target at once (aim assist for strikes and bolts).</summary>
        public void FaceTowards(Vector3 point)
        {
            Vector3 to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
            {
                facing = to.normalized;
                model.rotation = Quaternion.LookRotation(facing);
            }
        }

        public void Celebrate() => Play("Cheer", 0.15f, true);

        public void Interact()
        {
            actionTimer = ActionLock;
            Play("Interact", 0.05f, true);
        }

        void Play(string state, float blend, bool restart = false)
        {
            if (animator == null || animator.runtimeAnimatorController == null || (!restart && state == currentState))
            {
                return;
            }
            currentState = state;
            animator.CrossFadeInFixedTime(state, blend, 0, 0f);
        }
    }
}
