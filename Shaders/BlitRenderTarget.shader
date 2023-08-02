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

			TEXTURE2D(_Blit_ColorInput);
			SAMPLER(sampler_Blit_ColorInput);

			float4 Fragment(BlitVaryings input) : SV_TARGET
			{
				return SAMPLE_TEXTURE2D(_Blit_ColorInput, sampler_Blit_ColorInput, input.texcoord);
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

			TEXTURE2D(_Blit_DepthInput);
			SAMPLER(sampler_Blit_DepthInput);

			void Fragment(BlitVaryings input, out float outDepth : SV_Depth)
			{
				outDepth = SAMPLE_DEPTH_TEXTURE(_Blit_DepthInput, sampler_Blit_DepthInput, input.texcoord);
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

			TEXTURE2D(_Blit_ColorInput);
			SAMPLER(sampler_Blit_ColorInput);
			TEXTURE2D(_Blit_DepthInput);
			SAMPLER(sampler_Blit_DepthInput);

			float4 Fragment(BlitVaryings input, out float outDepth : SV_Depth) : SV_TARGET
			{
				outDepth = SAMPLE_DEPTH_TEXTURE(_Blit_DepthInput, sampler_Blit_DepthInput, input.texcoord);
				return SAMPLE_TEXTURE2D(_Blit_ColorInput, sampler_Blit_ColorInput, input.texcoord);
			}

			ENDHLSL
		}
	}
}