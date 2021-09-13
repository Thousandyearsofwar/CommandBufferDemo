Shader "Unlit/GPUParticleShader_Final"
{
    Properties
    {
        [HideInInspector][NoScaleOffset]unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" { }
        [HideInInspector][NoScaleOffset]unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" { }
        [HideInInspector][NoScaleOffset]unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" { }
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"
            "UniversalMaterialType" = "Unlit" "Queue" = "Geometry" }
        Pass
        {
            Name "Pass"
            Tags {  }

            // Render State
            Cull Back
            Blend One Zero
            ZTest LEqual
            ZWrite On


            HLSLPROGRAM

            // Pragmas
            #pragma target 2.0
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag

            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
            {
                float3 positionOS : POSITION;
                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
            };

            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END
            
            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                StructuredBuffer<float3> _Positions;
            #endif

            float _Step;

            void ConfigureProcedural()
            {
                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                    float3 position = _Positions[unity_InstanceID];
                    unity_ObjectToWorld = 0.0;
                    unity_ObjectToWorld._m03_m13_m23_m33 = float4(position * 5, 1.0);
                    //Scale*5
                    unity_ObjectToWorld._m00_m11_m22 = _Step * 5;
                #endif
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // Returns the camera relative position (if enabled)
                float3 positionWS = TransformObjectToWorld(input.positionOS);

                output.positionWS = positionWS;

                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                return half4(input.positionWS, 1.0);
            }


            ENDHLSL

        }
    }
    FallBack "Hidden/Shader Graph/FallbackError"
}
