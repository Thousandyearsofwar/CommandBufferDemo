Shader "Unlit/UnlitTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _TestColor ("TestColor", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 100
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _TestColor;
        CBUFFER_END

        TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);

        struct Attributes
        {
            float4 positionOS: POSITION;
            float2 texcoord: TEXCOORD;
        };

        struct Varyings
        {
            float4 positionCS: SV_POSITION;
            float2 texcoord: TEXCOORD0;
        };
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment


            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.texcoord;
                return output;
            }

            float4 LitPassFragment(Varyings input): SV_TARGET
            {
                return _TestColor;
            }

            ENDHLSL

        }
    }
}
