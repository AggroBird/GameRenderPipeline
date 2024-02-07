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
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionTex_ST)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
#if defined(GRP_UNLIT_ADDITIONAL_PER_MATERIAL_PROPERTIES)
	GRP_UNLIT_ADDITIONAL_PER_MATERIAL_PROPERTIES
#endif
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


struct InputConfig
{
	float2 texcoord;
#if defined(_HAS_EMISSION_TEXTURE)
	float2 emission_Texcoord;
#endif
};

InputConfig GetInputConfig(float2 texcoord)
{
	InputConfig config;
	config.texcoord = texcoord;
#if defined(_HAS_EMISSION_TEXTURE)
	config.emission_Texcoord = TransformTexcoord(texcoord, PER_MATERIAL_PROP(_EmissionTex_ST));
#endif
	return config;
}

float4 GetDiffuse(InputConfig config)
{
	float4 textureColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, config.texcoord);
	float4 color = PER_MATERIAL_PROP(_Color);
	return textureColor * color;
}

float3 GetEmission(InputConfig config)
{
	float4 color = PER_MATERIAL_PROP(_EmissionColor);
#if defined(_HAS_EMISSION_TEXTURE)
	color *= SAMPLE_TEXTURE2D(_EmissionTex, sampler_MainTex, config.emission_Texcoord);
#endif
	return color.rgb;
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

void ClipAlpha(float alpha, InputConfig config)
{
	AlphaDiscard(alpha, GetCutoff(config));
}

#endif