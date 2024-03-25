#ifndef _GRP_LIGHTING
#define _GRP_LIGHTING

#include "Common.hlsl"
#include "Surface.hlsl"
#include "Shadows.hlsl"
#include "Light.hlsl"
#include "BRDF.hlsl"
#include "GlobalIllumination.hlsl"

#if defined(_CELL_SHADING_ENABLED)
TEXTURE2D(_CellShading_Falloff);
SAMPLER(sampler_CellShading_Falloff);
#endif

#ifndef GRP_LIGHT_ATTENUATION_FUNC
#define GRP_LIGHT_ATTENUATION_FUNC DefaultLightAttenuation
#endif

#ifndef GRP_LIGHT_GET_TOTAL_FUNC
#define GRP_LIGHT_GET_TOTAL_FUNC DefaultGetLightTotal
#endif

float3 DefaultLightAttenuation(Surface surface, Light light)
{
	float d = dot(surface.normal, light.direction);
#if defined(_CELL_SHADING_ENABLED)
	d = SAMPLE_TEXTURE2D(_CellShading_Falloff, sampler_CellShading_Falloff, float2(d, 0.5)).r;
#endif
    float a = lerp(1, saturate(d * light.attenuation), surface.shadowStrength);
	return light.color * a;
}

float3 DefaultGetLightTotal(Surface surface, BRDF brdf, GlobalIllumination globalIllumination)
{
	ShadowData shadowData = GetShadowData(surface);
	
	// Metallic, smoothness and fresnel
	float3 indirect = IndirectBRDF(surface, brdf, globalIllumination.specular);
	
	// Direct lights
	float3 result = indirect;
	for (int i = 0; i < GetDirectionalLightCount(); i++)
	{
		Light light = GetDirectionalLight(i, surface, shadowData);
		result += DirectBRDF(surface, brdf, light) * GRP_LIGHT_ATTENUATION_FUNC(surface, light);
	}

	// Other lights
#if defined(_LIGHTS_PER_OBJECT)
	for (int j = 0; j < min(unity_LightData.y, 8); j++)
	{
		int lightIndex = unity_LightIndices[(uint)j / 4][(uint)j % 4];
		Light light = GetOtherLight(lightIndex, surface, shadowData);
		result += DirectBRDF(surface, brdf, light) * GRP_LIGHT_ATTENUATION_FUNC(surface, light);
	}
#else
	for (int j = 0; j < GetOtherLightCount(); j++)
	{
		Light light = GetOtherLight(j, surface, shadowData);
		result += DirectBRDF(surface, brdf, light) * GRP_LIGHT_ATTENUATION_FUNC(surface, light);
	}
#endif

	// Ambient
	result += brdf.diffuse * globalIllumination.ambient;

	return result;
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