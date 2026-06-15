using UnityEngine;

namespace LizardCrossing
{
    public enum Sfx { Stomp, StompFar, Whoosh, Pickup, Death, Dash, UiClick, Win, CloseCall, Hit }

    /// <summary>
    /// Procedurally synthesized SFX (docs/DECISIONS.md D15) played through a
    /// small AudioSource pool. The Sfx enum is the seam where authored audio
    /// replaces synthesis in the polish phase.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        private const int Voices = 8;
        private const int SampleRate = 44100;

        private AudioSource[] _pool;
        private int _next;
        private AudioClip[] _clips;

        public void Init()
        {
            Instance = this;
            _pool = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _pool[i] = src;
            }

            _clips = new AudioClip[10];
            _clips[(int)Sfx.Stomp] = Synth("stomp", 0.45f, t =>
                Mathf.Sin(2f * Mathf.PI * 55f * t) * Decay(t, 9f) * 0.9f + Noise() * Decay(t, 26f) * 0.5f);
            _clips[(int)Sfx.StompFar] = Synth("stomp_far", 0.35f, t =>
                Mathf.Sin(2f * Mathf.PI * 48f * t) * Decay(t, 12f) * 0.55f + Noise() * Decay(t, 40f) * 0.2f);
            _clips[(int)Sfx.Whoosh] = Synth("whoosh", 0.5f, t =>
                Noise() * Mathf.Sin(Mathf.PI * t / 0.5f) * 0.35f);
            _clips[(int)Sfx.Pickup] = Synth("pickup", 0.22f, t =>
                (Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 1318f * t) * 0.3f) * Decay(t, 14f));
            _clips[(int)Sfx.Death] = Synth("death", 0.5f, t =>
                Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(320f, 70f, t / 0.5f) * t) * Decay(t, 5f) * 0.7f);
            _clips[(int)Sfx.Dash] = Synth("dash", 0.3f, t =>
                Noise() * Decay(t, 11f) * 0.3f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(200f, 700f, t / 0.3f) * t) * Decay(t, 9f) * 0.2f);
            _clips[(int)Sfx.UiClick] = Synth("click", 0.08f, t =>
                Mathf.Sin(2f * Mathf.PI * 1200f * t) * Decay(t, 60f) * 0.5f);
            _clips[(int)Sfx.Win] = Synth("win", 0.9f, t =>
            {
                // quick ascending major arpeggio
                float f = t < 0.22f ? 523f : t < 0.44f ? 659f : t < 0.66f ? 784f : 1046f;
                return Mathf.Sin(2f * Mathf.PI * f * t) * 0.45f * Mathf.Clamp01(1.2f - t);
            });
            _clips[(int)Sfx.CloseCall] = Synth("closecall", 0.25f, t =>
                Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 1400f, t / 0.25f) * t) * Decay(t, 12f) * 0.35f);
            _clips[(int)Sfx.Hit] = Synth("hit", 0.3f, t =>
                Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(420f, 120f, t / 0.3f) * t) * Decay(t, 10f) * 0.6f
                + Noise() * Decay(t, 30f) * 0.3f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Play(Sfx sfx, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (Instance == null) return;
            Instance.PlayInternal(sfx, volume, pitchJitter);
        }

        private void PlayInternal(Sfx sfx, float volume, float pitchJitter)
        {
            var clip = _clips[(int)sfx];
            if (clip == null) return;
            var src = _pool[_next];
            _next = (_next + 1) % Voices;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private static System.Random _noiseRng = new System.Random(777);
        private static float Noise() { return (float)(_noiseRng.NextDouble() * 2.0 - 1.0); }
        private static float Decay(float t, float rate) { return Mathf.Exp(-t * rate); }

        private static AudioClip Synth(string name, float seconds, System.Func<float, float> wave)
        {
            int n = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(wave(t), -1f, 1f);
            }
            // short fade-out to avoid end clicks
            int fade = Mathf.Min(256, n);
            for (int i = 0; i < fade; i++)
                data[n - 1 - i] *= i / (float)fade;

            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
