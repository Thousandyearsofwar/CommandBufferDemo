Shader "Unlit/GPUParticleProceduralShader"
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

        CBUFFER_START(UnityBillboardPerCamera)
        float3 unity_BillboardNormal;
        float3 unity_BillboardTangent;
        float4 unity_BillboardCameraParams;
        #define unity_BillboardCameraPosition (unity_BillboardCameraParams.xyz)
        #define unity_BillboardCameraXZAngle (unity_BillboardCameraParams.w)
        CBUFFER_END

        CBUFFER_START(UnityBillboardPerBatch)
        float4 unity_BillboardInfo; // x: num of billboard slices; y: 1.0f / (delta angle between slices)
        float4 unity_BillboardSize; // x: width; y: height; z: bottom
        float4 unity_BillboardImageTexCoords[16];
        CBUFFER_END
        struct ParticleData
        {
            float4 position;
        };

        StructuredBuffer<ParticleData> _Positions;
        float _Step;

        struct Attributes
        {
            uint vid : SV_VertexID;
            float4 positionOS : POSITION;
            uint instanceID : SV_InstanceID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float2 texcoord : TEXCOORD1;
            float3 viewWS : TEXCOORD2;
            uint instanceID : SV_InstanceID;
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
                Varyings output = (Varyings)0;
                float2 texcoord;
                texcoord.x = float(((input.vid + 1) & 2) >> 1);
                texcoord.y = float((input.vid & 2) >> 1);

                float3 objectSpace = float3(texcoord.xy - 0.5, 0.0);

                float3 worldSpace = _Positions[input.vid / 4].position.xyz;
                
                unity_ObjectToWorld = 0.0;
                //
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(worldSpace, 1.0);
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(worldSpace + float3(input.instanceID.x, input.instanceID.x, 0.0), 1.0);
                unity_ObjectToWorld._m00_m11_m22 = 1.0;

                float4 pivotWS = mul(UNITY_MATRIX_M, float4(0, 0, 0, 1));
                float4 pivotVS = mul(UNITY_MATRIX_V, pivotWS);
                float4 positionVS = pivotVS + float4(objectSpace.xy, 0, 1);

                output.positionCS = TransformWViewToHClip(positionVS.xyz);

                //output.positionCS = TransformObjectToHClip(objectSpace.xyz);

                output.positionWS = (objectSpace.xyz);
                output.viewWS = GetWorldSpaceViewDir(output.positionWS);

                output.texcoord = texcoord;
                return output;
            }

            float4 LitPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return float4(input.texcoord.xy, 0.0, 1.0f);
            }

            ENDHLSL

        }
    }
}
