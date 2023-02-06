#ifndef _GRP_LIT_INPUT
#define _GRP_LIT_INPUT

#include "Common.hlsl"

TEXTURE2D(_MainTex);
TEXTURE2D(_EmissionTex);
TEXTURE2D(_NormalTex);
SAMPLER(sampler_MainTex);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
	UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


struct InputConfig
{
	float2 texcoord;
};

InputConfig GetInputConfig(float2 texcoord)
{
	InputConfig config;
	config.texcoord = texcoord;
	return config;
}

float4 GetDiffuse(InputConfig config)
{
	float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
	color *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, config.texcoord);
	return color;
}

float3 GetEmission(InputConfig config)
{
	float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissionColor);
#if defined(_HAS_EMISSION)
	color *= SAMPLE_TEXTURE2D(_EmissionTex, sampler_MainTex, config.texcoord);
#endif
	return color.rgb;
}

float3 GetNormal(InputConfig config)
{
	float4 map = SAMPLE_TEXTURE2D(_NormalTex, sampler_MainTex, config.texcoord);
	float scale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalScale);
	return DecodeNormal(map, scale);
}

float GetAlpha(InputConfig config)
{
	return GetDiffuse(config).a;
}

float GetFinalAlpha(float alpha)
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ZWrite) ? 1.0 : alpha;
}

float GetCutoff(InputConfig config)
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetMetallic(InputConfig config)
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
}

float GetSmoothness(InputConfig config)
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
}

float GetFresnel(InputConfig config)
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Fresnel);
}

void ClipAlpha(float alpha, InputConfig config)
{
	AlphaDiscard(alpha, GetCutoff(config));
}

#endif