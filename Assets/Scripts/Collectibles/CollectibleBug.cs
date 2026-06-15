using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Collectible bug (packet required system: CollectibleBug): a hovering fly
    /// with flapping wings and a soft ground glow for readability. Magnetizes
    /// to the lizard at close range, pops with particles + chirp on pickup.
    /// </summary>
    public class CollectibleBug : MonoBehaviour
    {
        private const float MagnetRadius = 3.2f;
        private const float CollectRadius = 0.95f;
        private const float MagnetSpeed = 9f;

        private Transform _wingL;
        private Transform _wingR;
        private float _phase;
        private float _baseY;

        public static CollectibleBug Spawn(Transform parent, Vector3 groundPos, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = groundPos + Vector3.up * 0.75f;
            var bug = go.AddComponent<CollectibleBug>();
            bug.Construct();
            return bug;
        }

        private void Construct()
        {
            _phase = Random.value * 10f;
            _baseY = transform.position.y;

            // body + head
            Part(PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.42f, 0.36f, 0.55f),
                new Color(0.2f, 0.14f, 0.1f));
            Part(PrimitiveType.Sphere, new Vector3(0f, 0.06f, 0.3f), new Vector3(0.26f, 0.24f, 0.26f),
                new Color(0.12f, 0.09f, 0.07f));
            // iridescent back shimmer
            Part(PrimitiveType.Sphere, new Vector3(0f, 0.12f, -0.05f), new Vector3(0.3f, 0.18f, 0.4f),
                new Color(0.25f, 0.5f, 0.45f));

            _wingL = Wing(-1);
            _wingR = Wing(1);

            // ground glow so risk/reward placement reads from the low camera
            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(glow.GetComponent<Collider>());
            glow.name = "BugGlow";
            glow.transform.SetParent(transform, false);
            glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            glow.transform.localPosition = new Vector3(0f, -(_baseY - 0.04f), 0f);
            glow.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            var gr = glow.GetComponent<Renderer>();
            gr.material = new Material(MaterialCache.SoftParticle);
            gr.material.color = new Color(0.65f, 1f, 0.45f, 0.32f);
            gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Part(PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
        {
            var p = GameObject.CreatePrimitive(type);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(transform, false);
            p.transform.localPosition = localPos;
            p.transform.localScale = localScale;
            p.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(color);
        }

        private Transform Wing(int side)
        {
            var wing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(wing.GetComponent<Collider>());
            wing.name = "Wing";
            wing.transform.SetParent(transform, false);
            wing.transform.localPosition = new Vector3(side * 0.16f, 0.18f, -0.02f);
            wing.transform.localScale = new Vector3(0.42f, 0.7f, 1f);
            var r = wing.GetComponent<Renderer>();
            r.material = new Material(MaterialCache.SoftParticle);
            r.material.color = new Color(0.9f, 0.95f, 1f, 0.55f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return wing.transform;
        }

        private void Update()
        {
            float t = Time.time + _phase;

            // hover bob + lazy yaw
            Vector3 p = transform.position;
            p.y = _baseY + Mathf.Sin(t * 2.4f) * 0.16f;
            transform.position = p;
            transform.rotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.7f) * 50f, 0f);

            // wing buzz
            float flap = Mathf.Sin(t * 38f) * 55f;
            _wingL.localRotation = Quaternion.Euler(-20f, 0f, 35f + flap);
            _wingR.localRotation = Quaternion.Euler(-20f, 0f, -35f - flap);

            var gm = GameStateManager.Instance;
            var player = PlayerController.Instance;
            if (gm == null || player == null || gm.State != GameState.Playing) return;

            Vector3 toPlayer = player.transform.position + Vector3.up * 0.5f - transform.position;
            float dist = toPlayer.magnitude;

            if (dist < CollectRadius)
            {
                gm.CollectBug();
                ParticleFx.PickupPop(transform.position);
                GameAudio.Play(Sfx.Pickup);
                Destroy(gameObject);
            }
            else if (dist < MagnetRadius)
            {
                transform.position += toPlayer.normalized * (MagnetSpeed * Time.deltaTime);
                _baseY = transform.position.y;
            }
        }
    }
}
