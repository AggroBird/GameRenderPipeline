Shader "Hidden/GRP/SkyboxRender"
{
	Properties
	{
        
	}
	Subshader
	{
		HLSLINCLUDE
		#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/BlitPass.hlsl"
		#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/Environment.hlsl"
		#pragma multi_compile _ _SKYBOX_CLOUDS_ENABLED
		ENDHLSL

		Tags
		{
			"Queue" = "Background"
			"RenderType" = "Background"
			"PreviewType" = "Skybox"
		}
		Cull Off ZWrite Off

		Pass
		{
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment RenderSkyboxGradient

			float4 RenderSkyboxGradient(BlitVaryings input) : SV_TARGET
			{
				return float4(SampleSkyboxGradient(normalize(CameraTraceDirection(input.texcoord)), true), 1);
			}

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment RenderSkyboxCubemap

			TEXTURECUBE(_SkyboxStaticCubemap);
			SAMPLER(sampler_SkyboxStaticCubemap);

			float4 RenderSkyboxCubemap(BlitVaryings input) : SV_TARGET
			{
				return SAMPLE_TEXTURECUBE(_SkyboxStaticCubemap, sampler_SkyboxStaticCubemap, normalize(CameraTraceDirection(input.texcoord)));
			}

			ENDHLSL
		}
	}
}