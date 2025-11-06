Shader "Custom/GlowDissolve"
{
    Properties
    {
        _MainTex("Base Texture", 2D) = "white" {}
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _Threshold("Dissolve Threshold", Range(0,1)) = 0.5
        _GlowColor("Glow Color", Color) = (1,1,0,1)
        _GlowIntensity("Glow Intensity", Range(0,5)) = 2
        _NoiseScale("Noise Scale", Range(1,20)) = 10
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard alpha:clip

        sampler2D _MainTex;
        sampler2D _NoiseTex;
        float _Threshold;
        float4 _GlowColor;
        float _GlowIntensity;
        float _NoiseScale;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NoiseTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // 基础贴图颜色
            float3 baseCol = tex2D(_MainTex, IN.uv_MainTex).rgb;

            // 噪点
            float3 noiseCol = tex2D(_NoiseTex, IN.uv_NoiseTex * _NoiseScale).rgb;
            float dissolve = (noiseCol.r + noiseCol.g + noiseCol.b) / 3.0;

            // 溶解区域：n < threshold → 消失
            clip(dissolve - _Threshold);

            // 输出颜色
            o.Albedo = baseCol;

            // 自发光
            o.Emission = _GlowColor.rgb * _GlowIntensity;
        }
        ENDCG
    }
}
