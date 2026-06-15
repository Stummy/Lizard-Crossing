using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Shared construction + hit logic for footstep hazards: shoe meshes,
    /// pant-leg cylinders that exit the top of frame (docs/DECISIONS.md D13),
    /// and the sole footprint hit test. Warning telegraphs live in WarningMarker.
    /// </summary>
    public static class HazardParts
    {
        // color-blocked sneaker bodies + a vivid accent each (reference look)
        public static readonly Color[] ShoeColors =
        {
            new Color(0.12f, 0.58f, 0.6f),  // teal
            new Color(0.9f, 0.9f, 0.92f),   // white
            new Color(0.18f, 0.22f, 0.45f), // navy
            new Color(0.9f, 0.45f, 0.32f),  // coral
        };

        public static readonly Color[] AccentColors =
        {
            new Color(0.92f, 0.28f, 0.5f),  // magenta
            new Color(0.6f, 0.85f, 0.2f),   // lime
            new Color(0.95f, 0.6f, 0.2f),   // orange
            new Color(0.2f, 0.72f, 0.72f),  // teal
        };

        public static readonly Color[] PantColors =
        {
            new Color(0.28f, 0.34f, 0.5f),  // denim
            new Color(0.22f, 0.22f, 0.24f), // black
            new Color(0.55f, 0.5f, 0.42f),  // khaki
        };

        /// <summary>
        /// Builds a giant shoe (sole 11 x 4.5 by default) with a pant leg rising
        /// out of frame. Pivot of the returned transform is the sole's
        /// ground-contact center.
        /// </summary>
        public static Transform BuildShoe(Transform parent, Color shoeColor, Color accentColor,
            Color pantColor, float soleLength, float soleWidth)
        {
            var root = new GameObject("Shoe").transform;
            root.SetParent(parent, false);

            // imported sneaker model when present; otherwise the procedural sneaker
            if (ModelLibrary.TryBuild(ModelLibrary.SneakerKey, root, soleLength, 0f) == null)
                BuildProceduralShoe(root, shoeColor, accentColor, pantColor, soleLength, soleWidth);

            // blocking collider so a planted shoe is a solid wall to the lizard
            var col = root.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 2.2f, 0f);
            col.size = new Vector3(soleWidth, 4.6f, soleLength);
            col.enabled = false;
            return root;
        }

        private static void BuildProceduralShoe(Transform root, Color shoeColor, Color accentColor,
            Color pantColor, float soleLength, float soleWidth)
        {
            float w = soleWidth, L = soleLength;
            Color white = new Color(0.96f, 0.95f, 0.9f);

            // dark tread outsole
            Box(root, new Vector3(0f, 0.32f, 0f), new Vector3(w, 0.64f, L), new Color(0.16f, 0.16f, 0.18f));
            // chunky white midsole with a slight overhang
            Box(root, new Vector3(0f, 1.1f, 0f), new Vector3(w * 1.05f, 1.3f, L), white);
            // heel air-unit bubble (accent pop)
            var bubble = Sphere(root, new Vector3(0f, 0.95f, -L * 0.32f),
                new Vector3(w * 0.82f, 0.95f, L * 0.26f), accentColor);
            bubble.name = "AirBubble";

            // two-tone upper: main body + rounded toe box
            Box(root, new Vector3(0f, 2.5f, -L * 0.06f), new Vector3(w * 0.9f, 2.4f, L * 0.74f), shoeColor);
            Sphere(root, new Vector3(0f, 2.0f, L * 0.33f), new Vector3(w * 0.94f, 2.0f, L * 0.48f), shoeColor);
            // accent side panel (the "swoosh") + heel counter
            Box(root, new Vector3(0f, 2.0f, L * 0.02f), new Vector3(w * 1.0f, 1.0f, L * 0.46f), accentColor);
            Box(root, new Vector3(0f, 2.9f, -L * 0.42f), new Vector3(w * 0.9f, 2.0f, L * 0.16f), accentColor);

            // tongue + laces
            Box(root, new Vector3(0f, 3.5f, L * 0.04f), new Vector3(w * 0.58f, 1.5f, L * 0.12f),
                Color.Lerp(shoeColor, white, 0.6f));
            for (int k = 0; k < 3; k++)
                Box(root, new Vector3(0f, 3.35f - k * 0.35f, L * (0.12f - k * 0.09f)),
                    new Vector3(w * 0.66f, 0.16f, 0.18f), white);

            // sock collar, then a thin denim leg rising out of frame (human stays mythic)
            var collar = Cylinder(root, new Vector3(0f, 4.4f, -L * 0.18f),
                new Vector3(w * 0.85f, 0.7f, w * 0.85f), new Color(0.92f, 0.92f, 0.95f));
            collar.name = "Collar";
            var leg = Cylinder(root, new Vector3(0f, 5.2f + 15f, -L * 0.18f),
                new Vector3(w * 0.95f, 15f, w * 0.95f), pantColor);
            leg.name = "PantLeg";
            Box(root, new Vector3(0f, 5.4f, -L * 0.18f), new Vector3(w, 0.5f, w),
                Color.Lerp(pantColor, Color.black, 0.2f));
        }

        /// <summary>
        /// True if the player is under the sole footprint (in the shoe's local
        /// frame), with a forgiveness margin trimmed off the edges.
        /// </summary>
        public static bool FootprintContains(Transform shoe, Vector3 worldPos,
            float soleLength, float soleWidth)
        {
            Vector3 local = shoe.InverseTransformPoint(worldPos);
            float halfW = soleWidth * 0.5f - GameConst.StompKillPad;
            float halfL = soleLength * 0.5f - GameConst.StompKillPad;
            return Mathf.Abs(local.x) <= halfW && Mathf.Abs(local.z) <= halfL;
        }

        /// <summary>Impact feedback shared by every footfall.</summary>
        public static void DoImpact(Vector3 landPos, float dustScale)
        {
            var player = PlayerController.Instance;
            float dist = player != null
                ? Vector3.Distance(landPos, player.transform.position) : 100f;
            float severity = Mathf.Clamp01(1f - dist / 22f);

            GameEvents.RaiseHazardImpact(landPos, severity);
            ParticleFx.StompDust(landPos, dustScale);
            GameAudio.Play(dist < 14f ? Sfx.Stomp : Sfx.StompFar,
                Mathf.Lerp(0.4f, 1f, severity));
        }

        /// <summary>Hit / near-miss resolution after a footfall lands.</summary>
        public static void ResolveSlam(Transform shoe, Vector3 landPos, float soleLength, float soleWidth)
        {
            var player = PlayerController.Instance;
            var gm = GameStateManager.Instance;
            if (player == null || gm == null || gm.State != GameState.Playing) return;

            Vector3 p = player.KillCheckPosition;
            // airborne (a jump) or invulnerable (revive / camouflage) clears the footfall
            if (!player.IsInvulnerable && !player.IsAirborne && FootprintContains(shoe, p, soleLength, soleWidth))
            {
                gm.HitPlayer(landPos);
            }
            else
            {
                float dist = Vector2.Distance(new Vector2(landPos.x, landPos.z), new Vector2(p.x, p.z));
                if (dist < GameConst.CloseCallRadius + soleWidth * 0.5f)
                    GameEvents.RaiseNearMiss(landPos);
            }
        }

        private static GameObject Box(Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            return Primitive(PrimitiveType.Cube, parent, pos, scale, color);
        }

        private static GameObject Sphere(Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            return Primitive(PrimitiveType.Sphere, parent, pos, scale, color);
        }

        private static GameObject Cylinder(Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            return Primitive(PrimitiveType.Cylinder, parent, pos, scale, color);
        }

        private static GameObject Primitive(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>()); // blocking handled by the root box
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(color);
            return go;
        }
    }
}
