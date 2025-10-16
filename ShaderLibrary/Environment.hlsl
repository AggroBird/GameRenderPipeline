#ifndef _GRP_ENVIRONMENT
#define _GRP_ENVIRONMENT

// Fog
#if defined(_FOG_LINEAR) || defined(_FOG_EXP) || defined(_FOG_EXP2)
#define FOG_ENABLED

// rgb = Groundcolor, a = fog blend
float4 _FogAmbientColor;
float3 _FogInscatteringColor;
float3 _FogLightDirection;
float4 _FogParam;

#define FOG_ATTRIBUTE_TYPE float4

float ComputeFogFactor(float fogAttributeCSZ)
{
#if defined(_FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    return saturate(UNITY_Z_0_FAR_FROM_CLIPSPACE(fogAttributeCSZ) * _FogParam.x + _FogParam.y);
#elif defined(_FOG_EXP) || defined(_FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // -density * z computed at vertex
    return _FogParam.x * UNITY_Z_0_FAR_FROM_CLIPSPACE(fogAttributeCSZ);
#else
    return 0;
#endif
}

float ComputeFogIntensity(float fogAttribute)
{
    float fogIntensity = 0.0;

#if defined(_FOG_EXP)
    // factor = exp(-density*z)
    // fogFactor = density*z compute at vertex
    fogIntensity = 1 - saturate(exp2(-fogAttribute));
#elif defined(_FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // fogFactor = density*z compute at vertex
    fogIntensity = 1 - saturate(exp2(-fogAttribute * fogAttribute));
#elif defined(_FOG_LINEAR)
    fogIntensity = 1 - fogAttribute;
#endif

    return fogIntensity * _FogAmbientColor.a;
}

void ApplyFog(inout float3 rgb, FOG_ATTRIBUTE_TYPE fogAttribute)
{
    float fogFactor = ComputeFogFactor(fogAttribute.x);
    float3 viewDir = normalize(fogAttribute.yzw - _WorldSpaceCameraPos);
    float inscatteringBlend = pow(max(dot(viewDir, _FogLightDirection) - saturate(-viewDir.y), 0), 8);
    float3 fogColor = lerp(_FogAmbientColor.rgb, _FogInscatteringColor, saturate(inscatteringBlend));
    rgb = lerp(rgb, fogColor, ComputeFogIntensity(fogFactor));
}

void BlendFog(inout float3 rgb, FOG_ATTRIBUTE_TYPE fogAttribute)
{
    float fogFactor = ComputeFogFactor(fogAttribute.x);
    rgb = lerp(rgb, float3(0, 0, 0), ComputeFogIntensity(fogFactor));
}

#define FOG_ATTRIBUTE(idx) FOG_ATTRIBUTE_TYPE fogAttribute : TEXCOORD##idx;
#define TRANSFER_FOG(o, v) o.fogAttribute = float4(vertexPositions.positionCS.z, vertexPositions.positionWS)
#define APPLY_FOG(i, rgb) ApplyFog(rgb, i.fogAttribute)
#define BLEND_FOG(i, rgb) BlendFog(rgb, i.fogAttribute)

#else

#define FOG_ATTRIBUTE(idx)
#define TRANSFER_FOG(o, v)
#define APPLY_FOG(i, rgb)
#define BLEND_FOG(i, rgb)

#endif


// Skybox
TEXTURE2D(_SkyboxGradientTexture);
SAMPLER(sampler_SkyboxGradientTexture);

float3 _SkyboxGroundColor;

float3 SampleSkyboxGradient(float3 dir, bool applyGroundColor = false)
{
    float3 skyColor = SAMPLE_TEXTURE2D(_SkyboxGradientTexture, sampler_SkyboxGradientTexture, float2(max(dir.y, 0), 0.5)).rgb;
    if (applyGroundColor)
    {
        skyColor = lerp(skyColor, _SkyboxGroundColor.rgb, saturate(dir.y * -15));
    }
    return skyColor;
}

TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);

float _SkyboxAnimTime;

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"


#if !defined(_SCENE_LIGHT_OVERRIDE)

float3 SampleSkyboxCubemap(float3 dir, float mip = 0)
{
    float4 environment = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, dir, mip);
    return DecodeHDREnvironment(environment, unity_SpecCube0_HDR);
}

#else

float3 SampleSkyboxCubemap(float3 dir, float mip = 0)
{
    return float3(0, 0, 0);
}

#endif

#endif