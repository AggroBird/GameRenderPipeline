#ifndef _GRP_TERRAIN_GRASS_INPUT
#define _GRP_TERRAIN_GRASS_INPUT

#include "Common.hlsl"

CBUFFER_START(TerrainGrass)
	half4 _WavingTint;
	float4 _WaveAndDistance;
	float4 _CameraPosition;
	float3 _CameraRight, _CameraUp;
CBUFFER_END

CBUFFER_START(UnityPerMaterial)
	float4 _MainTex_ST;
	half4 _BaseColor;
	half4 _SpecColor;
	half4 _EmissionColor;
	half _Cutoff;
	half _Shininess;
CBUFFER_END

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);


#endif