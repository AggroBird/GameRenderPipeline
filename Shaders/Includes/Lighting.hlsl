#ifndef _GRP_LIGHTING
#define _GRP_LIGHTING

#include "Common.hlsl"
#include "Surface.hlsl"
#include "Shadows.hlsl"
#include "Light.hlsl"
#include "BRDF.hlsl"
#include "GlobalIllumination.hlsl"

float3 IncomingLight(Surface surface, Light light)
{
	float d = saturate(dot(surface.normal, light.direction) * light.attenuation);
	return light.color * d;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
	return DirectBRDF(surface, brdf, light) * IncomingLight(surface, light);
}

float3 GetTotalLighting(Surface surface, BRDF brdf, GlobalIllumination globalIllumination)
{
	ShadowData shadowData = GetShadowData(surface);

	float3 color = IndirectBRDF(surface, brdf, globalIllumination.specular);
	for (int i = 0; i < GetDirectionalLightCount(); i++)
	{
		Light light = GetDirectionalLight(i, surface, shadowData);
		color += GetLighting(surface, brdf, light);
	}

#if defined(_LIGHTS_PER_OBJECT)
	for (int j = 0; j < min(unity_LightData.y, 8); j++)
	{
		int lightIndex = unity_LightIndices[(uint)j / 4][(uint)j % 4];
		Light light = GetOtherLight(lightIndex, surface, shadowData);
		color += GetLighting(surface, brdf, light);
	}
#else
	for (int j = 0; j < GetOtherLightCount(); j++)
	{
		Light light = GetOtherLight(j, surface, shadowData);
		color += GetLighting(surface, brdf, light);
	}
#endif

	// Ambient
	color += brdf.diffuse * globalIllumination.ambient;

	return color;
}

float GetPrimaryDirectionalShadow(float3 positionWS)
{
	ShadowData global = GetShadowData(positionWS, -TransformWorldToView(positionWS).z, 0);

	for (int i = 0; i < GetDirectionalLightCount(); i++)
	{
		DirectionalShadowData dirShadowData = GetDirectionalShadowData(i, global);
		return GetDirectionalShadowValue(dirShadowData, global, positionWS);
	}

	return 1;
}

#endif