#ifndef _GRP_LIGHT
#define _GRP_LIGHT

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64

CBUFFER_START(_CustomLight)
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

	int _OtherLightCount;
	float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];

#if defined(_SCENE_LIGHT_OVERRIDE)
	float4 _OverrideLightColor;
	float4 _OverrideLightDirection;
	float4 _OverrideLightAmbient;
#endif
CBUFFER_END

struct Light
{
	float3 color;
	float3 direction;
	float attenuation;
};

int GetDirectionalLightCount()
{
#if defined(_SCENE_LIGHT_OVERRIDE)
	return 1;
#endif
	
	return _DirectionalLightCount;
}

int GetOtherLightCount()
{
#if defined(_SCENE_LIGHT_OVERRIDE)
	return 0;
#endif
	
	return _OtherLightCount;
}

DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
	DirectionalShadowData data;
	data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
	data.normalBias = _DirectionalLightShadowData[lightIndex].z;
	return data;
}

Light GetDirectionalLight(int lightIndex, Surface surface, ShadowData shadowData)
{
    Light light;
#if defined(_SCENE_LIGHT_OVERRIDE)
	light.color = _OverrideLightColor.rgb;
	light.direction = _OverrideLightDirection.rgb;
	light.attenuation = 1;
	return light;
#endif
	
	light.color = _DirectionalLightColors[lightIndex].rgb;
	light.direction = _DirectionalLightDirections[lightIndex].xyz;

	DirectionalShadowData dirShadowData = GetDirectionalShadowData(lightIndex, shadowData);
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surface);

	return light;
}

OtherShadowData GetOtherShadowData(int lightIndex)
{
	OtherShadowData data;
	data.strength = _OtherLightShadowData[lightIndex].x;
	data.tileIndex = _OtherLightShadowData[lightIndex].y;
	data.isPoint = _OtherLightShadowData[lightIndex].z == 1.0;
	data.lightPosition = float3(0, 0, 0);
	data.lightDirection = float3(0, 0, 0);
	data.spotDirection = float3(0, 0, 0);
	return data;
}

Light GetOtherLight(int lightIndex, Surface surface, ShadowData shadowData)
{
	Light light;
	light.color = _OtherLightColors[lightIndex].rgb;
	float3 position = _OtherLightPositions[lightIndex].xyz;
	float3 ray = position - surface.position;
	light.direction = normalize(ray);
	float distanceSqr = max(dot(ray, ray), 0.00001);
	float rangeAttenuation = pow2(saturate(1.0 - pow2(distanceSqr * _OtherLightPositions[lightIndex].w)));
	float4 spotAngles = _OtherLightSpotAngles[lightIndex];
	float3 spotDirection = _OtherLightDirections[lightIndex].xyz;
	float spotAttenuation = pow2(saturate(dot(spotDirection, light.direction) * spotAngles.x + spotAngles.y));

	OtherShadowData otherShadowData = GetOtherShadowData(lightIndex);
	otherShadowData.lightPosition = position;
	otherShadowData.lightDirection = light.direction;
	otherShadowData.spotDirection = spotDirection;
	light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surface) * spotAttenuation * rangeAttenuation / distanceSqr;

	return light;
}

#endif