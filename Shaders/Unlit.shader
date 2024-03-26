Shader "GRP/Unlit"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)
		[Space]
		_EmissionTex("Emission Texture", 2D) = "black" {}
		[HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)
		[Space]
		[Toggle(_ALPHATEST_ON)] _Clipping("Alpha Clipping", Float) = 0
		_Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
		[Space]
		[KeywordEnum(On, Clip, Dither, Off)] _Shadows("Cast Shadows", Float) = 0
		[Space]
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Z Test", Float) = 4
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
	}
	Subshader
	{
		HLSLINCLUDE
		#pragma multi_compile_instancing
		#pragma multi_compile _ LOD_FADE_CROSSFADE
		#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/UnlitInput.hlsl"
		ENDHLSL

		Pass
		{
			Name "UnlitPass"

			Blend[_SrcBlend][_DstBlend], One OneMinusSrcAlpha
			ZWrite[_ZWrite]
			ZTest[_ZTest]
			Cull[_Cull]

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _ALPHATEST_ON
			#pragma shader_feature _HAS_EMISSION_TEXTURE
			#pragma multi_compile _ _OUTPUT_NORMALS_ENABLED
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/UnlitPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "ShadowPass"

			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/ShadowCasterPass.hlsl"
			ENDHLSL
		}
	}
	Dependency "BillboardShader" = "Diffuse"
	CustomEditor "AggroBird.GameRenderPipeline.Editor.CustomShaderGUI"
}