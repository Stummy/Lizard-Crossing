using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Built-in RP image-effect that grades the gameplay frame for a premium
    /// mobile look (contrast, saturation, warm tint, vignette). Sits on the
    /// gameplay camera, below the screen-space HUD so the HUD stays ungraded and
    /// crisp. Cheap: one full-screen blit.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraGrade : MonoBehaviour
    {
        private Material _mat;

        public void Setup()
        {
            var shader = Shader.Find("Hidden/LizardCrossing/Grade");
            if (shader == null) { enabled = false; return; }
            _mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _mat.SetFloat("_Contrast", 1.1f);
            _mat.SetFloat("_Saturation", 1.22f);
            _mat.SetColor("_TintColor", new Color(1.06f, 1.0f, 0.9f, 1f));
            _mat.SetFloat("_WarmTint", 0.6f);
            _mat.SetFloat("_Vignette", 0.95f);
            _mat.SetFloat("_VignetteStrength", 0.42f);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (_mat == null) { Graphics.Blit(src, dst); return; }
            Graphics.Blit(src, dst, _mat);
        }
    }
}
