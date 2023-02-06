Shader "Hidden/GRP/BlitRenderTarget"
{
	Properties
	{

	}
	Subshader
	{
		HLSLINCLUDE
		#include "Includes/BlitPass.hlsl"
		ENDHLSL

		Pass
		{
			Name "Color"

			ZWrite Off
			ZTest Always
			Cull Off

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment Fragment

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			float4 Fragment(BlitVaryings input) : SV_TARGET
			{
				return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);
			}

			ENDHLSL
		}

		Pass
		{
			Name "Depth"

			ZWrite On
			ZTest Always
			Cull Off
			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment Fragment

			TEXTURE2D(_DepthTex);
			SAMPLER(sampler_DepthTex);

			void Fragment(BlitVaryings input, out float outDepth : SV_Depth)
			{
				outDepth = SAMPLE_DEPTH_TEXTURE(_DepthTex, sampler_DepthTex, input.texcoord);
			}

			ENDHLSL
		}

		Pass
		{
			Name "ColorAndDepth"

			ZWrite On
			ZTest Always
			Cull Off

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment Fragment

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_DepthTex);
			SAMPLER(sampler_DepthTex);

			float4 Fragment(BlitVaryings input, out float outDepth : SV_Depth) : SV_TARGET
			{
				outDepth = SAMPLE_DEPTH_TEXTURE(_DepthTex, sampler_DepthTex, input.texcoord);
				return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);
			}

			ENDHLSL
		}
	}
}