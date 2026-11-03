using UnityEngine;

namespace SpookyPath.Game
{
    /// <summary>
    /// Particle effects built from code (no effect packs): sparks, flames, frost rings, healing
    /// motes, teleport swirls, lava embers and the artefact's aura, all from one soft-dot texture.
    /// </summary>
    public static class Fx
    {
        static GameArt art;

        public static void Init(GameArt gameArt) => art = gameArt;

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;

        /// <summary>A looping or one-shot particle system with sensible defaults.</summary>
        public static ParticleSystem Make(string name, Transform parent, Vector3 localPosition, bool additive = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = art != null ? (additive ? art.ParticleAdditive : art.ParticleAlpha) : null;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        static void Gradient(ParticleSystem system, Color start, Color end)
        {
            ParticleSystem.ColorOverLifetimeModule colour = system.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;
        }

        static void Shrink(ParticleSystem system)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        }

        /// <summary>A flame that licks upwards (torches, braziers, the firebolt).</summary>
        public static ParticleSystem Flame(Transform parent, Vector3 localPosition, Color core, Color tip, float scale = 1f)
        {
            ParticleSystem system = Make("Flame", parent, localPosition);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f * scale, 1.4f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f * scale, 0.7f * scale);
            main.startColor = core;
            main.gravityModifier = -0.15f;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 26f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.08f * scale;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            Gradient(system, core, tip);
            Shrink(system);
            system.Play();
            return system;
        }

        /// <summary>A one-shot burst of sparks that cleans itself up.</summary>
        public static void Burst(Vector3 position, Color color, int count, float speed, float size = 0.35f, float lifetime = 0.6f, float gravity = 0.4f)
        {
            ParticleSystem system = Make("Burst", null, position);
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.duration = 0.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            Gradient(system, color, color * 0.4f);
            Shrink(system);
            system.Play();
        }

        /// <summary>A flat ring that expands along the floor (Frost Nova, shattering, landing).</summary>
        public static void Ring(Vector3 position, Color color, float radius, int count = 70)
        {
            ParticleSystem system = Make("Ring", null, position + Vector3.up * 0.3f);
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = 0.55f;
            main.startSpeed = radius / 0.55f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startColor = color;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            shape.radiusThickness = 0f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            Gradient(system, color, color * 0.5f);
            system.Play();
        }

        /// <summary>Motes that rise around a point (healing, the exit, purified wards).</summary>
        public static ParticleSystem Motes(Transform parent, Vector3 localPosition, Color color, float radius, float rate, bool loop)
        {
            ParticleSystem system = Make("Motes", parent, localPosition);
            ParticleSystem.MainModule main = system.main;
            main.loop = loop;
            main.duration = loop ? 5f : 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startColor = color;
            main.gravityModifier = -0.12f;
            if (!loop)
            {
                main.stopAction = ParticleSystemStopAction.Destroy;
            }
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.rotation = new Vector3(90f, 0f, 0f);
            Gradient(system, color, color * 0.6f);
            system.Play();
            return system;
        }

        /// <summary>Embers drifting up from a lake of lava covering <paramref name="size"/> metres.</summary>
        public static ParticleSystem Embers(Vector3 centre, Vector2 size)
        {
            ParticleSystem system = Make("Embers", null, centre);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.maxParticles = 400;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = Hex("#ff8a2a");
            main.gravityModifier = -0.05f;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = Mathf.Clamp(size.x * size.y * 0.04f, 10f, 120f);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size.x, 0.1f, size.y);
            Gradient(system, Hex("#ffd27a"), Hex("#ff3d10"));
            system.Play();
            return system;
        }

        /// <summary>Particles spiralling around a point (teleport stones, the artefact's darkness).</summary>
        public static ParticleSystem Swirl(Transform parent, Vector3 localPosition, Color color, float radius, bool additive = true)
        {
            ParticleSystem system = Make("Swirl", parent, localPosition, additive);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.8f);
            main.startSpeed = 0.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startColor = color;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 30f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 0.2f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalY = 2.2f;
            velocity.y = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            Gradient(system, color, color * 0.5f);
            system.Play();
            return system;
        }

        /// <summary>A point light that fades out and removes itself.</summary>
        public static void Flash(Vector3 position, Color color, float intensity, float range, float seconds)
        {
            var go = new GameObject("Flash");
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            go.AddComponent<FadingLight>().Seconds = seconds;
        }

        /// <summary>A simple mesh with a material, no collider.</summary>
        public static GameObject Shape(string name, Mesh mesh, Material material, Transform parent, Vector3 localPosition, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return go;
        }

        /// <summary>A soft dark disc under a character so it reads against the floor.</summary>
        public static GameObject BlobShadow(Transform parent, float radius)
        {
            GameObject shadow = Shape("Shadow", art.Quad, art.BlobShadow, parent, new Vector3(0f, 0.04f, 0f), new Vector3(radius * 2f, radius * 2f, 1f));
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return shadow;
        }
    }

    /// <summary>Fades a light to nothing, then destroys it.</summary>
    public sealed class FadingLight : MonoBehaviour
    {
        public float Seconds = 0.3f;
        Light lit;
        float start;
        float age;

        void Awake()
        {
            lit = GetComponent<Light>();
        }

        void Start() => start = lit.intensity;

        void Update()
        {
            age += Time.deltaTime;
            lit.intensity = Mathf.Lerp(start, 0f, age / Seconds);
            if (age >= Seconds)
            {
                Destroy(gameObject);
            }
        }
    }
}
