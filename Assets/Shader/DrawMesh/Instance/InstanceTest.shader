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
        //已经包含UnityInstancing.hlsl
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_DEFINE_INSTANCED_PROP(float4, _TestColor)
        UNITY_DEFINE_INSTANCED_PROP(float, _Value)
        UNITY_INSTANCING_BUFFER_END(Props)

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
            
            //#pragma instancing_options nomatrices
            //没有M矩阵变换
            //#pragma instancing_options force_same_maxcount_for_gl

            #pragma instancing_options maxcount:512
            #pragma instancing_options forcemaxcount:512
            //MaxCount值最大被定义为500但是你可以绘制512个[翻译:https://mp.weixin.qq.com/s/qDPfrn2Vtw4qLUpiOBCa8g][原文:https://catlikecoding.com/unity/tutorials/rendering/part-19/]
            //CBuffer只能存4096个16 btye(float)
            //#pragma instancing_options assumeuniformscaling
            //假定XYZ使用相同的缩放值，更改UNITY_WORLDTOOBJECTARRAY_CB的分布
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
