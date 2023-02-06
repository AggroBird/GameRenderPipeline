Shader "Hidden/GRP/SkyboxRender"
{
	Properties
	{
        
	}
	Subshader
	{
		Tags
		{
			"Queue" = "Background"
			"RenderType" = "Background"
			"PreviewType" = "Skybox"
		}

		HLSLINCLUDE
		#include "Includes/SkyboxRenderPasses.hlsl"
		#pragma multi_compile _ _SKYBOX_CLOUDS_ON
		ENDHLSL

		Cull Off
		ZTest Always
		ZWrite Off

		// Render skybox cubemap dynamic
		Pass
		{
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment RenderSkyboxCubemapDynamic
			ENDHLSL
		}

		// Render skybox cubemap static
		Pass
		{
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment RenderSkyboxCubemapStatic
			ENDHLSL
		}

		// Blur skybox cubemap
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment BlurSkyboxCubemap
			ENDHLSL
		}

		// Render skybox world dynamic
		Pass
		{
			ZWrite On
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment RenderSkyboxWorldDynamic
			ENDHLSL
		}

		// Render skybox world static
		Pass
		{
			ZTest LEqual

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment RenderSkyboxWorldStatic
			ENDHLSL
		}
	}
}