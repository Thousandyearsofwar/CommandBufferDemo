Shader "Unlit/InstanceTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _Value ("Value", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 100
        HLSLINCLUDE
        //已经包含UntiyInstancing.hlsl
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_DEFINE_INSTANCED_PROP(float4, _TestColor)
        UNITY_DEFINE_INSTANCED_PROP(float, _Value)
        UNITY_INSTANCING_BUFFER_END(Props)

        // CBUFFER_START(UnityPerMaterial)
        
        // CBUFFER_END
        //UNITY_INSTANCING_BUFFER_START 如果需要逐物体不同属性需要跟CBUFFER_START替换
        


        // TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);

        struct Attributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
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
            //没有M矩阵变换
            //#pragma instancing_options nomatrices
            //#pragma instancing_options force_same_maxcount_for_gl
            //#pragma instancing_options maxcount:50
            //#pragma instancing_options forcemaxcount:50

            //#pragma instancing_options assumeuniformscaling

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float4 LitPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return UNITY_ACCESS_INSTANCED_PROP(Props, _TestColor) * UNITY_ACCESS_INSTANCED_PROP(Props, _Value);
            }

            ENDHLSL

        }
    }
}
