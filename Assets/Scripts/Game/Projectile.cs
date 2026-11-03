using UnityEngine;

namespace SpookyPath.Game
{
    /// <summary>A bolt in flight: the explorer's firebolt or a skeleton mage's curse. The director moves and resolves it.</summary>
    public sealed class Projectile : MonoBehaviour
    {
        public bool Hostile;
        public float Damage;
        public Vector3 Velocity;
        public float Life = 2.5f;
        public Color Colour;

        public static Projectile Launch(GameArt art, Vector3 from, Vector3 direction, float speed, float damage, bool hostile)
        {
            var go = new GameObject(hostile ? "Curse bolt" : "Firebolt");
            go.transform.position = from;
            var projectile = go.AddComponent<Projectile>();
            projectile.Hostile = hostile;
            projectile.Damage = damage;
            projectile.Velocity = direction.normalized * speed;
            projectile.Colour = hostile ? Fx.Hex("#c04dff") : Fx.Hex("#ff8a2a");
            Fx.Shape("Core", art.Sphere, hostile ? art.Curse : art.Ember, go.transform, Vector3.zero, Vector3.one * (hostile ? 0.45f : 0.4f))
                .GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ParticleSystem trail = Fx.Flame(go.transform, Vector3.zero, projectile.Colour, hostile ? Fx.Hex("#2a0040") : Fx.Hex("#ff2a00"), 0.9f);
            ParticleSystem.EmissionModule emission = trail.emission;
            emission.rateOverTime = 60f;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = projectile.Colour;
            light.range = 5f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
            return projectile;
        }
    }
}
