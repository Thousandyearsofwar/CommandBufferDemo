Shader "Unlit/UnlitTest _LightModeTest"
{
    Properties
    {
        //_MainTex ("Texture", 2D) = "white" { }
        _TestColor ("TestColor", Color) = (1, 1, 1, 1)
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
            UNITY_DEFINE_INSTANCED_PROP(float4,_TestColor)
        UNITY_INSTANCING_BUFFER_END(Props)


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
            Tags { "LightMode" = "m_CustomLightMode" }
            Cull Off
            ZWrite Off
            HLSLPROGRAM

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #pragma multi_compile_instancing

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
                return UNITY_ACCESS_INSTANCED_PROP(Props,_TestColor);
            }

            ENDHLSL

        }
    }
}
