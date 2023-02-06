#ifndef _GRP_GLOBAL_ILLUMINATION
#define _GRP_GLOBAL_ILLUMINATION

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "Environment.hlsl"

float4 _AmbientLightColor;

struct GlobalIllumination
{
	float3 specular;
	float3 ambient;
};

float3 SampleEnvironment(Surface surface, BRDF brdf)
{
	float3 dir = reflect(-surface.viewDirection, surface.normal);
	float mip = PerceptualRoughnessToMipmapLevel(brdf.perceptualRoughness, 6);
	return SampleSkyboxCubemap(dir, mip).rgb;
}

GlobalIllumination GetGlobalIllumination(Surface surface, BRDF brdf)
{
	GlobalIllumination gi;
	gi.specular = SampleEnvironment(surface, brdf);
	gi.ambient = _AmbientLightColor.rgb;
	return gi;
}

#endif