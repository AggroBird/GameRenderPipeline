Shader "Hidden/GRP/Terrain/LitBase"
{
	Properties
	{
		[MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _MainTex("Albedo(RGB), Smoothness(A)", 2D) = "white" {}
        _MetallicTex ("Metallic (R)", 2D) = "black" {}
        [HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}
	}
	Subshader
	{
		HLSLINCLUDE
		#pragma multi_compile_instancing
		#pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
		#pragma multi_compile __ _ALPHATEST_ON
		ENDHLSL

		Tags
		{
			"Queue" = "Geometry-100"
			"TerrainCompatible" = "True"
		}

		Pass
		{
			Tags
			{
				"LightMode" = "GRPLit"
			}
			
			HLSLPROGRAM
			#pragma target 2.0
			#pragma shader_feature _RECEIVE_SHADOWS
			#pragma multi_compile _ _FOG_LINEAR _FOG_EXP _FOG_EXP2
			#pragma multi_compile _ _OUTPUT_NORMALS_ENABLED
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7
			#pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
			#pragma multi_compile _ _LIGHTS_PER_OBJECT
			#pragma multi_compile _ _CELL_SHADING_ENABLED
            #pragma shader_feature_local _NORMALMAP
			#pragma vertex TerrainLitPassVertex
			#pragma fragment TerrainLitPassFragment
            #define TERRAIN_SPLAT_BASEPASS 1
			#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/TerrainLitInput.hlsl"
			#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/TerrainLitPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			#pragma vertex TerrainShadowCasterPassVertex
			#pragma fragment TerrainShadowCasterPassFragment
			#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/TerrainLitInput.hlsl"
			#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/TerrainLitPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Tags
			{
				"LightMode" = "DepthOnly"
			}

			ZWrite On
			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment
			#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/TerrainLitInput.hlsl"
			#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/TerrainLitPass.hlsl"
			ENDHLSL
		}

        Pass
        {
            Tags
			{
				"LightMode" = "SceneSelectionPass"
			}

            HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment
			#define SCENESELECTIONPASS
			#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/TerrainLitInput.hlsl"
			#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/TerrainLitPass.hlsl"
            ENDHLSL
        }
		UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
	}
}