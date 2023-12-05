#ifndef _GRP_LIGHTING
#define _GRP_LIGHTING

#include "Common.hlsl"
#include "Surface.hlsl"
#include "Shadows.hlsl"
#include "Light.hlsl"
#include "BRDF.hlsl"
#include "GlobalIllumination.hlsl"

#if defined(_CELL_SHADING_ENABLED)
float _CellShading_Steps;
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
	float a = saturate(d * light.attenuation);
#if defined(_CELL_SHADING_ENABLED)
	a = SAMPLE_TEXTURE2D(_CellShading_Falloff, sampler_CellShading_Falloff, float2(a, 0.5)).rgb;
#endif
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

// Experimental hatching
#if defined(_HATCHING_ENABLED)

TEXTURE2D(_Hatching_Dark);
TEXTURE2D(_Hatching_Bright);
SAMPLER(sampler_Hatching_Dark);
SAMPLER(sampler_Hatching_Bright);
float _Hatching_Scale;
float _Hatching_Intensity;

float3 HatchingConstantScale(float2 texcoord, float intensity, float distance)
{
	float log2_dist = log2(distance);

	float2 floored_log_dist = floor((log2_dist + float2(0.0, 1.0)) * 0.5) * 2.0 - float2(0.0, 1.0);
	float2 uv_scale = min(1, pow(2.0, floored_log_dist));

	float uv_blend = abs(frac(log2_dist * 0.5) * 2.0 - 1.0);

	float2 scaledUVA = texcoord / uv_scale.x; // 16
	float2 scaledUVB = texcoord / uv_scale.y; // 8 

	float3 hatch0A = SAMPLE_TEXTURE2D(_Hatching_Dark, sampler_Hatching_Dark, scaledUVA).rgb;
	float3 hatch1A = SAMPLE_TEXTURE2D(_Hatching_Bright, sampler_Hatching_Bright, scaledUVA).rgb;

	float3 hatch0B = SAMPLE_TEXTURE2D(_Hatching_Dark, sampler_Hatching_Dark, scaledUVB).rgb;
	float3 hatch1B = SAMPLE_TEXTURE2D(_Hatching_Bright, sampler_Hatching_Bright, scaledUVB).rgb;

	float3 hatch0 = lerp(hatch0A, hatch0B, uv_blend);
	float3 hatch1 = lerp(hatch1A, hatch1B, uv_blend);

	float3 overbright = max(0, intensity - 1.0);

	float3 weightsA = saturate((intensity * 6.0) + float3(-0, -1, -2));
	float3 weightsB = saturate((intensity * 6.0) + float3(-3, -4, -5));

	weightsA.xy -= weightsA.yz;
	weightsA.z -= weightsB.x;
	weightsB.xy -= weightsB.yz;

	hatch0 = hatch0 * weightsA;
	hatch1 = hatch1 * weightsB;

	float3 hatching = overbright + hatch0.r +
		hatch0.g + hatch0.b +
		hatch1.r + hatch1.g +
		hatch1.b;

	return hatching;
}

void ApplyHatching(inout float3 color, float3 diffuse, float3 positionWS, float2 texcoord)
{
	float intensity = dot(color, float3(0.2326, 0.7152, 0.0722)) * _Hatching_Intensity;
	float dist = distance(_WorldSpaceCameraPos.xyz, positionWS) * unity_CameraInvProjection[0][0];
	color = diffuse * HatchingConstantScale(texcoord * _Hatching_Scale, intensity, dist);
}
#endif

#endif