Shader "GRP/Lit"
{
	Properties
	{
		_MainTex("Diffuse Texture", 2D) = "white" {}
		_Color("Diffuse Color", Color) = (1, 1, 1, 1)
		[Space]
		_EmissionTex("Emission Texture", 2D) = "black" {}
		[HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)
		[Space]
		_NormalTex("Normal Texture", 2D) = "normal" {}
		_NormalScale("Normal Scale", Range(0, 1)) = 1
		[Space]
		_Metallic("Metallic", Range(0, 1)) = 0
		_Smoothness("Smoothness", Range(0, 1)) = 0.2
		_Fresnel("Fresnel", Range(0, 1)) = 1
		[Space]
		[Toggle(_ALPHATEST_ON)] _Clipping("Alpha Clipping", Float) = 0
		_Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
		[Space]
		[KeywordEnum(On, Clip, Dither, Off)] _Shadows("Cast Shadows", Float) = 0
		[Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows("Receive Shadows", Float) = 1
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
		#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/LitInput.hlsl"
		ENDHLSL

		Pass
		{
			Name "LitPass"

			Tags
			{
				"LightMode" = "GRPLit"
			}

			Blend[_SrcBlend][_DstBlend], One OneMinusSrcAlpha
			ZWrite[_ZWrite]
			ZTest[_ZTest]
			Cull[_Cull]

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _ALPHATEST_ON
			#pragma shader_feature _HAS_EMISSION_TEXTURE
			#pragma shader_feature _HAS_NORMAL_TEXTURE
			#pragma shader_feature _RECEIVE_SHADOWS
			#pragma multi_compile _ _FOG_LINEAR _FOG_EXP _FOG_EXP2
			#pragma multi_compile _ _OUTPUT_NORMALS_ENABLED
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7
			#pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
			#pragma multi_compile _ _LIGHTS_PER_OBJECT
			#pragma multi_compile _ _CELL_SHADING_ENABLED
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment
			#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/LitPass.hlsl"
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