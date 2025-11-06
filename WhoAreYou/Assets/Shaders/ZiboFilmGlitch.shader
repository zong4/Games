Shader "Custom/ZiboFilmGlitch"
{
    Properties
    {
        _MainTex ("Base (Scene Texture)", 2D) = "white" {}
        _Distortion ("Distortion Strength", Range(0, 0.1)) = 0.03
        _NoiseIntensity ("Noise Intensity", Range(0,1)) = 0.3
        _NoiseScale ("Noise Scale", Range(1, 100)) = 40
        _ChromaticAberration ("RGB Split", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        ZTest Always
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Distortion;
            float _NoiseIntensity;
            float _NoiseScale;
            float _ChromaticAberration;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ---- 1️⃣ 扭曲波动 ----
                float wave = sin(i.uv.y * 50 + _Time.y * 8.0) * _Distortion;
                i.uv.x += wave;

                // ---- 2️⃣ RGB 通道分离 ----
                float2 uvR = i.uv + float2(_ChromaticAberration * 0.001, 0);
                float2 uvB = i.uv - float2(_ChromaticAberration * 0.001, 0);

                float3 color;
                color.r = tex2D(_MainTex, uvR).r;
                color.g = tex2D(_MainTex, i.uv).g;
                color.b = tex2D(_MainTex, uvB).b;

                // ---- 3️⃣ 加入随机噪点 ----
                float noise = rand(i.uv * _NoiseScale + _Time.y * 60.0);
                color += (noise - 0.5) * _NoiseIntensity;

                return float4(color, 1);
            }
            ENDCG
        }
    }
}
