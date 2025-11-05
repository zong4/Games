Shader "Custom/TVNoiseWithNormal"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _BlockSize("Block Size", Range(1, 128)) = 8
        _TimeSpeed("Noise Speed", Range(0,10)) = 1
        _Brightness("Brightness", Range(0,10)) = 1
        _Threshold("Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        ZWrite On
        ZTest LEqual
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NormalMap;
            float4 _MainTex_ST;
            float _BlockSize;
            float _TimeSpeed;
            float _Brightness;
            float _Threshold;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 离散化 UV
                float2 blockUV = floor(uv * _BlockSize) / _BlockSize;

                // 随机值（用于判断是否显示噪声）
                float randomVal = rand(blockUV + _Time.y * _TimeSpeed);

                // 主纹理采样颜色
                fixed4 mainColor = tex2D(_MainTex, uv);

                // 法线贴图采样
                fixed3 normalTex = UnpackNormal(tex2D(_NormalMap, uv));

                fixed4 outColor;

                if (randomVal > _Threshold)
                {
                    // 显示噪声
                    float r = rand(blockUV + _Time.y * _TimeSpeed) * _Brightness;
                    float g = rand(blockUV + _Time.y * _TimeSpeed + 10.0) * _Brightness;
                    float b = rand(blockUV + _Time.y * _TimeSpeed + 20.0) * _Brightness;
                    outColor = fixed4(r, g, b, 1.0);
                    normalTex = float3(0,0,1); // 噪声使用默认法线
                }
                else
                {
                    outColor = mainColor;

                                    // 简单光照效果 (方向光)
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normalTex, lightDir));
                outColor.rgb *= NdotL;
                }

                return outColor;
            }
            ENDCG
        }
    }
}
