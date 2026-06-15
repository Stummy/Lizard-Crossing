using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Trauma-based shake (packet required system: CameraShake). Trauma is
    /// squared so small impacts whisper and big ones roar; Perlin noise keeps
    /// motion organic instead of jittery.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        private float _trauma;
        private float _seed;

        private void Awake()
        {
            _seed = Random.value * 100f;
        }

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        /// <summary>
        /// Decays trauma and returns this frame's local position offset and roll
        /// (degrees). Call once per LateUpdate from the camera controller.
        /// </summary>
        public void Evaluate(out Vector3 posOffset, out float rollDeg)
        {
            _trauma = Mathf.Max(0f, _trauma - GameConst.CamTraumaDecay * Time.unscaledDeltaTime);
            float shake = _trauma * _trauma;
            if (shake < 0.0001f)
            {
                posOffset = Vector3.zero;
                rollDeg = 0f;
                return;
            }

            float t = Time.unscaledTime * 22f;
            posOffset = new Vector3(
                (Mathf.PerlinNoise(_seed, t) - 0.5f) * 2f,
                (Mathf.PerlinNoise(_seed + 13f, t) - 0.5f) * 2f,
                0f) * (0.35f * shake);
            rollDeg = (Mathf.PerlinNoise(_seed + 29f, t) - 0.5f) * 2f * 6f * shake;
        }
    }
}
