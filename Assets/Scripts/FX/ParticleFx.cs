using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Code-configured particle systems: stomp dust rings, pickup pops, dash
    /// scuttle dust. One pooled instance per effect type, moved + replayed.
    /// </summary>
    public class ParticleFx : MonoBehaviour
    {
        public static ParticleFx Instance { get; private set; }

        private ParticleSystem _dust;
        private ParticleSystem _pickup;
        private ParticleSystem _dash;

        public void Init()
        {
            Instance = this;
            _dust = BuildSystem("DustBurst", new Color(0.78f, 0.74f, 0.66f, 0.85f), 0.9f, 2.2f);
            _pickup = BuildSystem("PickupPop", new Color(0.55f, 1f, 0.45f, 0.95f), 0.28f, 0.45f);
            _dash = BuildSystem("DashDust", new Color(0.8f, 0.77f, 0.7f, 0.6f), 0.35f, 0.5f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private ParticleSystem BuildSystem(string name, Color color, float size, float life)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.gravityModifier = 0.35f;
            main.maxParticles = 128;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = false; // burst-only via Emit()

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.4f;

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = grad;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = MaterialCache.SoftParticle;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        public static void StompDust(Vector3 pos, float scale)
        {
            if (Instance == null) return;
            Instance._dust.transform.position = pos + Vector3.up * 0.1f;
            Instance._dust.Emit(Mathf.RoundToInt(18 * Mathf.Clamp(scale, 0.5f, 2f)));
        }

        public static void PickupPop(Vector3 pos)
        {
            if (Instance == null) return;
            Instance._pickup.transform.position = pos;
            Instance._pickup.Emit(10);
        }

        public static void DashDust(Vector3 pos)
        {
            if (Instance == null) return;
            Instance._dash.transform.position = pos + Vector3.up * 0.05f;
            Instance._dash.Emit(6);
        }
    }
}
