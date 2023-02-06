#ifndef _GRP_SURFACE
#define _GRP_SURFACE

struct Surface
{
	float3 position;
	float3 normal;
	float3 interpolatedNormal;
	float3 viewDirection;
	float depth;
	float3 color;
	float alpha;
	float metallic;
	float smoothness;
	float fresnel;
	float dither;
};

Surface MakeSurface(float4 diffuse, float3 positionWS, float3 interpolatedNormalWS, float metallic, float smoothness, float fresnel)
{
	Surface surface;
	surface.position = positionWS;
	surface.normal = normalize(interpolatedNormalWS);
	surface.interpolatedNormal = interpolatedNormalWS;
	surface.viewDirection = normalize(_WorldSpaceCameraPos - positionWS);
	surface.depth = -TransformWorldToView(positionWS).z;
	surface.color = diffuse.rgb;
	surface.alpha = diffuse.a;
	surface.metallic = metallic;
	surface.smoothness = smoothness;
	surface.fresnel = fresnel;
	surface.dither = 0;
	return surface;
}

Surface MakeSurface(float4 diffuse, float3 positionWS, float3 interpolatedNormalWS, float metallic, float smoothness, float fresnel, float2 ditherUV)
{
	Surface surface = MakeSurface(diffuse, positionWS, interpolatedNormalWS, metallic, smoothness, fresnel);
	surface.dither = InterleavedGradientNoise(ditherUV, 0);
	return surface;
}


#endif