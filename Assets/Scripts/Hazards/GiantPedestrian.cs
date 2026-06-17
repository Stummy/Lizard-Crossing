using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// A giant pedestrian that WALKS across the corridor — the realistic
    /// replacement for the disembodied shoe stride. A real humanoid model towers
    /// over the lizard from its low POV, legs and arms swinging procedurally
    /// through the Humanoid avatar's bones (no animation clip needed). Each
    /// footfall is the hazard: a descending foot telegraphs a shadow at its
    /// landing spot, then stomps. The player weaves between footfalls.
    ///
    /// Gait uses a deterministic stance model: the left foot "strikes" at gait
    /// phase pi/2 and the right at 3pi/2, so the kill zone and its telegraph are
    /// exact and fair (no guessing from skating ankle positions). When no human
    /// model is present the lane falls back to the procedural shoe hazard
    /// (see HazardLaneManager), so the game still runs asset-free.
    /// </summary>
    public class GiantPedestrian : MonoBehaviour
    {
        // ---- tuning (public so they can be tweaked live in the editor) ----
        public float height = 2.5f;       // sized to the kit's grand building doors (~2.8u);
                                          // ~12x the 0.2u lizard, a touch under a doorway
        public float thighAmp = 26f;      // deg the upper legs swing fore/aft
        public float kneeAmp = 34f;       // deg the knee flexes on the back-swing
        public float kneePhase = 0.9f;    // rad the knee flex trails the thigh swing
        public float armAmp = 18f;        // deg the arms counter-swing
        public float armDown = 72f;       // deg the arms drop from the T-pose to the sides
        public float bobAmp = 0.055f;     // world units the body bobs per step
        public float faceYaw = 0f;        // extra yaw if the model's forward isn't +Z
        public float killRadius = 0.25f;  // ground radius under a planted foot that squishes
        public float forwardReachMul = 1f;// scales how far ahead a foot plants

        const float MarginOutside = 16f;  // how far off-corridor the walk starts/ends

        // ---- gait state ----
        float _phase;
        float _cadence;       // rad/s (one step = pi)
        float _speed;         // world units/s along the walk direction
        float _legLen;        // measured hip->foot distance (for stride/foot reach)
        Vector3 _startPos, _endPos;  // walks from start to end (any horizontal direction)
        float _respawnDelay;
        bool _resting;
        float _restTimer;

        Vector3 _walkDir;
        Vector3 _lateral;     // horizontal axis the legs swing about (sagittal plane)

        // ---- rig ----
        Transform _holder;
        Animator _animator;
        Transform _hips, _spine, _thighL, _calfL, _footL, _thighR, _calfR, _footR, _armL, _armR;
        Quaternion _bThighL, _bCalfL, _bThighR, _bCalfR, _bArmL, _bArmR;

        // ---- per-foot hazard bookkeeping ----
        WarningMarker _markerL, _markerR;
        bool _struckL, _struckR;          // already resolved this foot's current strike

        /// <summary>Legacy crossing-lane spawn (kit-city fallback): walks across the
        /// corridor (±X) at the lane's Z.</summary>
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
            float stepDuration, float startDelay, float respawnDelay, float scale, float startProgress = 0f)
        {
            var go = new GameObject("GiantPedestrian");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<GiantPedestrian>();
            p.height *= scale;
            p.killRadius *= scale;
            p._respawnDelay = respawnDelay;
            p._restTimer = startDelay;
            p._resting = true; // hold off until the start delay elapses
            p._cadence = Mathf.PI / Mathf.Max(0.05f, stepDuration);

            p._startPos = start;
            p._endPos = end;
            p._walkDir = (end - start).normalized;
            p._lateral = Vector3.Cross(Vector3.up, p._walkDir).normalized;

            if (!p.BuildHuman()) { Destroy(go); return null; }

            float reach = p._legLen * Mathf.Sin(p.thighAmp * Mathf.Deg2Rad) * p.forwardReachMul;
            p._speed = (2f * reach) / Mathf.Max(0.05f, stepDuration);

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

        bool BuildHuman()
        {
            var prefab = Resources.Load<GameObject>(ModelLibrary.HumanPath);
            if (prefab == null) return false;

            _holder = new GameObject("human").transform;
            _holder.SetParent(transform, false);
            _holder.localRotation = Quaternion.LookRotation(_walkDir, Vector3.up)
                                    * Quaternion.Euler(0f, faceYaw, 0f);

            var go = Instantiate(prefab);
            go.transform.SetParent(_holder, false);
            go.transform.localScale = Vector3.one;

            // normalize to target height, base on the ground, centered on x/z
            Bounds b;
            if (!LocalBounds(go, _holder, out b)) { Destroy(_holder.gameObject); return false; }
            float h = Mathf.Max(b.size.y, 1e-4f);
            go.transform.localScale = Vector3.one * (height / h);
            if (LocalBounds(go, _holder, out b))
                go.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);

            // URP-safe textured material (never the Standard-shader magenta trap)
            var albedo = Resources.Load<Texture2D>("Models/Human/human_albedo");
            var normal = Resources.Load<Texture2D>("Models/Human/human_normal");
            var mat = MaterialCache.GetTexturedNormal(albedo, normal, Color.white, 0.28f, 1f, 1f);
            foreach (var r in _holder.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mat;
            foreach (var c in _holder.GetComponentsInChildren<Collider>())
                Destroy(c);

            _animator = go.GetComponent<Animator>();
            if (_animator == null || !_animator.isHuman) { Destroy(_holder.gameObject); return false; }

            _hips   = _animator.GetBoneTransform(HumanBodyBones.Hips);
            _spine  = _animator.GetBoneTransform(HumanBodyBones.Spine);
            _thighL = _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            _calfL  = _animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            _footL  = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _thighR = _animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            _calfR  = _animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            _footR  = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
            _armL   = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _armR   = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (_thighL == null || _footL == null || _thighR == null || _footR == null)
            { Destroy(_holder.gameObject); return false; }

            _bThighL = _thighL.localRotation; _bCalfL = _calfL.localRotation;
            _bThighR = _thighR.localRotation; _bCalfR = _calfR.localRotation;
            _bArmL = _armL != null ? _armL.localRotation : Quaternion.identity;
            _bArmR = _armR != null ? _armR.localRotation : Quaternion.identity;

            _legLen = Vector3.Distance(_thighL.position, _footL.position);

            // Drive the bones ourselves: a controller-less Animator still resamples
            // the rig back to bind pose, fighting our Pose(). Disable it and keep the
            // skinned bounds valid by hand so the giant is never wrongly culled.
            _animator.enabled = false;
            foreach (var smr in _holder.GetComponentsInChildren<SkinnedMeshRenderer>())
                smr.updateWhenOffscreen = true;
            return true;
        }

        void PlaceAtStart()
        {
            transform.position = _startPos;
            _phase = 0f;
            _struckL = _struckR = false;
            Pose(0f);
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
            // Walk as ambient cross-traffic the moment the level loads (Ready),
            // not only once the run starts — an empty alley reads as broken.
            // Only telegraph and stomp once the run is actually Playing.
            bool active = gm.State == GameState.Ready || gm.State == GameState.Playing;
            if (!active) return;
            bool playing = gm.State == GameState.Playing;

            if (_resting)
            {
                _restTimer -= Time.deltaTime;
                if (_restTimer <= 0f) { _resting = false; SetVisible(true); }
                else return;
            }

            _phase += _cadence * Time.deltaTime;
            transform.position += _walkDir * (_speed * Time.deltaTime);
            Pose(_phase);

            if (playing)
            {
                // left foot strikes at pi/2, right at 3pi/2 (mod 2pi)
                UpdateFoot(_markerL, Mathf.PI * 0.5f, ref _struckL);
                UpdateFoot(_markerR, Mathf.PI * 1.5f, ref _struckR);
            }
            else
            {
                _markerL.Hide();
                _markerR.Hide();
            }

            if (Vector3.Dot(transform.position - _endPos, _walkDir) > 0f) // walked past the end
            {
                _resting = true;
                _restTimer = _respawnDelay;
                PlaceAtStart();
            }
        }

        /// <summary>
        /// Pose every driven bone for a gait phase. Public so the editor can scrub
        /// the walk cycle without entering Play. Each bone resets to its bind
        /// rotation then swings about the world sagittal axis, so we never need to
        /// know the rig's per-bone local axes.
        /// </summary>
        public void Pose(float phase)
        {
            if (_thighL == null) return;
            float l = Mathf.Sin(phase);
            float r = Mathf.Sin(phase + Mathf.PI);

            Swing(_thighL, _bThighL, thighAmp * l);
            Swing(_thighR, _bThighR, thighAmp * r);

            // knees flex (one direction only) as each leg swings through the back
            Swing(_calfL, _bCalfL, -kneeAmp * Mathf.Max(0f, Mathf.Sin(phase + kneePhase)));
            Swing(_calfR, _bCalfR, -kneeAmp * Mathf.Max(0f, Mathf.Sin(phase + Mathf.PI + kneePhase)));

            // arms drop from the imported T-pose to the sides, then counter-swing
            PoseArm(_armL, _bArmL, -armDown, armAmp * r); // arms oppose same-side legs
            PoseArm(_armR, _bArmR,  armDown, armAmp * l);

            if (_holder != null)
            {
                Vector3 lp = _holder.localPosition;
                lp.y = bobAmp * Mathf.Abs(Mathf.Cos(phase));
                _holder.localPosition = lp;
            }
        }

        void Swing(Transform bone, Quaternion bind, float angleDeg)
        {
            if (bone == null) return;
            bone.localRotation = bind;
            bone.Rotate(_lateral, angleDeg, Space.World);
        }

        // Arms need two rotations: first drop to the sides about the walk axis
        // (flips correctly with direction since both axis and arm side flip), then
        // counter-swing fore/aft about the sagittal axis like the legs.
        void PoseArm(Transform bone, Quaternion bind, float downDeg, float swingDeg)
        {
            if (bone == null) return;
            bone.localRotation = bind;
            bone.Rotate(_walkDir, downDeg, Space.World);
            bone.Rotate(_lateral, swingDeg, Space.World);
        }

        /// <summary>
        /// Telegraph the foot's predicted landing, then resolve the stomp at strike.
        /// The landing spot is forward of the body by the leg's reach; the marker
        /// fills as the gait phase approaches the strike phase, giving a readable
        /// lead even though the body is occluded by buildings.
        /// </summary>
        void UpdateFoot(WarningMarker marker, float strikePhase, ref bool struck)
        {
            float wrapped = Mathf.Repeat(_phase - strikePhase + Mathf.PI, Mathf.PI * 2f) - Mathf.PI;
            // wrapped in (-pi, pi]: 0 == striking now, negative == approaching, positive == lifted

            Vector3 plant = PredictedPlant(strikePhase);

            if (wrapped < 0f)
            {
                // approaching: ramp the telegraph over the final ~half step
                float t = Mathf.Clamp01(1f + wrapped / (Mathf.PI * 0.9f));
                if (Mathf.Abs(plant.x) < GameConst.CorridorHalfWidth + killRadius)
                {
                    marker.SetWorldPosition(plant);
                    marker.SetIntensity(t);
                }
                else marker.Hide();
                struck = false;
            }
            else
            {
                // strike just happened (phase passed the strike point)
                if (!struck)
                {
                    struck = true;
                    // ambient foot traffic must NOT shake the screen or thud every step
                    // (dozens of pedestrians = constant trauma) — only resolve the squish.
                    if (Mathf.Abs(plant.x) < GameConst.CorridorHalfWidth + killRadius)
                        ResolveStomp(plant);
                }
                marker.Hide();
            }
        }

        Vector3 PredictedPlant(float strikePhase)
        {
            float reach = _legLen * Mathf.Sin(thighAmp * Mathf.Deg2Rad) * forwardReachMul;
            // foot lands ahead of the body along the walk direction
            Vector3 plant = transform.position + _walkDir * reach;
            plant.y = GameConst.GroundY;
            return plant;
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
