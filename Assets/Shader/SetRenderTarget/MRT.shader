Shader "Unlit/MRT"
{
    Properties { }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 100
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)

        CBUFFER_END
        TEXTURE2D_X_FLOAT(_CameraDepthAttachment);
        SAMPLER(sampler_CameraDepthAttachment);
        TEXTURE2D_X_FLOAT(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);

        struct Attributes
        {
            float4 positionOS : POSITION;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };

        struct FragmentOutput
        {
            half4 GBuffer0 : SV_Target0;
            half4 GBuffer1 : SV_Target1;
            half4 GBuffer2 : SV_Target2;
            half4 GBuffer3 : SV_Target3;
        };
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            #pragma vertex m_LitPassVertex
            #pragma fragment m_LitGBufferPassFragment

            Varyings m_LitPassVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            FragmentOutput m_LitGBufferPassFragment(Varyings input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                FragmentOutput m_FragmentOutput;
                //m_FragmentOutput.GBuffer0 = float4(1, 0, 0, 1) * SAMPLE_TEXTURE2D(_CameraDepthAttachment, sampler_CameraDepthAttachment, float2(0.5, 0.5)).r;
                m_FragmentOutput.GBuffer0 = float4(1, 0, 0, 1) * SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, float2(0.5, 0.5)).r;
                m_FragmentOutput.GBuffer1 = float4(0, 1, 0, 1);
                m_FragmentOutput.GBuffer2 = float4(0, 0, 1, 1);
                m_FragmentOutput.GBuffer3 = float4(1, 0, 0, 1);

                return m_FragmentOutput;
            }
            ENDHLSL

        }
    }
}
