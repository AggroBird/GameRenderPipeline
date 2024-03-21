Shader "Hidden/GRP/Terrain/LitBaseGen"
{
	Properties
    {
        [HideInInspector] _Control("AlphaMap", 2D) = "" {}
        [HideInInspector] _Splat3("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] _Splat2("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat1("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat0("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
        [HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
        [HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
        [HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}
        [HideInInspector][Gamma] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 1.0

        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
    }

    Subshader
    {
        HLSLINCLUDE
        #pragma target 3.0
        
        #define _TERRAIN_BASEMAP_GEN

		#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/TerrainLitInput.hlsl"
		#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/TerrainLitPass.hlsl"

        ENDHLSL

        Pass
        {
            Tags
            {
                "Name" = "_MainTex"
                "Format" = "ARGB32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]
            HLSLPROGRAM

            #pragma vertex TerrainLitPassVertex
            #pragma fragment TerrainLitBaseGenDiffuse

            half4 TerrainLitBaseGenDiffuse(Varyings input) : SV_Target
            {
	            InputConfig config = GetTerrainInputConfig(input.texcoord, input.texSplat01, input.texSplat23);
	            SplatmapMix(config);
	            return GetDiffuse(config);
            }

            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "Name" = "_MetallicTex"
                "Format" = "R8"
                "Size" = "1/4"
                "EmptyColor" = "FF000000"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            #pragma vertex TerrainLitPassVertex
            #pragma fragment TerrainLitBaseGenMetallic

            half4 TerrainLitBaseGenMetallic(Varyings IN) : SV_Target
            {
                // TODO: Generate metallic
                return float4(0, 0, 0, 0);
            }

            ENDHLSL
        }
    }
}