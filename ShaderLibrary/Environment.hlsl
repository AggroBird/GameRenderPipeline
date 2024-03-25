#ifndef _GRP_ENVIRONMENT
#define _GRP_ENVIRONMENT

// Fog
#if defined(_FOG_LINEAR) || defined(_FOG_EXP) || defined(_FOG_EXP2)
#define FOG_ENABLED

// rgb = Groundcolor, a = fog blend
half4 _FogAmbientColor;
half3 _FogInscatteringColor;
half3 _FogLightDirection;
float4 _FogParam;

// x = distance fog factor
// y = vertical fog factor
// z = light direction dot
// w = viewdir y
#define FOG_ATTRIBUTE_TYPE float4

FOG_ATTRIBUTE_TYPE ComputeFogFactor(VertexPositions vertexPositions)
{
    float clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(vertexPositions.positionCS.z);

    FOG_ATTRIBUTE_TYPE result;

#if defined(_FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    result.x = saturate(clipZ_01 * _FogParam.x + _FogParam.y);
#elif defined(_FOG_EXP) || defined(_FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // -density * z computed at vertex
    result.x = _FogParam.x * clipZ_01;
#else
    result.x = 0;
#endif


    half3 viewDir = (vertexPositions.positionWS - _WorldSpaceCameraPos);
    float viewLen = length(viewDir);
    viewDir /= viewLen;

    result.y = pow(max(dot(viewDir, _FogLightDirection) - saturate(-viewDir.y), 0), 8);
    result.z = viewLen;
    result.w = viewDir.y;
    //result.w = vertexPositions.positionWS.y;

    return result;
}

half ComputeFogIntensity(FOG_ATTRIBUTE_TYPE fogAttribute)
{
    half fogIntensity = 0.0;

#if defined(_FOG_EXP)
    // factor = exp(-density*z)
    // fogFactor = density*z compute at vertex
    fogIntensity = 1 - saturate(exp2(-fogAttribute.x));
#elif defined(_FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // fogFactor = density*z compute at vertex
    fogIntensity = 1 - saturate(exp2(-fogAttribute.x * fogAttribute.x));
#elif defined(_FOG_LINEAR)
    fogIntensity = 1 - fogAttribute.x;
#endif

    return fogIntensity * _FogAmbientColor.a;
}

void ApplyFog(inout half3 rgb, FOG_ATTRIBUTE_TYPE fogAttribute)
{
    half3 fogColor = lerp(_FogAmbientColor.rgb, _FogInscatteringColor, saturate(fogAttribute.y));
    rgb = lerp(rgb, fogColor, ComputeFogIntensity(fogAttribute));
}

void BlendFog(inout half3 rgb, FOG_ATTRIBUTE_TYPE fogAttribute)
{
    rgb = lerp(rgb, half3(0, 0, 0), ComputeFogIntensity(fogAttribute));
}

#define FOG_ATTRIBUTE(idx) FOG_ATTRIBUTE_TYPE fogAttribute : TEXCOORD##idx;
#define TRANSFER_FOG(o, v) o.fogAttribute = ComputeFogFactor(v)
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

half3 _SkyboxGroundColor;

half3 SampleSkyboxGradient(half3 dir, bool applyGroundColor = false)
{
    half3 skyColor = SAMPLE_TEXTURE2D(_SkyboxGradientTexture, sampler_SkyboxGradientTexture, float2(max(dir.y, 0), 0.5)).rgb;
    if (applyGroundColor)
    {
        skyColor = lerp(skyColor, _SkyboxGroundColor.rgb, saturate(dir.y * -15));
    }
    return skyColor;
}

TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

half3 SampleSkyboxCubemap(half3 dir, float mip = 0)
{
    half4 environment = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, dir, mip);
    return DecodeHDREnvironment(environment, unity_SpecCube0_HDR);
}

#endif