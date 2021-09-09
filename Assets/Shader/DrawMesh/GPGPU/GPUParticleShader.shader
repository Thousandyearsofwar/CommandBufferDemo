Shader "Unlit/GPUParticleShader"
{
    Properties { }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 100
        HLSLINCLUDE
        //已经包含UntiyInstancing.hlsl
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        StructuredBuffer<float3> _Positions;
        float _Step;
        struct Attributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            HLSLPROGRAM

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #pragma multi_compile_instancing
            //#pragma instancing_options procedural:ConfigureProcedural
            
            void ConfigureProcedural(Attributes input)
            {
                #if defined(INSTANCING_ON)
                    float3 position = _Positions[input.instanceID];
                    unity_ObjectToWorld = 0.0;
                    unity_ObjectToWorld._m03_m13_m23_m33 = float4(position * 5, 1.0);
                    unity_ObjectToWorld._m00_m11_m22 = _Step * 5;
                #endif
            }

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                ConfigureProcedural(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            float4 LitPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return float4(input.positionWS, 1.0);
            }

            ENDHLSL

        }
    }
}
