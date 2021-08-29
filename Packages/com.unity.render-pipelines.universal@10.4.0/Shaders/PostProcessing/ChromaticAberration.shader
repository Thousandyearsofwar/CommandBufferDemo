Shader "Hidden/URPPostProcess/ChromaticAberration"
{
	Properties
	{
		[HideInInspector]_MainTex ("MainTex", 2D) = "white" { }
		_Intensity ("Intensity", float) = 0.5
	}
	SubShader
	{
		Tags { "RenderType" = "UniversalRenderPipeline" }
		Cull Off
		ZWrite Off
		ZTest Always

		HLSLINCLUDE

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		CBUFFER_START(UnityPerMaterial)
		float4 _CameraColorTexture_TexelSize;
		float _Intensity;
		CBUFFER_END


		TEXTURE2D(_CameraColorTexture);
		SAMPLER(sampler_CameraColorTexture);

		TEXTURE2D(_AberrationLUT);
		SAMPLER(sampler_AberrationLUT);

		ENDHLSL

		Pass
		{
			HLSLPROGRAM

			#pragma vertex vertexShader
			#pragma fragment fragmentShader

			struct Attributes
			{
				float4 positionOS: POSITION;
				float2 texcoord: TEXCOORD;
			};
			struct Varyings
			{
				float4 positionCS: SV_POSITION;
				float2 texcoord: TEXCOORD;
			};

			Varyings vertexShader(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.texcoord = input.texcoord;
				return output;
			}
			
			float4 ChromaticAberration(Varyings input)
			{
				float2 coords = 2.0 * input.texcoord - 1.0;
				float2 end = input.texcoord - coords * dot(coords, coords) * _Intensity;

				float2 diff = end - input.texcoord;
				int samples = clamp(int(length(_CameraColorTexture_TexelSize.zw * diff / 2.0)), 3, 16);
				float2 delta = diff / samples;
				float2 pos = input.texcoord;
				float3 sum = (0.0).xxx, filterSum = (0.0).xxx;
				
				for (int i = 0; i < samples; i++)
				{
					float t = (i + 0.5f) / samples;
					
					float3 s = SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_CameraColorTexture, pos, 0).rgb;
					float3 filter = SAMPLE_TEXTURE2D_LOD(_AberrationLUT, sampler_AberrationLUT, float2(t, 0), 0).rgb;

					sum += s * filter;
					filterSum += filter;
					pos += delta;
				}

				return float4(sum / filterSum, 1.0);
			}

			

			float4 fragmentShader(Varyings input): SV_TARGET
			{
				
				return ChromaticAberration(input);
			}


			ENDHLSL

		}
	}
}