#ifndef _GRP_UNLIT_INPUT
#define _GRP_UNLIT_INPUT

#include "Common.hlsl"

TEXTURE2D(_MainTex);
TEXTURE2D(_EmissionTex);
SAMPLER(sampler_MainTex);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
	UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
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
	float4 textureColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, config.texcoord);
	float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
	return textureColor * color;
}

float3 GetEmission(InputConfig config)
{
	float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissionColor);
#if defined(_HAS_EMISSION_TEXTURE)
	color *= SAMPLE_TEXTURE2D(_EmissionTex, sampler_MainTex, config.texcoord);
#endif
	return color.rgb;
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

void ClipAlpha(float alpha, InputConfig config)
{
	AlphaDiscard(alpha, GetCutoff(config));
}

#endif