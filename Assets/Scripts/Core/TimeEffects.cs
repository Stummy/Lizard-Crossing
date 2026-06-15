using System.Collections;
using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Hit-stop and slow-motion. Effects stack safely: the strongest active
    /// effect wins, and timeScale is always restored to 1.
    /// </summary>
    public class TimeEffects : MonoBehaviour
    {
        public static TimeEffects Instance { get; private set; }

        private float _baseFixedDelta;
        private int _activeEffects;

        public void Init()
        {
            Instance = this;
            _baseFixedDelta = Time.fixedDeltaTime;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baseFixedDelta;
            if (Instance == this) Instance = null;
        }

        public void HitStop(float duration = GameConst.HitStopDuration)
        {
            StartCoroutine(ScaleRoutine(0.02f, duration));
        }

        public void SlowMo(float scale = GameConst.NearMissSlowScale, float duration = GameConst.NearMissSlowDuration)
        {
            StartCoroutine(ScaleRoutine(scale, duration));
        }

        private IEnumerator ScaleRoutine(float scale, float duration)
        {
            _activeEffects++;
            Time.timeScale = Mathf.Min(Time.timeScale, scale);
            Time.fixedDeltaTime = _baseFixedDelta * Time.timeScale;

            yield return new WaitForSecondsRealtime(duration);

            _activeEffects--;
            if (_activeEffects <= 0)
            {
                _activeEffects = 0;
                Time.timeScale = 1f;
                Time.fixedDeltaTime = _baseFixedDelta;
            }
        }
    }
}
