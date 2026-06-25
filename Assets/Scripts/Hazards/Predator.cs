using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// The alley cat: a looming predator that chases the lizard from behind (−Z),
    /// giving the run its forward pull. It rubber-bands — it gains ground when the
    /// lizard dawdles and falls back when the lizard sprints or dashes, so keeping
    /// moving keeps you alive. When it closes to striking range it swipes, which
    /// routes through GameStateManager.HitPlayer (so it eats the tail first, then a
    /// heart), and is knocked back so the chase continues.
    ///
    /// It lives behind the camera, so it's usually unseen — the HUD danger meter
    /// reads <see cref="Threat01"/> to telegraph how close it is.
    ///
    /// Uses an imported cat model (Resources/Models/cat) if present, else a
    /// procedural prowling silhouette so the game runs asset-free.
    /// </summary>
    public class Predator : MonoBehaviour
    {
        public static Predator Instance { get; private set; }

        /// <summary>0 = far / no danger, 1 = about to be caught. Drives the HUD meter.</summary>
        public float Threat01 { get; private set; }

        private Transform _target;
        private Transform _visual;
        private float _gap;          // world-units behind the lizard
        private float _xVel;         // SmoothDamp velocity for lateral tracking
        private float _nextHitTime;
        private float _bob;
        private float _lungeUntil;   // cat visual lunges into frame while > Time.time

        public static Predator Spawn(Transform parent, Transform target)
        {
            var go = new GameObject("AlleyCat");
            if (parent != null) go.transform.SetParent(parent, false);
            var p = go.AddComponent<Predator>();
            Instance = p;
            p._target = target;
            p._gap = GameConst.CatStartGap;
            p.BuildVisual();
            p.Reposition(true);
            return p;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildVisual()
        {
            // Imported cat model first (Resources/Models/cat); else a procedural cat.
            var model = ModelLibrary.TryBuild(ModelLibrary.CatKey, transform, 0.7f, 180f);
            if (model != null)
            {
                _visual = model;
                foreach (var r in _visual.GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = MaterialCache.GetUrpEquivalent(r.sharedMaterial);
                return;
            }
            _visual = BuildProceduralCat();
        }

        // A dark, low, prowling cat silhouette — body, head, ears, tail, glowing eyes.
        // Sized ~0.45u tall (≈3× the 0.15u lizard): it towers from the low POV.
        private Transform BuildProceduralCat()
        {
            var holder = new GameObject("cat").transform;
            holder.SetParent(transform, false);

            Color fur = new Color(0.13f, 0.13f, 0.16f);
            Color furDark = new Color(0.08f, 0.08f, 0.10f);
            Color eye = new Color(0.85f, 1f, 0.35f);

            Part(holder, new Vector3(0f, 0.26f, -0.05f), new Vector3(0.30f, 0.28f, 0.62f), fur);     // body
            Part(holder, new Vector3(0f, 0.30f, 0.30f), new Vector3(0.20f, 0.20f, 0.24f), fur);      // chest/neck
            var head = Part(holder, new Vector3(0f, 0.40f, 0.40f), new Vector3(0.22f, 0.20f, 0.22f), fur); // head
            // ears
            Part(holder, new Vector3(0.08f, 0.52f, 0.40f), new Vector3(0.07f, 0.12f, 0.05f), furDark);
            Part(holder, new Vector3(-0.08f, 0.52f, 0.40f), new Vector3(0.07f, 0.12f, 0.05f), furDark);
            // glowing eyes
            Part(holder, new Vector3(0.06f, 0.42f, 0.50f), new Vector3(0.05f, 0.05f, 0.03f), eye);
            Part(holder, new Vector3(-0.06f, 0.42f, 0.50f), new Vector3(0.05f, 0.05f, 0.03f), eye);
            // legs
            for (int sx = -1; sx <= 1; sx += 2)
            {
                Part(holder, new Vector3(0.11f * sx, 0.12f, 0.22f), new Vector3(0.07f, 0.24f, 0.08f), furDark);
                Part(holder, new Vector3(0.11f * sx, 0.12f, -0.22f), new Vector3(0.08f, 0.26f, 0.09f), furDark);
            }
            // tail (curving up behind)
            Part(holder, new Vector3(0f, 0.30f, -0.34f), new Vector3(0.08f, 0.08f, 0.30f), fur);
            Part(holder, new Vector3(0f, 0.42f, -0.46f), new Vector3(0.07f, 0.07f, 0.16f), fur);
            return holder;
        }

        private Transform Part(Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = p.GetComponent<Collider>(); if (col != null) Destroy(col);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = pos;
            p.transform.localScale = scale;
            p.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(color);
            return p.transform;
        }

        private void LateUpdate()
        {
            var gm = GameStateManager.Instance;
            if (gm == null || _target == null) return;

            bool playing = gm.State == GameState.Playing;
            if (!playing)
            {
                // Lurk at the starting gap until the run begins.
                Reposition(false);
                Threat01 = 0f;
                return;
            }

            // Rubber-band: gain ground when the lizard dawdles, lose it when it sprints
            // forward or dashes. Forward (+Z) progress is what buys safety.
            var player = PlayerController.Instance;
            float fwd = player != null ? player.Velocity.z : 0f;
            float run01 = Mathf.Clamp01(fwd / GameConst.LizardMoveSpeed);
            float close = Mathf.Lerp(GameConst.CatCloseRate, -GameConst.CatFallbackRate, run01);
            _gap = Mathf.Clamp(_gap - close * Time.deltaTime, GameConst.CatCatchDistance, GameConst.CatMaxGap);

            Reposition(false);

            // threat meter
            float denom = Mathf.Max(0.01f, GameConst.CatThreatNearGap - GameConst.CatCatchDistance);
            Threat01 = Mathf.Clamp01(1f - (_gap - GameConst.CatCatchDistance) / denom);

            // strike when it closes in. The cat normally rides behind the camera, so a
            // bare hit would be invisible — telegraph the swipe with a forward lunge into
            // frame, a claw puff, and camera trauma so the scratch is seen and felt.
            if (_gap <= GameConst.CatCatchDistance + 0.01f && Time.time >= _nextHitTime)
            {
                _nextHitTime = Time.time + GameConst.CatHitCooldown;
                _lungeUntil = Time.time + 0.25f;
                ParticleFx.DashDust(_target.position);            // claw scuff at the lizard
                GameEvents.RaiseHazardImpact(_target.position, 0.7f); // camera shake
                gm.HitPlayer(transform.position, DeathCause.Caught);
                _gap = Mathf.Min(GameConst.CatMaxGap, GameConst.CatThreatNearGap + 2f); // recoil so the chase resumes
            }

            // prowl bob + swipe lunge (lunge eases in then back out over the window)
            _bob += Time.deltaTime * Mathf.Lerp(6f, 12f, run01);
            if (_visual != null)
            {
                float lunge = 0f;
                if (Time.time < _lungeUntil)
                {
                    float k = 1f - (_lungeUntil - Time.time) / 0.25f; // 0..1 across the window
                    lunge = Mathf.Sin(k * Mathf.PI) * GameConst.CatLungeDistance;
                }
                _visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_bob)) * 0.03f, lunge);
            }
        }

        // Place the cat behind the lizard, tracking its lane laterally with a little lag,
        // riding the street height, and facing forward toward its prey.
        private void Reposition(bool snap)
        {
            Vector3 t = _target.position;
            float x = snap ? t.x : Mathf.SmoothDamp(transform.position.x, t.x, ref _xVel, 0.45f);
            float z = t.z - _gap;
            float y = StreetGround.HeightAt(x, z);
            transform.position = new Vector3(x, y, z);

            Vector3 toPrey = new Vector3(t.x - x, 0f, t.z - z);
            if (toPrey.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(toPrey.normalized, Vector3.up);
        }
    }
}
