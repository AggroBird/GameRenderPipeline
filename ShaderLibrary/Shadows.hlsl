#ifndef _GRP_SHADOWS
#define _GRP_SHADOWS

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
	#define DIRECTIONAL_FILTER_SAMPLES 4
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#if defined(_OTHER_PCF3)
	#define OTHER_FILTER_SAMPLES 4
	#define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_OTHER_PCF5)
	#define OTHER_FILTER_SAMPLES 9
	#define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_OTHER_PCF7)
	#define OTHER_FILTER_SAMPLES 16
	#define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_SHADOWED_OTHER_LIGHT_COUNT 16
#define MAX_CASCADE_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
TEXTURE2D_SHADOW(_OtherShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
	int _CascadeCount;
	float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
	float4 _CascadeData[MAX_CASCADE_COUNT];
	float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
	float4x4 _OtherShadowMatrices[MAX_SHADOWED_OTHER_LIGHT_COUNT];
	float4 _OtherShadowTiles[MAX_SHADOWED_OTHER_LIGHT_COUNT];
	float4 _ShadowAtlasSize;
	float4 _ShadowDistanceFade;
CBUFFER_END


half FadedShadowStrength(half distance, half scale, half fade)
{
	return saturate((1.0 - distance * scale) * fade);
}

struct ShadowData
{
	int cascadeIndex;
	half cascadeBlend;
    half strength;
};

ShadowData GetShadowData(float3 positionWS, half depth, half dither)
{
	ShadowData data;
	data.cascadeBlend = 1.0;
	data.strength = FadedShadowStrength(depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);

	int i;
	for (i = 0; i < _CascadeCount; i++)
	{
		half4 sphere = _CascadeCullingSpheres[i];
        half distanceSqr = DistanceSquared(positionWS, sphere.xyz);
		if (distanceSqr < sphere.w)
		{
            half fade = FadedShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
			if (i == _CascadeCount - 1)
			{
				data.strength *= fade;
			}
			else
			{
				data.cascadeBlend = fade;
			}
			break;
		}
	}

	if (i == _CascadeCount && _CascadeCount > 0)
	{
		data.strength = 0.0;
	}
#if defined(_CASCADE_BLEND_DITHER)
	else if (data.cascadeBlend < dither)
	{
		i += 1;
	}
#endif
#if !defined(_CASCADE_BLEND_SOFT)
	data.cascadeBlend = 1.0;
#endif

	data.cascadeIndex = i;
	return data;
}

ShadowData GetShadowData(Surface surface)
{
	return GetShadowData(surface.position, surface.depth, surface.dither);
}

half SampleDirectionalShadowAtlas(float3 positionSTS)
{
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

half FilterDirectionalShadow(float3 positionSTS)
{
#if defined(DIRECTIONAL_FILTER_SETUP)
	real weights[DIRECTIONAL_FILTER_SAMPLES];
	real2 positions[DIRECTIONAL_FILTER_SAMPLES];
	float4 size = _ShadowAtlasSize.yyxx;
	DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
	half shadow = 0;
	for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++)
	{
		shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
	}
	return shadow;
#else
	return SampleDirectionalShadowAtlas(positionSTS);
#endif
}

half SampleOtherShadowAtlas(float3 positionSTS, float3 bounds)
{
	positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
	return SAMPLE_TEXTURE2D_SHADOW(_OtherShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

half FilterOtherShadow(float3 positionSTS, float3 bounds)
{
#if defined(OTHER_FILTER_SETUP)
	real weights[OTHER_FILTER_SAMPLES];
	real2 positions[OTHER_FILTER_SAMPLES];
	float4 size = _ShadowAtlasSize.wwzz;
	OTHER_FILTER_SETUP(size, positionSTS.xy, weights, positions);
	half shadow = 0;
	for (int i = 0; i < OTHER_FILTER_SAMPLES; i++)
	{
		shadow += weights[i] * SampleOtherShadowAtlas(float3(positions[i].xy, positionSTS.z), bounds);
	}
	return shadow;
#else
	return SampleOtherShadowAtlas(positionSTS, bounds);
#endif
}


struct DirectionalShadowData
{
    half strength;
	int tileIndex;
    half normalBias;
};

half GetDirectionalShadowAttenuation(DirectionalShadowData directional, ShadowData global, Surface surface)
{
#if !defined(_RECEIVE_SHADOWS)
	return 1.0;
#endif

	if (directional.strength <= 0.0) return 1.0;

	float3 normalBias = surface.interpolatedNormal * (directional.normalBias * _CascadeData[global.cascadeIndex].y);
	float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex], float4(surface.position + normalBias, 1.0)).xyz;
    half shadow = FilterDirectionalShadow(positionSTS);

	if (global.cascadeBlend < 1.0)
	{
		normalBias = surface.interpolatedNormal * (directional.normalBias * _CascadeData[global.cascadeIndex + 1].y);
		positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1], float4(surface.position + normalBias, 1.0)).xyz;
		shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
	}

	return lerp(1.0, shadow, directional.strength * surface.shadowStrength);
}
half GetDirectionalShadowValue(DirectionalShadowData directional, ShadowData global, float3 positionWS)
{
#if !defined(_RECEIVE_SHADOWS)
	return 1.0;
#endif

	if (directional.strength <= 0.0) return 1.0;

	float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex], float4(positionWS, 1.0)).xyz;
    half shadow = FilterDirectionalShadow(positionSTS);

	if (global.cascadeBlend < 1.0)
	{
		positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1], float4(positionWS, 1.0)).xyz;
		shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
	}

	return lerp(1.0, shadow, directional.strength);
}


struct OtherShadowData
{
    half strength;
	int tileIndex;
	bool isPoint;
	float3 lightPosition;
	float3 lightDirection;
	float3 spotDirection;
};

static const float3 pointShadowPlanes[6] =
{
	float3(-1.0, 0.0, 0.0),
	float3(1.0, 0.0, 0.0),
	float3(0.0, -1.0, 0.0),
	float3(0.0, 1.0, 0.0),
	float3(0.0, 0.0, -1.0),
	float3(0.0, 0.0, 1.0)
};

half GetOtherShadow(OtherShadowData other, ShadowData global, Surface surface)
{
	float tileIndex = other.tileIndex;
	float3 lightPlane = other.spotDirection;
	if (other.isPoint)
	{
		float faceOffset = CubeMapFaceID(-other.lightDirection);
		tileIndex += faceOffset;
		lightPlane = pointShadowPlanes[faceOffset];
	}

	float4 tileData = _OtherShadowTiles[tileIndex];
	float3 surfaceToLight = other.lightPosition - surface.position;
	float distanceToLightPlane = dot(surfaceToLight, lightPlane);
	float3 normalBias = surface.interpolatedNormal * (distanceToLightPlane * tileData.w);
	float4 positionSTS = mul(_OtherShadowMatrices[tileIndex], float4(surface.position + normalBias, 1.0));
	return FilterOtherShadow(positionSTS.xyz / positionSTS.w, tileData.xyz);
}

half GetOtherShadowAttenuation(OtherShadowData other, ShadowData global, Surface surface)
{
#if !defined(_RECEIVE_SHADOWS)
	return 1.0;
#endif

	if (global.strength * other.strength > 0)
	{
		return GetOtherShadow(other, global, surface);
	}
	else
	{
		return 1.0;
	}
}

#endif