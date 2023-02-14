Shader "Hidden/GRP/TerrainLitAdd"
{
	Properties
	{
		[KeywordEnum(On, Clip, Dither, Off)] _Shadows("Cast Shadows", Float) = 0
		[Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows("Receive Shadows", Float) = 1
		
		[HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
		[HideInInspector] _Splat0("Layer 0 (R)", 2D) = "grey" {}
		[HideInInspector] _Splat1("Layer 1 (G)", 2D) = "grey" {}
		[HideInInspector] _Splat2("Layer 2 (B)", 2D) = "grey" {}
		[HideInInspector] _Splat3("Layer 3 (A)", 2D) = "grey" {}
		[HideInInspector] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
		[HideInInspector] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
		[HideInInspector] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
		[HideInInspector] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
		[HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}
	}


	Subshader
	{
		HLSLINCLUDE
		#pragma multi_compile_instancing
		#pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
		#pragma multi_compile __ _ALPHATEST_ENABLED
		ENDHLSL

		Tags
		{
			"Queue" = "Geometry-99"
		}

		Pass
		{
			Tags
			{
				"LightMode" = "GRPLit"
			}

			Blend One One
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _RECEIVE_SHADOWS
			#pragma shader_feature _HATCHING_ENABLED
			#pragma multi_compile _ _OUTPUT_NORMALS_ENABLED
			#pragma multi_compile _ _FOG_LINEAR _FOG_EXP _FOG_EXP2
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7
			#pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
			#pragma multi_compile _ _LIGHTS_PER_OBJECT
			#pragma vertex TerrainLitPassVertex
			#pragma fragment TerrainLitPassFragment
			#define TERRAIN_SPLAT_ADDPASS
			#include "Includes/TerrainLitInput.hlsl"
			#include "Includes/TerrainLitPass.hlsl"
			ENDHLSL
		}
	}
	CustomEditor "AggroBird.GRP.Editor.CustomShaderGUI"
}