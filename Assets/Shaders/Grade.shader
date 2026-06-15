Shader "Hidden/LizardCrossing/Grade"
{
    // Full-screen color grade for the premium mobile pop (docs/DECISIONS.md):
    // contrast, saturation, a warm tint, and a vignette that focuses the eye on
    // the lizard. Applied as a Built-in RP image effect on the gameplay camera.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Contrast ("Contrast", Float) = 1.09
        _Saturation ("Saturation", Float) = 1.2
        _TintColor ("Tint", Color) = (1.06, 1.0, 0.92, 1)
        _WarmTint ("Warm Tint Amount", Float) = 0.6
        _Vignette ("Vignette", Float) = 0.9
        _VignetteStrength ("Vignette Strength", Float) = 0.45
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Contrast;
            float _Saturation;
            float4 _TintColor;
            float _WarmTint;
            float _Vignette;
            float _VignetteStrength;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);

                // contrast around mid-grey
                c.rgb = (c.rgb - 0.5) * _Contrast + 0.5;

                // saturation
                float l = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(float3(l, l, l), c.rgb, _Saturation);

                // warm tint
                c.rgb *= lerp(float3(1, 1, 1), _TintColor.rgb, _WarmTint);

                // vignette
                float2 d = (i.uv - 0.5) * 2.0;
                float vig = saturate(1.0 - dot(d, d) * _Vignette);
                c.rgb *= lerp(1.0, vig, _VignetteStrength);

                return saturate(c);
            }
            ENDCG
        }
    }
    FallBack Off
}
