#ifndef _GRP_LIT_INPUT
#define _GRP_LIT_INPUT

#include "Common.hlsl"

TEXTURE2D(_MainTex);
TEXTURE2D(_EmissionTex);
TEXTURE2D(_NormalTex);
TEXTURE2D(_SurfaceTex);
SAMPLER(sampler_MainTex);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
	UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionTex_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _NormalTex_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _SurfaceTex_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
	UNITY_DEFINE_INSTANCED_PROP(float, _IndividualShadowStrength)
#if defined(GRP_LIT_ADDITIONAL_PER_MATERIAL_PROPERTIES)
	GRP_LIT_ADDITIONAL_PER_MATERIAL_PROPERTIES
#endif
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


struct InputConfig
{
	float2 texcoord;
#if defined(_HAS_EMISSION_TEXTURE)
	float2 emission_Texcoord;
#endif
#if defined(_HAS_NORMAL_TEXTURE)
	float2 normal_Texcoord;
#endif
#if defined(_HAS_SURFACE_TEXTURE)
	float2 surface_Texcoord;
#endif
};

InputConfig GetInputConfig(float2 texcoord)
{
	InputConfig config;
	config.texcoord = TransformTexcoord(texcoord, PER_MATERIAL_PROP(_MainTex_ST));
#if defined(_HAS_EMISSION_TEXTURE)
	config.emission_Texcoord = TransformTexcoord(texcoord, PER_MATERIAL_PROP(_EmissionTex_ST));
#endif
#if defined(_HAS_NORMAL_TEXTURE)
	config.normal_Texcoord = TransformTexcoord(texcoord, PER_MATERIAL_PROP(_NormalTex_ST));
#endif
#if defined(_HAS_SURFACE_TEXTURE)
	config.surface_Texcoord = TransformTexcoord(texcoord, PER_MATERIAL_PROP(_SurfaceTex_ST));
#endif
	return config;
}

float4 GetDiffuse(InputConfig config)
{
	float4 color = PER_MATERIAL_PROP(_Color);
	color *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, config.texcoord);
	return color;
}

float3 GetEmission(InputConfig config)
{
	float4 color = PER_MATERIAL_PROP(_EmissionColor);
#if defined(_HAS_EMISSION_TEXTURE)
	color *= SAMPLE_TEXTURE2D(_EmissionTex, sampler_MainTex, config.emission_Texcoord);
#endif
	return color.rgb;
}

float3 GetNormal(InputConfig config)
{
#if defined(_HAS_NORMAL_TEXTURE)
	float4 map = SAMPLE_TEXTURE2D(_NormalTex, sampler_MainTex, config.normal_Texcoord);
	float scale = PER_MATERIAL_PROP(_NormalScale);
	return DecodeNormal(map, scale);
#else
	return float3(0, 1, 0);
#endif
}

float GetAlpha(InputConfig config)
{
	return GetDiffuse(config).a;
}

float GetFinalAlpha(float alpha)
{
	return PER_MATERIAL_PROP(_ZWrite) ? 1.0 : alpha;
}

float GetCutoff(InputConfig config)
{
	return PER_MATERIAL_PROP(_Cutoff);
}

struct SurfaceInfo
{
	float metallic;
	float smoothness;
	float fresnel;
};

SurfaceInfo GetSurfaceInfo(InputConfig config)
{
	SurfaceInfo result;
	result.metallic = PER_MATERIAL_PROP(_Metallic);
	result.smoothness = PER_MATERIAL_PROP(_Smoothness);
	result.fresnel = PER_MATERIAL_PROP(_Fresnel);
#if defined(_HAS_SURFACE_TEXTURE)
	float3 msf = SAMPLE_TEXTURE2D(_SurfaceTex, sampler_MainTex, config.surface_Texcoord).xyz;
	result.metallic *= msf.x;
	result.smoothness *= msf.y;
	result.fresnel *= msf.z;
#endif
	return result;
}

void ClipAlpha(float alpha, InputConfig config)
{
	AlphaDiscard(alpha, GetCutoff(config));
}

#endif