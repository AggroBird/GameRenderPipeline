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

// Experimental hatching
#if defined(_HATCHING_ENABLED)

TEXTURE2D(_Hatch0);
TEXTURE2D(_Hatch1);
SAMPLER(sampler_Hatch0);
SAMPLER(sampler_Hatch1);
float _HatchScale;
float _HatchIntensity;

float3 HatchingConstantScale(float2 _uv, float _intensity, float _dist)
{
	float log2_dist = log2(_dist);

	float2 floored_log_dist = floor((log2_dist + float2(0.0, 1.0)) * 0.5) * 2.0 - float2(0.0, 1.0);
	float2 uv_scale = min(1, pow(2.0, floored_log_dist));

	float uv_blend = abs(frac(log2_dist * 0.5) * 2.0 - 1.0);

	float2 scaledUVA = _uv / uv_scale.x; // 16
	float2 scaledUVB = _uv / uv_scale.y; // 8 

	float3 hatch0A = SAMPLE_TEXTURE2D(_Hatch0, sampler_Hatch0, scaledUVA).rgb;
	float3 hatch1A = SAMPLE_TEXTURE2D(_Hatch1, sampler_Hatch1, scaledUVA).rgb;

	float3 hatch0B = SAMPLE_TEXTURE2D(_Hatch0, sampler_Hatch0, scaledUVB).rgb;
	float3 hatch1B = SAMPLE_TEXTURE2D(_Hatch1, sampler_Hatch1, scaledUVB).rgb;

	float3 hatch0 = lerp(hatch0A, hatch0B, uv_blend);
	float3 hatch1 = lerp(hatch1A, hatch1B, uv_blend);

	float3 overbright = max(0, _intensity - 1.0);

	float3 weightsA = saturate((_intensity * 6.0) + float3(-0, -1, -2));
	float3 weightsB = saturate((_intensity * 6.0) + float3(-3, -4, -5));

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
	float intensity = dot(color, float3(0.2326, 0.7152, 0.0722)) * _HatchIntensity;
	color = diffuse * HatchingConstantScale(texcoord * _HatchScale, intensity, distance(_WorldSpaceCameraPos.xyz, positionWS) * unity_CameraInvProjection[0][0]);
}
#endif

#endif