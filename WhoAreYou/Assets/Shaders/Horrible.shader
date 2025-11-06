Shader "Custom/GrainyEvilShader_NoDistort"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Noise Intensity", Range(0,1)) = 0.2
        _Contrast ("Contrast", Range(0.5,2)) = 1.2
        _Saturation ("Saturation", Range(0,1)) = 0.7
        _Vignette ("Vignette Strength", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Intensity, _Contrast, _Saturation, _Vignette;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // 简单噪点函数
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 取样原始颜色
                fixed4 col = tex2D(_MainTex, uv);

                // 调整对比与饱和度
                float3 gray = dot(col.rgb, float3(0.3, 0.59, 0.11));
                col.rgb = lerp(gray.xxx, col.rgb, _Saturation);
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;

                // 静态噪点
                float noise = rand(floor(uv * 800.0)) - 0.5;
                col.rgb += noise * _Intensity;

                // 暗角
                float2 dist = abs(uv - 0.5);
                float vign = smoothstep(0.3, 0.7, length(dist) * 1.5);
                col.rgb *= 1 - vign * _Vignette;

                return col;
            }
            ENDHLSL
        }
    }
}
