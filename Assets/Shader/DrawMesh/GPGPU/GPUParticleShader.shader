Shader "Unlit/GPUParticleShader"
{
    Properties { }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
        HLSLINCLUDE
        //已经包含UntiyInstancing.hlsl
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        struct ParticleData
        {
            float4 position;
            float4 uv;
        };

        StructuredBuffer<float3> _Positions;
        float _Step;
        struct Attributes
        {
            float4 positionOS : POSITION;
            uint instanceID : SV_InstanceID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
        };
        

        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            Blend One Zero
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #pragma multi_compile_instancing
            //#define UNITY_INSTANCING_ENABLED
            //#pragma instancing_options procedural:ConfigureProcedural

            void ConfigureProcedural(Attributes input)
            {
                //#if defined(UNITY_INSTANCING_ENABLED)
                float3 position = _Positions[input.instanceID];
                unity_ObjectToWorld = 0.0;
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(position * 5, 1.0);
                unity_ObjectToWorld._m00_m11_m22 = _Step * 5;
                //#endif
            }

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                ConfigureProcedural(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            float4 LitPassFragment(Varyings input) : SV_TARGET
            {
                return float4(input.positionWS, 1.0);
            }

            ENDHLSL

        }
    }
}
