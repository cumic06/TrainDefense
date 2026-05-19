Shader "Custom/Sprite/Red"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _RedTint ("Red Tint", Color) = (1, 0, 0, 1)
        _RedAmount ("Red Amount", Range(0, 1)) = 1.0
        _Opacity ("Opacity", Range(0, 1)) = 1.0
        _EmissionColor ("Emission Color", Color) = (1, 0.2, 0.2, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "RenderPipeline"    = "UniversalPipeline"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
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

            CBUFFER_START(UnityPerMaterial)
                float4 _RedTint;
                float  _RedAmount;
                float  _Opacity;
                float4 _EmissionColor;
                float  _EmissionIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv         = IN.uv;
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                // 원본 밝기(luma)를 유지하면서 빨간색 틴트 적용
                half luma = dot(c.rgb, half3(0.299, 0.587, 0.114));
                half3 redColored = luma * _RedTint.rgb;
                c.rgb = lerp(c.rgb, redColored, _RedAmount);

                // 발광(Emission) 효과 — 알파 영역에만 적용
                c.rgb += _EmissionColor.rgb * _EmissionIntensity * c.a;

                // 불투명도 적용
                c.a *= _Opacity;

                return c;
            }
            ENDHLSL
        }
    }
}
