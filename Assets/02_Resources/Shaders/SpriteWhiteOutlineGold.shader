Shader "Custom/Sprite/WhiteOutlineGold"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Outline Tint", Color) = (1,1,1,1)
        [IntRange] _OutlineStartPx ("Outline Start (px from edge)", Range(0, 10)) = 0
        [IntRange] _OutlineEndPx ("Outline End (px from edge)", Range(1, 10)) = 3
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.5
        _GoldAmount ("Gold Amount", Range(0,1)) = 1
        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineSpeed ("Shine Speed", Range(0,5)) = 1
        _ShineWidth ("Shine Width", Range(0.01,0.5)) = 0.12
        _ShineIntensity ("Shine Intensity", Range(0,3)) = 1
        _ShineAngle ("Shine Angle (deg)", Range(0,360)) = 45
        [Toggle] _RealtimeAnimation ("Realtime (Ignore Timescale)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"              = "Transparent"
            "RenderType"         = "Transparent"
            "RenderPipeline"     = "UniversalPipeline"
            "IgnoreProjector"    = "True"
            "PreviewType"        = "Plane"
            "CanUseSpriteAtlas"  = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Sprite2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _OutlineStartPx;
                float  _OutlineEndPx;
                float  _AlphaThreshold;
                float  _GoldAmount;
                float4 _ShineColor;
                float  _ShineSpeed;
                float  _ShineWidth;
                float  _ShineIntensity;
                float  _ShineAngle;
                float  _RealtimeAnimation;
            CBUFFER_END

            float _GlobalUnscaledTime; // set by ShaderUnscaledTimeUpdater.cs via Shader.SetGlobalFloat

            // 텍스처 밖은 투명으로 본다 — 그림이 텍스처 끝까지 꽉 찬 곳(GM_CreamBox 윗변 등)은 바깥을 읽으면 반대편 픽셀이 나와 외곽선이 빠진다.
            half SampleAlpha(float2 uv)
            {
                if (any(uv < 0.0) || any(uv > 1.0)) return 0;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv         = IN.uv;
                // _Color는 frag에서 외곽선 영역에만 적용 (SpriteRenderer.color = vertex color만 통과)
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                // 알파 영역 바깥은 그대로
                if (c.a < _AlphaThreshold) return c;

                // 가장 가까운 알파 0 픽셀까지 거리 측정 (4방향)
                float2 texel    = _MainTex_TexelSize.xy;
                float  edgeDist = _OutlineEndPx + 1.0;
                int    maxPx    = (int)_OutlineEndPx;

                [loop]
                for (int i = 1; i <= maxPx; i++)
                {
                    float2 d = texel * (float)i;
                    half a1 = SampleAlpha(IN.uv + float2( d.x,  0));
                    half a2 = SampleAlpha(IN.uv + float2(-d.x,  0));
                    half a3 = SampleAlpha(IN.uv + float2(  0,  d.y));
                    half a4 = SampleAlpha(IN.uv + float2(  0, -d.y));
                    half minA = min(min(a1, a2), min(a3, a4));
                    if (minA < _AlphaThreshold && edgeDist > _OutlineEndPx)
                    {
                        edgeDist = (float)i;
                    }
                }

                // 외곽선 마스크 (_OutlineStartPx ~ _OutlineEndPx 범위)
                float maskIn      = smoothstep(_OutlineStartPx - 0.5, _OutlineStartPx + 0.5, edgeDist);
                float maskOut     = smoothstep(_OutlineEndPx + 0.5, _OutlineEndPx - 0.5, edgeDist);
                float outlineMask = maskIn * maskOut;

                // 외곽선 영역의 금색 (luma → 금색 그라데이션, _Color 틴트는 외곽선만 곱해짐)
                half  luma       = dot(c.rgb, half3(0.299, 0.587, 0.114));
                half3 darkGold   = half3(0.30, 0.18, 0.00);
                half3 brightGold = half3(1.20, 1.00, 0.50);
                half3 gold       = lerp(darkGold, brightGold, luma) * _Color.rgb;

                c.rgb = lerp(c.rgb, gold, outlineMask * _GoldAmount);

                // 광택 띠 (외곽선 영역만, _Color 틴트 적용)
                float  angleRad     = radians(_ShineAngle);
                float2 dir          = float2(cos(angleRad), sin(angleRad));
                float  shineCoord   = dot(IN.uv, dir);
                float  shineTime    = lerp(_Time.y, _GlobalUnscaledTime, _RealtimeAnimation);
                float  bandPos      = frac(shineTime * _ShineSpeed) * 2.0 - 0.5;
                float  distFromBand = abs(shineCoord - bandPos);
                float  shine        = smoothstep(_ShineWidth, 0.0, distFromBand);
                c.rgb              += shine * _ShineColor.rgb * _Color.rgb * _ShineIntensity * c.a * outlineMask;

                return c;
            }
            ENDHLSL
        }
    }
}
