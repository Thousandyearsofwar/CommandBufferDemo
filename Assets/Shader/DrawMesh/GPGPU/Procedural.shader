Shader "Unlit/Procedural"
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

        struct ParticleData
        {
            float4 position;
            float4 uv;
        };

        StructuredBuffer<ParticleData> _Positions;
        float _Step;

        struct Attributes
        {
            uint vid : SV_VertexID;
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float2 texcoord : TEXCOORD1;
            float3 viewWS : TEXCOORD2;
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


            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 v1 = _Positions[input.vid].position.xyz;
                unity_ObjectToWorld = 0.0;
                #if defined(INSTANCING_ON)
                    unity_ObjectToWorld._m03_m13_m23_m33 = float4(v1 + 2 * float3(input.instanceID.x, input.instanceID.x, 0), 1.0);
                    unity_ObjectToWorld._m00_m11_m22 = 1.0;
                #endif
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.viewWS = GetWorldSpaceViewDir(output.positionWS);
                output.texcoord = _Positions[input.vid].uv.xy;
                return output;
            }

            float4 LitPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData. albedo = float3(1.0f, 1.0f, 1.0f);
                surfaceData. specular = half3(0.0h, 0.0h, 0.0h);
                surfaceData. metallic = 0.5f;
                surfaceData. smoothness = 0.5f;
                surfaceData. normalTS = float3(0.0f, 1.0f, 0.0f);
                surfaceData .alpha = 1.0f;


                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = half3(0.0h, 0.0h, 1.0h);

                inputData.viewDirectionWS = input.viewWS;
                
                float4 color = UniversalFragmentPBR(inputData, surfaceData);

                return float4(input.texcoord.xy, 0.0f, 1.0f);
            }

            ENDHLSL

        }
    }
}
