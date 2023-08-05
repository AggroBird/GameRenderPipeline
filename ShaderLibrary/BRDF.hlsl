#ifndef _GRP_BRDF
#define _GRP_BRDF

struct BRDF
{
	float3 diffuse;
	float3 specular;
	float roughness;
	float perceptualRoughness;
	float fresnel;
};

#define MIN_REFLECTIVITY 0.04

float OneMinusReflectivity(float metallic)
{
	float range = 1.0 - MIN_REFLECTIVITY;
	return range - metallic * range;
}

BRDF GetBRDF(Surface surface)
{
	BRDF brdf;

	float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
	brdf.diffuse = surface.color * oneMinusReflectivity;
	brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);

	brdf.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(brdf.perceptualRoughness);

	brdf.fresnel = saturate(surface.smoothness + 1.0 - oneMinusReflectivity);

	return brdf;
}

float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = pow2(saturate(dot(surface.normal, h)));
	float lh2 = pow2(saturate(dot(light.direction, h)));
	float r2 = pow2(brdf.roughness);
	float d2 = pow2(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 IndirectBRDF(Surface surface, BRDF brdf, float3 specular)
{
	float fresnelStrength = surface.fresnel * Pow4(1.0 - saturate(dot(surface.normal, surface.viewDirection)));
	float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
	reflection /= brdf.roughness * brdf.roughness + 1.0;
	return reflection;
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
	return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif