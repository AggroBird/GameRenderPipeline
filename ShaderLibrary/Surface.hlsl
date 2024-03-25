#ifndef _GRP_SURFACE
#define _GRP_SURFACE

float _GlobalShadowStrength;

struct Surface
{
	float3 position;
	half3 normal;
	half3 interpolatedNormal;
	half3 viewDirection;
	half depth;
	half3 color;
	half alpha;
	half metallic;
	half smoothness;
	half fresnel;
	half dither;
    half shadowStrength;
};

Surface MakeSurface(half4 diffuse, float3 positionWS, half3 interpolatedNormalWS, half metallic, half smoothness, half fresnel)
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
    surface.shadowStrength = _GlobalShadowStrength;
	return surface;
}

Surface MakeSurface(half4 diffuse, float3 positionWS, half3 interpolatedNormalWS, half metallic, half smoothness, half fresnel, float2 ditherUV)
{
	Surface surface = MakeSurface(diffuse, positionWS, interpolatedNormalWS, metallic, smoothness, fresnel);
	surface.dither = InterleavedGradientNoise(ditherUV, 0);
	return surface;
}


#endif