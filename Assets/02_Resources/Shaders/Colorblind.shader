Shader "Custom/Colorblind"
{
    Properties
    {
        _ColorblindType ("Type (1=Protan,2=Deutan,3=Tritan)", Float) = 0
        _Strength       ("Strength", Range(0, 1)) = 1
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
            Name "Colorblind"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ColorblindType;
            float _Strength;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float3 rgb = color.rgb;

                // 색약 유형별 시뮬레이션 행렬(Brettel/Vienot 근사)과 에러 보정 행렬.
                // 보정(daltonization): 색약자가 구분 못하는 색의 차이를 구분 가능한 채널로 재분배한다.
                float3x3 simMat;
                float3x3 errMat;

                if (_ColorblindType < 1.5)
                {
                    // Protanopia (적색맹)
                    simMat = float3x3(0.567, 0.433, 0.0,
                                      0.558, 0.442, 0.0,
                                      0.0,   0.242, 0.758);
                    errMat = float3x3(0.0, 0.0, 0.0,
                                      0.7, 1.0, 0.0,
                                      0.7, 0.0, 1.0);
                }
                else if (_ColorblindType < 2.5)
                {
                    // Deuteranopia (녹색맹)
                    simMat = float3x3(0.625, 0.375, 0.0,
                                      0.70,  0.30,  0.0,
                                      0.0,   0.30,  0.70);
                    errMat = float3x3(0.0, 0.0, 0.0,
                                      0.7, 1.0, 0.0,
                                      0.7, 0.0, 1.0);
                }
                else
                {
                    // Tritanopia (청색맹)
                    simMat = float3x3(0.95, 0.05,  0.0,
                                      0.0,  0.433, 0.567,
                                      0.0,  0.475, 0.525);
                    errMat = float3x3(1.0, 0.0, 0.7,
                                      0.0, 1.0, 0.7,
                                      0.0, 0.0, 0.0);
                }

                float3 simulated  = mul(simMat, rgb);
                float3 error      = rgb - simulated;
                float3 correction = mul(errMat, error);
                float3 corrected  = saturate(rgb + correction);

                color.rgb = lerp(rgb, corrected, _Strength);
                return color;
            }
            ENDHLSL
        }
    }
}
