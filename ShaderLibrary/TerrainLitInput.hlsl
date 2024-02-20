#ifndef _GRP_TERRAIN_LIT_INPUT
#define _GRP_TERRAIN_LIT_INPUT

#include "Common.hlsl"

TEXTURE2D(_Control);    SAMPLER(sampler_Control);
TEXTURE2D(_Splat0);     SAMPLER(sampler_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);

#ifdef _NORMALMAP
TEXTURE2D(_Normal0);     SAMPLER(sampler_Normal0);
TEXTURE2D(_Normal1);
TEXTURE2D(_Normal2);
TEXTURE2D(_Normal3);
#endif

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

#if defined(_ALPHATEST_ON)
TEXTURE2D(_TerrainHolesTexture);
SAMPLER(sampler_TerrainHolesTexture);
#endif

void ClipHoles(float2 texcoord)
{
#if defined(_ALPHATEST_ON)
	float hole = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, texcoord).r;
	clip(hole == 0.0f ? -1 : 1);
#endif
}

#if defined(UNITY_INSTANCING_ENABLED)
TEXTURE2D(_TerrainHeightmapTexture);
TEXTURE2D(_TerrainNormalmapTexture);
SAMPLER(sampler_TerrainNormalmapTexture);
#endif

UNITY_INSTANCING_BUFFER_START(Terrain)
	UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
UNITY_INSTANCING_BUFFER_END(Terrain)

CBUFFER_START(_Terrain)
	float _Metallic0, _Metallic1, _Metallic2, _Metallic3;
	float _Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3;
	float4 _DiffuseRemapScale0, _DiffuseRemapScale1, _DiffuseRemapScale2, _DiffuseRemapScale3;

	float4 _Control_ST;
	float4 _Control_TexelSize;
	float4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;

#if defined(UNITY_INSTANCING_ENABLED)
	float4 _TerrainHeightmapRecipSize;   // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
	float4 _TerrainHeightmapScale;       // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
#endif

#ifdef SCENESELECTIONPASS
	int _ObjectId;
	int _PassValue;
#endif
CBUFFER_END


struct InputConfig
{
	float2 texcoord;

	float2 splatTexcoord;
	float4 splatControl;

	float4 texSplat01;
	float4 texSplat23;

	float4 splatDiffuse;
	float splatSmoothness;
	float splatMetallic;
};

InputConfig GetTerrainInputConfig(float2 texcoord, float4 texSplat01, float4 texSplat23)
{
	InputConfig config;
	config.texcoord = texcoord;

	config.splatTexcoord = (texcoord * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
	config.splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, config.splatTexcoord);

	config.texSplat01 = texSplat01;
	config.texSplat23 = texSplat23;

	config.splatDiffuse = float4(0, 0, 0, 0);
	config.splatSmoothness = 0;
	config.splatMetallic = 0;

	return config;
}
InputConfig GetInputConfig(float2 texcoord)
{
	return GetTerrainInputConfig(texcoord, float4(0, 0, 0, 0), float4(0, 0, 0, 0));
}

void SplatmapMix(inout InputConfig config)
{
	float4 diffuseTex[4];
	diffuseTex[0] = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, config.texSplat01.xy);
	diffuseTex[1] = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, config.texSplat01.zw);
	diffuseTex[2] = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, config.texSplat23.xy);
	diffuseTex[3] = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, config.texSplat23.zw);

	float4 splatAlpha = half4(diffuseTex[0].a, diffuseTex[1].a, diffuseTex[2].a, diffuseTex[3].a);
	float4 defaultSmoothness = splatAlpha * float4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
	float4 defaultMetallic = float4(_Metallic0, _Metallic1, _Metallic2, _Metallic3);

	float4 opacityAsDensity = saturate((splatAlpha - (float4(1.0, 1.0, 1.0, 1.0) - config.splatControl)) * 20.0);
	opacityAsDensity += 0.001 * config.splatControl;
	float4 useOpacityAsDensityParam = { _DiffuseRemapScale0.w, _DiffuseRemapScale1.w, _DiffuseRemapScale2.w, _DiffuseRemapScale3.w };
	config.splatControl = lerp(opacityAsDensity, config.splatControl, useOpacityAsDensityParam);

	float weight = dot(config.splatControl, 1.0);
#if defined(TERRAIN_SPLAT_ADDPASS)
	clip(weight <= 0.005 ? -1.0 : 1.0);
#endif
	config.splatControl /= (weight + HALF_MIN);

	config.splatDiffuse += diffuseTex[0] * half4(_DiffuseRemapScale0.rgb * config.splatControl.rrr, 1.0);
	config.splatDiffuse += diffuseTex[1] * half4(_DiffuseRemapScale1.rgb * config.splatControl.ggg, 1.0);
	config.splatDiffuse += diffuseTex[2] * half4(_DiffuseRemapScale2.rgb * config.splatControl.bbb, 1.0);
	config.splatDiffuse += diffuseTex[3] * half4(_DiffuseRemapScale3.rgb * config.splatControl.aaa, 1.0);

	config.splatSmoothness = dot(config.splatControl, defaultSmoothness);
	config.splatMetallic = dot(config.splatControl, defaultMetallic);

	config.splatDiffuse.a = weight;
}

float4 GetDiffuse(InputConfig config)
{
	return config.splatDiffuse;
}

float GetAlpha(InputConfig config)
{
	return 1;
}

float GetCutoff(InputConfig config)
{
	return 1;
}

float GetMetallic(InputConfig config)
{
	return config.splatMetallic;
}

float GetSmoothness(InputConfig config)
{
	return config.splatSmoothness;
}

#endif