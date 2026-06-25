using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Unified input seam (docs/DECISIONS.md D10).
    /// Desktop: WASD/arrows + Space dash. Touch: drag-anywhere virtual joystick;
    /// dash comes from the HUD button (which calls PressDash).
    /// TouchDriver must be ticked once per frame by Bootstrap's updater.
    /// </summary>
    public static class InputProvider
    {
        /// <summary>Test seam: when set, bot playtests drive the lizard.</summary>
        public static Vector2? MoveOverride;

        /// <summary>Test seam: set true to fire the start gate once (bot playtests).</summary>
        public static bool StartOverride;

        /// <summary>Held steer from the on-screen LEFT/RIGHT buttons: -1 (left), 0, +1 (right).
        /// Auto-run uses this (and A/D / drag) as the lizard's only directional control.</summary>
        public static float ButtonSteer;

        private static Vector2 _touchMove;
        private static bool _dashQueued;
        private static bool _touchActive;
        private static Vector2 _touchOrigin;
        private const float JoystickRadiusPx = 130f;

        /// <summary>Desired movement in screen space (x = right, y = forward), magnitude 0..1.</summary>
        public static Vector2 Move
        {
            get
            {
                if (MoveOverride.HasValue) return Vector2.ClampMagnitude(MoveOverride.Value, 1f);
                Vector2 kb = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                if (kb.sqrMagnitude > 0.01f) return Vector2.ClampMagnitude(kb, 1f);
                if (Mathf.Abs(ButtonSteer) > 0.01f) return new Vector2(Mathf.Clamp(ButtonSteer, -1f, 1f), 0f);
                return _touchMove;
            }
        }

        public static bool ConsumeDash()
        {
            bool dash = _dashQueued || Input.GetKeyDown(KeyCode.Space);
            _dashQueued = false;
            return dash;
        }

        /// <summary>Called by the on-screen dash button.</summary>
        public static void PressDash() { _dashQueued = true; }

        public static bool AnyStartPressed()
        {
            if (StartOverride) { StartOverride = false; return true; }
            return Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.anyKeyDown;
        }

        /// <summary>Tick from a MonoBehaviour Update. Maintains the virtual joystick.</summary>
        public static void Tick()
        {
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    _touchActive = true;
                    _touchOrigin = t.position;
                    _touchMove = Vector2.zero;
                }
                else if (_touchActive && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
                {
                    Vector2 delta = t.position - _touchOrigin;
                    _touchMove = Vector2.ClampMagnitude(delta / JoystickRadiusPx, 1f);
                    // drift the origin toward the finger so reversals feel instant
                    if (delta.magnitude > JoystickRadiusPx)
                        _touchOrigin = t.position - delta.normalized * JoystickRadiusPx;
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    _touchActive = false;
                    _touchMove = Vector2.zero;
                }
            }
            else if (_touchActive)
            {
                _touchActive = false;
                _touchMove = Vector2.zero;
            }
        }

        public static void Reset()
        {
            MoveOverride = null;
            StartOverride = false;
            ButtonSteer = 0f;
            _touchMove = Vector2.zero;
            _dashQueued = false;
            _touchActive = false;
        }
    }
}
