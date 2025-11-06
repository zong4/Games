Shader "Custom/SelectiveDissolve_KeepOtherLayers"
{
    Properties
    {
        _MainTex("Scene Texture", 2D) = "white" {}
        _MaskTex("Mask Texture", 2D) = "white" {}
        _Threshold("Dissolve Threshold", Range(0,1)) = 0
        _NoiseScale("Noise Scale", Range(1,50)) = 10
        _EdgeColor("Edge Color", Color) = (1,1,1,1) // 溶解变白
        _EdgeWidth("Edge Width", Range(0,0.2)) = 0.05
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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float _Threshold;
            float _NoiseScale;
            float _EdgeWidth;
            float4 _EdgeColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy,float2(12.9898,78.233)))*43758.5453);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = rand(i);
                float b = rand(i + float2(1,0));
                float c = rand(i + float2(0,1));
                float d = rand(i + float2(1,1));
                float2 u = f*f*(3-2*f);
                return lerp(a,b,u.x) + (c-a)*u.y*(1-u.x) + (d-b)*u.x*u.y;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float mask = tex2D(_MaskTex, i.uv).r;

                // 只对 Mask 白色区域溶解
                if(mask > 0.5)
                {
                    float n = noise(i.uv * _NoiseScale + _Time.y*0.1);
                    float dissolve = step(_Threshold, n);
                    float edge = smoothstep(_Threshold - _EdgeWidth, _Threshold, n);

                    // 溶解区域变白
                    col.rgb = lerp(_EdgeColor.rgb, col.rgb, edge);
                }
                // 非溶解区域保持原始颜色，不做处理

                return col;
            }
            ENDCG
        }
    }
}
