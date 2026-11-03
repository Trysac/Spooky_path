using SpookyPath.Core;
using UnityEngine;

namespace SpookyPath.Game
{
    /// <summary>
    /// A skeleton's body. The KayKit skeletons are unrigged (body, arms and head are separate
    /// meshes), so walking, winding up, striking and staggering are animated in code. On death
    /// the model is swapped for its "broken" variant whose pieces tumble apart.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class EnemyController : MonoBehaviour
    {
        static readonly float[] Scales = { 1.35f, 1.25f, 1.35f };

        CharacterController body;
        Transform model;
        Transform armLeft;
        Transform armRight;
        Transform head;
        Quaternion armLeftRest;
        Quaternion armRightRest;
        Quaternion headRest;
        Quaternion modelRest;
        Transform healthFill;
        GameObject healthBar;
        GameArt art;
        float walkCycle;
        float verticalSpeed;
        float punch;
        float healthShown = 1f;
        float sinceHurt = 10f;

        public EnemyBrain Brain { get; private set; }
        public bool Summoned { get; set; }
        public float DeadFor { get; private set; }

        public void Init(GameArt gameArt, EnemyBrain brain)
        {
            art = gameArt;
            Brain = brain;
            body = GetComponent<CharacterController>();
            body.height = 1.9f;
            body.radius = 0.55f;
            body.center = new Vector3(0f, 0.95f, 0f);
            body.stepOffset = 0.3f;
            gameObject.layer = 2;

            int kind = (int)brain.Kind;
            model = Instantiate(art.Skeletons[kind], transform).transform;
            model.localScale = Vector3.one * Scales[kind];
            foreach (Transform part in model.GetComponentsInChildren<Transform>(true))
            {
                if (part.name.EndsWith("_armLeft")) armLeft = part;
                else if (part.name.EndsWith("_armRight")) armRight = part;
                else if (part.name.EndsWith("_head")) head = part;
            }
            armLeft = ShoulderPivot(armLeft);
            armRight = ShoulderPivot(armRight);
            if (armLeft != null) armLeftRest = armLeft.localRotation;
            if (armRight != null) armRightRest = armRight.localRotation;
            if (head != null) headRest = head.localRotation;
            modelRest = model.localRotation;
            Fx.BlobShadow(transform, 0.75f);

            healthBar = new GameObject("Health bar");
            healthBar.transform.SetParent(transform, false);
            healthBar.transform.localPosition = new Vector3(0f, kind == (int)EnemyKind.Warrior ? 3.2f : 2.6f, 0f);
            Fx.Shape("Back", art.Quad, art.BlobShadow, healthBar.transform, Vector3.zero, new Vector3(1.3f, 0.18f, 1f));
            healthFill = Fx.Shape("Fill", art.Quad, art.RuneLit, healthBar.transform, new Vector3(0f, 0f, -0.01f), new Vector3(1.2f, 0.11f, 1f)).transform;
            healthBar.SetActive(false);
        }

        public Vector3 Eye => transform.position + Vector3.up * 1.5f;

        /// <summary>
        /// The arm meshes pivot at the model's origin, so swinging them directly would sweep them
        /// through the body. A pivot at the top of each arm turns them at the shoulder instead.
        /// </summary>
        static Transform ShoulderPivot(Transform arm)
        {
            var renderer = arm != null ? arm.GetComponent<Renderer>() : null;
            if (renderer == null)
            {
                return arm;
            }
            Bounds bounds = renderer.bounds;
            var pivot = new GameObject(arm.name + " shoulder").transform;
            pivot.SetParent(arm.parent, false);
            pivot.position = new Vector3(bounds.center.x, bounds.max.y - bounds.size.y * 0.15f, bounds.center.z);
            pivot.rotation = arm.parent.rotation;
            arm.SetParent(pivot, true);
            return pivot;
        }

        /// <summary>Moves toward the brain's intent, refusing to step off the walkable floor.</summary>
        public void Drive(float dt, EnemyIntent intent, LevelView level, Camera view)
        {
            sinceHurt += dt;
            if (Brain.IsDead)
            {
                DeadFor += dt;
                return;
            }
            Vector3 here = transform.position;
            Vector3 target = new Vector3(intent.MoveTarget.X, here.y, intent.MoveTarget.Y);
            Vector3 toTarget = target - here;
            Vector3 step = Vector3.zero;
            if (intent.Speed > 0f && toTarget.magnitude > 0.05f)
            {
                step = toTarget.normalized * Mathf.Min(intent.Speed * dt, toTarget.magnitude);
                Vector3 ahead = here + toTarget.normalized * 0.9f;
                if (level.TileAt(ahead) != Tile.Floor || level.PlatformUnder(ahead + Vector3.up * 0.1f) != null)
                {
                    step = Vector3.zero;
                }
            }
            verticalSpeed = body.isGrounded ? -2f : verticalSpeed - Tuning.Gravity * dt;
            body.Move(step + Vector3.up * verticalSpeed * dt);

            Vector3 look = new Vector3(intent.LookAt.X, here.y, intent.LookAt.Y) - here;
            if (look.sqrMagnitude > 0.04f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 1f - Mathf.Exp(-10f * dt));
            }
            AnimateBody(dt, step.magnitude / Mathf.Max(dt, 1e-4f));

            float fraction = Brain.Health / Brain.Stats.Health;
            healthShown = Mathf.MoveTowards(healthShown, fraction, dt * 1.5f);
            healthBar.SetActive(fraction < 1f && sinceHurt < 6f);
            if (healthBar.activeSelf && view != null)
            {
                healthBar.transform.rotation = view.transform.rotation;
                healthFill.localScale = new Vector3(1.2f * healthShown, 0.11f, 1f);
                healthFill.localPosition = new Vector3(-0.6f * (1f - healthShown), 0f, -0.01f);
            }
        }

        void AnimateBody(float dt, float speed)
        {
            punch = Mathf.MoveTowards(punch, 0f, dt * 4f);
            float stride = Mathf.Clamp01(speed / 3f);
            walkCycle += dt * (4f + speed * 2.2f);
            float swing = Mathf.Sin(walkCycle) * 35f * stride;
            float bob = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.12f * stride;
            bool winding = Brain.State == EnemyState.Attack;
            float raise = winding ? -70f : 0f;
            if (armLeft != null)
            {
                armLeft.localRotation = armLeftRest * Quaternion.Euler(swing + raise * 0.6f, 0f, 0f);
            }
            if (armRight != null)
            {
                armRight.localRotation = armRightRest * Quaternion.Euler(-swing + raise, 0f, 0f);
            }
            float lean = winding ? -12f : stride * 8f;
            model.localPosition = new Vector3(0f, bob, 0f);
            model.localRotation = Quaternion.Euler(lean - punch * 25f, 0f, Mathf.Sin(walkCycle * 0.5f) * 3f * stride) * modelRest;
            if (head != null)
            {
                head.localRotation = headRest * Quaternion.Euler(Mathf.Sin(Time.time * 1.7f + walkCycle) * 6f, 0f, 0f);
            }
        }

        /// <summary>Visual answer to a landed strike: a lunge forward.</summary>
        public void PlayStrike()
        {
            punch = -0.8f;
        }

        public void PlayHurt(Vector3 from)
        {
            sinceHurt = 0f;
            punch = 1f;
            Vector3 away = transform.position - from;
            away.y = 0f;
            if (away.sqrMagnitude > 0.01f && !Brain.IsDead)
            {
                body.Move(away.normalized * 0.35f);
            }
        }

        /// <summary>Swaps in the broken model and lets its bones fly apart.</summary>
        public void Shatter()
        {
            healthBar.SetActive(false);
            body.enabled = false;
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            model.gameObject.SetActive(false);
            GameObject broken = Instantiate(art.BrokenSkeletons[(int)Brain.Kind], position, rotation);
            broken.transform.localScale = model.localScale;
            foreach (MeshRenderer piece in broken.GetComponentsInChildren<MeshRenderer>())
            {
                var rigidbody = piece.gameObject.AddComponent<Rigidbody>();
                rigidbody.mass = 0.4f;
                var collider = piece.gameObject.AddComponent<BoxCollider>();
                collider.size = piece.localBounds.size * 0.7f;
                piece.gameObject.layer = 2;
                rigidbody.AddExplosionForce(220f, position + Vector3.up * 0.4f, 3f, 0.6f);
                rigidbody.AddTorque(Random.insideUnitSphere * 6f, ForceMode.Impulse);
            }
            Destroy(broken, 3.5f);
            Fx.Burst(position + Vector3.up * 1f, Fx.Hex("#e9e2cc"), 26, 5f, 0.4f, 0.8f, 1f);
            Fx.Burst(position + Vector3.up * 1.2f, Fx.Hex("#7dffb0"), 14, 2f, 0.5f, 1f, -0.2f);
        }
    }
}
