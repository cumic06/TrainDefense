Shader "Custom/GameOverEffect"
{
    Properties
    {
        _Intensity        ("Effect Intensity",   Range(0, 1)) = 0
        _ColorTint        ("Color Tint",         Color)       = (0.3, 0.3, 0.3, 1)
        _GrayscaleStrength("Grayscale Strength", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
        }

        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "GameOverEffect"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float  _Intensity;
            float4 _ColorTint;
            float  _GrayscaleStrength;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

                // 회색조 변환
                half luma = dot(color.rgb, half3(0.299h, 0.587h, 0.114h));
                half3 gray = half3(luma, luma, luma);
                half3 desaturated = lerp(color.rgb, gray, _GrayscaleStrength);

                // 색상 틴트 적용 (어둡게 + 색조)
                half3 tinted = desaturated * _ColorTint.rgb;

                // Intensity 기반 블렌딩
                color.rgb = lerp(color.rgb, tinted, _Intensity);

                return color;
            }
            ENDHLSL
        }
    }
}
