Shader "Hidden/GRP/TerrainDetailGrassBillboard"
{
	Properties
	{
		_WavingTint("Fade Color", Color) = (.7,.6,.5, 0)
		_MainTex("Base (RGB) Alpha (A)", 2D) = "white" {}
		_WaveAndDistance("Wave and distance", Vector) = (12, 3.6, 1, 1)
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
	}
	Subshader
	{
		Tags
		{
			"Queue" = "Geometry+200"
			"RenderPipeline" = "GameRenderPipeline"
			"ShaderModel" = "3.5"
		}

		Cull Off
		ColorMask RGB
		AlphaTest Greater[_Cutoff]

		Pass
		{
			Name "GrassPass"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma multi_compile_fog
			#pragma multi_compile _ _COLOR_SPACE_LINEAR
			#pragma multi_compile _ _OUTPUT_NORMALS_ENABLED
			#define _ALPHATEST_ENABLED
			#define _RECEIVE_SHADOWS
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7
			#pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
			#pragma multi_compile_instancing

			#pragma vertex GrassVertex
			#pragma fragment GrassFragment
			#define GRASS_BILLBOARD

			#include "Includes/TerrainGrassInput.hlsl"
			#include "Includes/TerrainGrassPass.hlsl"

			ENDHLSL
		}
	}
	CustomEditor "AggroBird.GRP.Editor.CustomShaderGUI"
}