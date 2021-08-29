Shader "Unlit/HSVAdjust"
{
    Properties 
    {
        [HideInInspector]_MainTex ("MainTex", 2D) = "white" { }
        _Hue ("Brightness", Range(0, 1)) = 1
        _Saturation ("Saturation", Range(0, 1)) = 1
        _Value ("Value", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float _Hue;
        float _Saturation;
        float _Value;
        CBUFFER_END

        TEXTURE2D(_CameraColorTexture);
        SAMPLER(sampler_CameraColorTexture);

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 texcoord : TEXCOORD;
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord : TEXCOORD;
        };
        ENDHLSL

        pass
        {
            HLSLPROGRAM

            #pragma vertex vertexShader
            #pragma fragment fragmentShader

            Varyings vertexShader(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.texcoord;
                return output;
            }

            float4 fragmentShader(Varyings input) : SV_TARGET
            {
                float4 tex = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, input.texcoord);
                float3 hsv = RgbToHsv(tex.xyz);
                hsv.x += _Hue ;
                hsv.y += _Saturation;
                hsv.z += _Value;
                hsv = saturate(hsv);

                tex.xyz = HsvToRgb(hsv);
                return tex;
            }

            ENDHLSL

        }
    }
}
