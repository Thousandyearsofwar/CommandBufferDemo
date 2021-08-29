Shader "Unlit/LitInstanceTest"
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
        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)


        struct Attributes
        {
            float4 positionOS: POSITION;
            float3 normalOS: NORMAL;
            float2 texcoord: TEXCOORD0;
            float2 lightmapUV: TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS: SV_POSITION;
            float2 texcoord: TEXCOORD0;
            DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
            float3 normalWS: TEXCOORD2;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };


        // #ifdef LIGHTMAP_ON
        //     #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) float2 lmName: TEXCOORD##index
        //     #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT) OUT.xy = lightmapUV.xy * lightmapScaleOffset.xy + lightmapScaleOffset.zw;
        //     #define OUTPUT_SH(normalWS, OUT)
        // #else
        //     #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) half3 shName: TEXCOORD##index
        //     #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT)
        //     #define OUTPUT_SH(normalWS, OUT) OUT.xyz = SampleSHVertex(normalWS)
        // #endif


        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            HLSLPROGRAM

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ UNITY_INSTANCED_LIGHTMAPSTS
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            //#pragma instancing_options nolightmap//使用实例化时获取不了LightmapST[Scale/Offset].
            //#pragma instancing_options nolightprobe//使用实例化时获取不了Light Probe values(包括occlusion data).
            
            //#pragma instancing_options nomatrices//没有M矩阵变换
            //#pragma instancing_options force_same_maxcount_for_gl
            //#pragma instancing_options maxcount:50
            //#pragma instancing_options forcemaxcount:50

            //#pragma instancing_options nolodfade //默认lodfade
            // #pragma instancing_options assumeuniformscaling


            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.texcoord = input.texcoord;
                output.normalWS = input.normalOS;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                //#define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT) OUT.xy = lightmapUV.xy * lightmapScaleOffset.xy + lightmapScaleOffset.zw;
                //output.lightmapUV = input.lightmapUV.xy * unity_LightmapST .xy + unity_LightmapST.zw;

                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
                //#define OUTPUT_SH(normalWS, OUT) OUT.xyz = SampleSHVertex(normalWS)
                //output.vertexSH = SampleSHVertex(output.normalWS.xyz);
                return output;
            }

            float4 LitPassFragment(Varyings input): SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 ColorGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, input.normalWS);
                //float3 ColorGI = SampleLightmap(input.lightmapUV, input.normalWS);
                //float3 ColorGI = SampleSHPixel(input.vertexSH, input.normalWS);
                //unity_LODFade.xxx *
                return float4(ColorGI, 1.0);
            }

            ENDHLSL

        }
    }
}
