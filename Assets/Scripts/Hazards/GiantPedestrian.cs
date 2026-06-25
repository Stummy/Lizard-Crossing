using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// A pedestrian that walks/runs across or along the street using a real
    /// imported casual character (npc_casual_set_00) driven by Humanoid walk/run
    /// animation clips (Kevin Iglesias). The legs are animated by the clip — so a
    /// walker looks like it's walking and a runner like it's running — and the
    /// squish hazard is synced to the ACTUAL footfall: we watch the live foot
    /// bones each frame and the stomp resolves exactly when a foot plants on the
    /// pavement. The player weaves between the planting feet.
    ///
    /// The character mesh, materials and Humanoid avatar all come from the prefab;
    /// we only assign the shared locomotion controller, drive forward translation
    /// in code, and keep the body grounded on the curb/road surface. When no NPC
    /// prefab is present the lane manager falls back to the procedural shoe hazard,
    /// so the game still runs asset-free.
    /// </summary>
    public class GiantPedestrian : MonoBehaviour
    {
        // ---- asset paths (under a Resources/ folder so the runtime-built world loads them) ----
        const string PrefabFolder = "NPC";                  // Resources/NPC/ped_*.prefab
        const string ControllerPath = "NPC/PedestrianLocomotion";

        // ---- tuning (public so they can be tweaked live in the editor) ----
        public float height = 2.5f;       // sized to the kit's grand building doors (~2.8u);
                                          // ~12x the 0.2u lizard, a touch under a doorway
        public float faceYaw = 0f;        // extra yaw if the model's forward isn't +Z
        public float killRadius = 0.25f;  // ground radius under a planted foot that squishes
        public float groundSink = 0.0f;   // small downward nudge so soles meet the pavement

        const float MarginOutside = 16f;  // how far off-corridor the walk starts/ends

        // foot-lift heights (scaled by height) that bound the telegraph/strike window
        const float TelegraphLift = 0.22f; // a descending foot this close to the ground telegraphs
        const float StrikeLift = 0.06f;    // ...and resolves the stomp once it plants this low

        // ---- motion state ----
        bool _running;
        bool _sprint;
        float _ankleHeight;   // ground offset: gap from the ankle bone down to the sole

        const float AnkleFraction = 0.05f; // sole sits ~5% of body height below the ankle bone
        float _speed;         // world units/s along the walk direction
        Vector3 _startPos, _endPos;  // walks from start to end (any horizontal direction)
        float _respawnDelay;
        bool _resting;
        float _restTimer;

        Vector3 _walkDir;

        // ---- rolling (player-relative) recycle: sidewalk streams ----
        bool _rolling;
        float _laneX, _groundY, _aheadMin, _aheadMax, _behindCull;

        // ---- rig ----
        Transform _holder;
        Animator _animator;
        Transform _footL, _footR;

        // ---- per-foot footfall tracking (drives the squish hazard) ----
        WarningMarker _markerL, _markerR;
        FootTrack _trackL, _trackR;

        struct FootTrack { public float prevY; public bool wasDescending; public bool struck; public bool init; }

        // shared, loaded once
        static GameObject[] _prefabs;
        static RuntimeAnimatorController _controller;

        /// <summary>Legacy crossing-lane spawn: walks across the corridor (±X) at the lane's Z.</summary>
        public static GiantPedestrian Spawn(Transform parent, LaneSpec lane)
        {
            int dir = lane.Dir >= 0 ? 1 : -1;
            float margin = GameConst.CorridorHalfWidth + MarginOutside;
            Vector3 start = new Vector3(dir > 0 ? -margin : margin, GameConst.GroundY, lane.Z);
            Vector3 end = new Vector3(-start.x, GameConst.GroundY, lane.Z);
            return SpawnTrack(parent, start, end, lane.StepDuration, lane.StartDelay, lane.RespawnDelay, lane.Scale);
        }

        /// <summary>
        /// Walk from <paramref name="start"/> to <paramref name="end"/> along any
        /// horizontal direction, then recycle. Used for street traffic: a pedestrian
        /// walking the sidewalk down the avenue (±Z) is just start/end along Z.
        /// </summary>
        public static GiantPedestrian SpawnTrack(Transform parent, Vector3 start, Vector3 end,
            float stepDuration, float startDelay, float respawnDelay, float scale,
            float startProgress = 0f, bool running = false, bool sprint = false)
        {
            var go = new GameObject(sprint ? "Sprinter" : running ? "Runner" : "GiantPedestrian");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<GiantPedestrian>();
            p.height *= scale;
            p.killRadius *= scale;
            p._respawnDelay = respawnDelay;
            p._restTimer = startDelay;
            p._resting = true; // hold off until the start delay elapses
            p._sprint = sprint;
            p._running = running || sprint;

            p._startPos = start;
            p._endPos = end;
            p._walkDir = (end - start).normalized;

            if (!p.BuildHuman()) { Destroy(go); return null; }

            // Translation speed is set directly per gait tier (real metres/second, scaled)
            // and matched to the clip's natural ground speed so the legs don't skate. The
            // animator plays each clip at ~1x (set in BuildHuman) so a runner actually
            // looks like it's running — the old "step duration" coupling slowed the run
            // clip below 1x, which read as a slow walk while the body glided forward.
            float moveSpeed = sprint ? Random.Range(5.5f, 7.5f)
                            : running ? Random.Range(3.8f, 5.0f)
                                      : Random.Range(1.3f, 2.1f);
            p._speed = moveSpeed * scale;

            float footW = p.killRadius * 2f;
            p._markerL = WarningMarker.Create(go.transform, footW, footW);
            p._markerR = WarningMarker.Create(go.transform, footW, footW);

            p.PlaceAtStart();
            if (startProgress > 0f) // pre-distribute along the track so the street is busy at once
            {
                p.transform.position = Vector3.Lerp(start, end, startProgress);
                p._resting = false;
                p.SetVisible(true);
            }
            return p;
        }

        /// <summary>
        /// A sidewalk pedestrian walking ALONG the avenue (±Z) in a player-relative
        /// rolling window. <paramref name="dir"/> &gt; 0 strolls forward (away from the
        /// lizard); &lt; 0 is oncoming (toward it). Whichever way it faces, it is always
        /// recycled to a fresh spot AHEAD of the lizard once overtaken or drifted off,
        /// so the crowd never streams up from behind.
        /// </summary>
        public static GiantPedestrian SpawnSidewalk(Transform parent, float laneX, float groundY, int dir,
            float stepDuration, bool running, float aheadMin, float aheadMax, float behindCull, float initialZ,
            bool sprint = false)
        {
            Vector3 dirVec = new Vector3(0f, 0f, dir >= 0 ? 1f : -1f);
            Vector3 start = new Vector3(laneX, groundY, initialZ);
            Vector3 end = start + dirVec * 1000f; // rolling recycle handles the real wrap, not this end
            var p = SpawnTrack(parent, start, end, stepDuration, 0f, 0f, 1f, 0f, running, sprint);
            if (p == null) return null;
            p._rolling = true;
            p._laneX = laneX;
            p._groundY = groundY;
            p._aheadMin = aheadMin;
            p._aheadMax = aheadMax;
            p._behindCull = behindCull;
            p.transform.position = start;
            p._resting = false;
            p.SetVisible(true);
            return p;
        }

        bool BuildHuman()
        {
            if (_prefabs == null)
            {
                _prefabs = Resources.LoadAll<GameObject>(PrefabFolder);
                _controller = Resources.Load<RuntimeAnimatorController>(ControllerPath);
            }
            if (_prefabs == null || _prefabs.Length == 0 || _controller == null) return false;

            _holder = new GameObject("human").transform;
            _holder.SetParent(transform, false);
            _holder.localRotation = Quaternion.LookRotation(_walkDir, Vector3.up)
                                    * Quaternion.Euler(0f, faceYaw, 0f);

            var prefab = _prefabs[Random.Range(0, _prefabs.Length)];
            var go = Instantiate(prefab);
            go.transform.SetParent(_holder, false);
            go.transform.localScale = Vector3.one;

            // normalize to target height, base on the ground, centered on x/z (keep
            // the prefab's own materials/textures — they're already clothed & textured)
            Bounds b;
            if (!LocalBounds(go, _holder, out b)) { Destroy(_holder.gameObject); return false; }
            float h = Mathf.Max(b.size.y, 1e-4f);
            go.transform.localScale = Vector3.one * (height / h);
            if (LocalBounds(go, _holder, out b))
                go.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);

            foreach (var c in _holder.GetComponentsInChildren<Collider>())
                Destroy(c);

            // The prefab's imported materials use a Standard/HDRP shader that renders
            // magenta under this project's URP pipeline. Remap each one to a URP/Lit
            // equivalent (per submesh, so face/shirt/pants/hair keep their own texture).
            foreach (var r in _holder.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = MaterialCache.GetUrpEquivalent(mats[i]);
                r.sharedMaterials = mats;
            }

            _animator = go.GetComponent<Animator>();
            if (_animator == null) _animator = go.GetComponentInChildren<Animator>();
            if (_animator == null || !_animator.isHuman) { Destroy(_holder.gameObject); return false; }

            _footL = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _footR = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (_footL == null || _footR == null) { Destroy(_holder.gameObject); return false; }

            // Sole-below-ankle gap used to plant the foot. Grounding reads the ankle
            // bones (cheap, exact) instead of aggregate skinned bounds — the modular
            // character has many sub-meshes whose loose bounds dipped below the soles,
            // which both lifted the body (the hover) and forced costly per-frame skin
            // bounds (updateWhenOffscreen) that tanked the framerate.
            _ankleHeight = height * AnkleFraction;

            // Drive the rig with the locomotion clip. Forward translation is in code,
            // not root motion. Cull animation when off-screen (CullUpdateTransforms):
            // an unseen pedestrian can't squish the lizard, so skipping its rig sampling
            // is free perf for a big crowd — only the visible peds (the ones that can hit
            // the player) pay the animation cost.
            _animator.runtimeAnimatorController = _controller;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // Cadence must MATCH the ground speed or the gait misreads. Runners
            // translate at 3.8-5.0 m/s and sprinters 5.5-7.5 — well above a walk — so
            // the leg clip has to cycle fast enough to cover that ground, else the body
            // glides forward on under-cycling legs and a run reads as a brisk walk
            // (exactly the bug owner saw). Run/sprint clip speeds raised so the legs
            // visibly pump at run cadence and the feet keep up with the body.
            // (Tune in-editor: faster = busier legs / less forward skate; too fast skates
            // backward.)
            _animator.speed = _sprint ? Random.Range(1.5f, 1.8f)
                            : _running ? Random.Range(1.2f, 1.45f)
                                       : Random.Range(0.9f, 1.1f);
            string state = _sprint ? "Sprint" : _running ? "Run" : "Walk";
            _animator.Play(state, 0, Random.value); // desync the crowd

            return true;
        }

        void PlaceAtStart()
        {
            transform.position = _startPos;
            _trackL = default(FootTrack);
            _trackR = default(FootTrack);
            SetVisible(false);
            if (_markerL != null) _markerL.Hide();
            if (_markerR != null) _markerR.Hide();
        }

        void SetVisible(bool on)
        {
            if (_holder != null) _holder.gameObject.SetActive(on);
        }

        void LateUpdate()
        {
            var gm = GameStateManager.Instance;
            if (gm == null) return;
            // Walk as ambient traffic the moment the level loads (Ready), not only
            // once the run starts — an empty street reads as broken. Only telegraph
            // and stomp once the run is actually Playing.
            bool active = gm.State == GameState.Ready || gm.State == GameState.Playing;
            if (!active) return;
            bool playing = gm.State == GameState.Playing;

            if (_resting)
            {
                _restTimer -= Time.deltaTime;
                if (_restTimer <= 0f) { _resting = false; SetVisible(true); }
                else return;
            }

            // Steer around solid street props (rubble, furniture) registered in ObstacleField
            // instead of walking through them: a lateral nudge away from anything in the path.
            Vector3 avoid = ObstacleField.Avoidance(transform.position, _walkDir,
                GameConst.PedAvoidRadius, GameConst.PedAvoidLookahead, _speed);
            transform.position += (_walkDir * _speed + avoid) * Time.deltaTime;

            // One ground raycast per frame, shared by grounding and both feet (the feet
            // are <1u apart, so the body-centre surface height is close enough). Cheaper
            // than three raycasts per pedestrian across a whole crowd.
            float ground = StreetGround.HeightAt(transform.position.x, transform.position.z);
            GroundBody(ground);

            if (playing)
            {
                TrackFoot(_footL, _markerL, ref _trackL, ground);
                TrackFoot(_footR, _markerR, ref _trackR, ground);
                CheckFootBump();
            }
            else
            {
                _markerL.Hide();
                _markerR.Hide();
            }

            if (_rolling)
            {
                RecycleRolling();
            }
            else if (Vector3.Dot(transform.position - _endPos, _walkDir) > 0f) // walked past the end
            {
                _resting = true;
                _restTimer = _respawnDelay;
                PlaceAtStart();
            }
        }

        /// <summary>
        /// Pin the planted foot's sole to the pavement off the live ANKLE bones. The
        /// clips plant the foot at the avatar's root plane (well above the bind-pose
        /// feet on these rigs), so a one-time bind-pose seat hovers; instead, every
        /// frame we drop the body so the lower ankle's sole (ankle minus _ankleHeight)
        /// rests on the surface under it. Reading the foot's own XZ for the ground
        /// height grounds it to whatever it's over (sidewalk or road), so curbs are
        /// crossed smoothly. Two bone reads per frame — no skinned-bounds cost.
        /// </summary>
        void GroundBody(float ground)
        {
            if (_footL == null || _footR == null) return;

            Transform planted = _footL.position.y <= _footR.position.y ? _footL : _footR;
            float soleY = planted.position.y - _ankleHeight;

            Vector3 pos = transform.position;
            pos.y += (ground - groundSink) - soleY; // moving the body moves soleY equally → settles in one step
            transform.position = pos;
        }

        void RecycleRolling()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            float pz = player.KillCheckPosition.z;
            float z = transform.position.z;
            if (z < pz - _behindCull || z > pz + _aheadMax + _behindCull)
            {
                float nz = pz + Random.Range(_aheadMin, _aheadMax);
                transform.position = new Vector3(_laneX, _groundY, nz);
                _trackL = default(FootTrack);
                _trackR = default(FootTrack);
            }
        }

        /// <summary>
        /// Watch one foot's live height. While it descends toward the pavement inside
        /// the corridor, telegraph the landing; the instant it stops descending near
        /// the ground (the footfall) resolve the squish exactly under it. Reading the
        /// real bone means the kill is in sync with the animation, whatever the clip.
        /// </summary>
        void TrackFoot(Transform foot, WarningMarker marker, ref FootTrack t, float groundY)
        {
            float lift = foot.position.y - groundY;            // height of the foot above ground
            float teleLift = TelegraphLift * (height / 2.5f);
            float strikeLift = StrikeLift * (height / 2.5f);

            float dy = t.init ? foot.position.y - t.prevY : 0f;
            bool descending = dy < -1e-4f;

            Vector3 plant = foot.position;
            plant.y = GameConst.GroundY;
            // The play corridor is centred on CorridorCenterX (~9 on the NYC sidewalk), NOT
            // world x=0. Gating on Abs(plant.x) left the lizard's right band (x>~9.3, where it
            // starts) outside the stomp zone, so the right-sidewalk lanes' feet never landed a
            // squish. Centre the gate on the corridor so the whole band is covered.
            bool inCorridor = Mathf.Abs(plant.x - GameConst.CorridorCenterX) < GameConst.CorridorHalfWidth + killRadius;

            if (descending && lift < teleLift && inCorridor)
            {
                marker.SetWorldPosition(plant);
                marker.SetIntensity(Mathf.Clamp01(1f - lift / teleLift));
                t.struck = false;
            }
            else if (t.init && t.wasDescending && !descending && lift < strikeLift)
            {
                // footfall: the descending foot just planted
                if (!t.struck)
                {
                    t.struck = true;
                    if (inCorridor) ResolveStomp(plant);
                }
                marker.Hide();
            }
            else if (lift > teleLift)
            {
                marker.Hide();
                t.struck = false;
            }

            t.prevY = foot.position.y;
            t.wasDescending = descending;
            t.init = true;
        }

        /// <summary>
        /// Head-on body collision: the lizard ran into this pedestrian's leg column.
        /// Distinct from the overhead foot-plant squish (<see cref="ResolveStomp"/>) — a
        /// leg is a standing vertical obstacle, so we test horizontal nearness to the
        /// body centre regardless of foot height. Non-damaging: it routes to FootBump,
        /// which staggers the lizard and wakes the cat. The player owns the re-trigger
        /// cooldown (CanFootBump) so one walk-through can't chain-stagger.
        /// </summary>
        void CheckFootBump()
        {
            var player = PlayerController.Instance;
            var gm = GameStateManager.Instance;
            if (player == null || gm == null) return;
            if (player.IsAirborne || player.IsInvulnerable || !player.CanFootBump) return;

            Vector3 p = player.KillCheckPosition;
            float r = GameConst.FootBumpRadius * (height / 2.5f); // widen with the pedestrian's size
            float dx = transform.position.x - p.x;
            float dz = transform.position.z - p.z;
            if (dx * dx + dz * dz <= r * r)
                gm.FootBump(transform.position);
        }

        void ResolveStomp(Vector3 plant)
        {
            var player = PlayerController.Instance;
            var gm = GameStateManager.Instance;
            if (player == null || gm == null || gm.State != GameState.Playing) return;

            Vector3 p = player.KillCheckPosition;
            float dist = Vector2.Distance(new Vector2(plant.x, plant.z), new Vector2(p.x, p.z));
            float kill = killRadius - GameConst.StompKillPad;

            if (!player.IsInvulnerable && !player.IsAirborne && dist <= kill)
                gm.HitPlayer(plant);
            else if (dist < GameConst.CloseCallRadius + killRadius)
                GameEvents.RaiseNearMiss(plant);
        }

        // Combined renderer bounds of go expressed in space's local frame.
        static bool LocalBounds(GameObject go, Transform space, out Bounds bounds)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            bounds = default(Bounds);
            bool has = false;
            for (int i = 0; i < rends.Length; i++)
            {
                Bounds wb = rends[i].bounds;
                Vector3 c = wb.center, e = wb.extents;
                for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 corner = c + new Vector3(e.x * sx, e.y * sy, e.z * sz);
                    Vector3 lp = space.InverseTransformPoint(corner);
                    if (!has) { bounds = new Bounds(lp, Vector3.zero); has = true; }
                    else bounds.Encapsulate(lp);
                }
            }
            return has;
        }
    }
}
