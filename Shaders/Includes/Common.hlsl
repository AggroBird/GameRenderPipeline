#ifndef _GRP_COMMON
#define _GRP_COMMON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
#define UNITY_PREV_MATRIX_M   unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

#define TEXTURE2D_SAMPLER2D(textureName, samplerName) TEXTURE2D(textureName); SAMPLER(samplerName)


#if UNITY_REVERSED_Z
    #if SHADER_API_OPENGL || SHADER_API_GLES || SHADER_API_GLES3
        //GL with reversed z => z clip range is [near, -far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(-(coord), 0)
    #else
        //D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
        //max is required to protect ourselves from near plane not being correct/meaningfull in case of oblique matrices.
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
    #endif
#elif UNITY_UV_STARTS_AT_TOP
    //D3d without reversed z => z clip range is [0, far] -> nothing to do
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
    //Opengl => z clip range is [-near, far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#endif


float pow2(float f)
{
	return f * f;
}

float DistanceSquared(float3 a, float3 b)
{
	return dot(a - b, a - b);
}

////////////////////////////////
// VERTEX TRANSFORMATION
////////////////////////////////
struct VertexPositions
{
	float3 positionWS;  // World space position
	float3 positionVS;  // View space position
	float4 positionCS;  // Homogeneous clip space position
	float4 positionNDC; // Homogeneous normalized device coordinates
};

float4 GetPositionNDC(float4 positionCS)
{
	float4 ndc = positionCS * 0.5f;
	ndc.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
	ndc.zw = positionCS.zw;
	return ndc;
}

VertexPositions GetVertexPositions(float3 positionOS)
{
    VertexPositions result;
    result.positionWS = TransformObjectToWorld(positionOS);
    result.positionVS = TransformWorldToView(result.positionWS);
    result.positionCS = TransformWorldToHClip(result.positionWS);
	result.positionNDC = GetPositionNDC(result.positionCS);
    return result;
}

float2 TransformTexcoord(float2 texcoord, float4 ST)
{
	return texcoord * ST.xy + ST.zw;
}


float3 DecodeNormal(float4 sample, float scale)
{
#if defined(UNITY_NO_DXT5nm)
	return UnpackNormalRGB(sample, scale);
#else
	return UnpackNormalmapRGorAG(sample, scale);
#endif
}

float3 NormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS)
{
	float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
	return TransformTangentToWorld(normalTS, tangentToWorld);
}


////////////////////////////////
// FRAGMENT OUTPUT
////////////////////////////////

#define USE_NORMAL_BUFFER 1

struct FragmentOutput
{
	float4 color : SV_TARGET0;
#if defined(_OUTPUT_NORMALS_ON)
	float4 normal : SV_TARGET1;
#endif
};

FragmentOutput MakeFragmentOutput(float4 color, float3 normal)
{
	FragmentOutput result;
	result.color = color;
#if defined(_OUTPUT_NORMALS_ON)
	result.normal = float4(PackNormalOctRectEncode(normalize(mul((float3x3)GetWorldToViewMatrix(), normal).xyz)), 0.0, 0.0);
#endif
	return result;
}



////////////////////////////////
// CLIPPING
////////////////////////////////
void ClipLOD(float2 clipSpacePosition, float fade)
{
#if defined(LOD_FADE_CROSSFADE)
	float dither = InterleavedGradientNoise(clipSpacePosition.xy, 0);
	clip(fade + (fade < 0.0 ? dither : -dither));
#endif
}

void AlphaDiscard(float alpha, float cutoff, float offset = 0.0h)
{
#if defined(_ALPHATEST_ON)
	clip(alpha - cutoff + offset);
#endif
}



////////////////////////////////
// COLOR CORRECTION
////////////////////////////////

float3 RGBToLinear(float3 color)
{
	float3 sRGBLo = color * 12.92;
	float3 sRGBHi = (pow(max(abs(color), 1.192092896e-07), float3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
	return float3(color <= 0.0031308) ? sRGBLo : sRGBHi;
}

float3 LinearToRGB(float3 color)
{
	float3 linearRGBLo = color / 12.92;
	float3 linearRGBHi = pow(max(abs((color + 0.055) / 1.055), 1.192092896e-07), float3(2.4, 2.4, 2.4));
	return float3(color <= 0.04045) ? linearRGBLo : linearRGBHi;
}

void ApplyLinearColorCorrection(inout float3 rgb)
{
#if defined(_COLOR_SPACE_LINEAR)
	rgb = LinearToRGB(rgb);
#endif
}
float3 CorrectLinearColor(float3 rgb)
{
#if defined(_COLOR_SPACE_LINEAR)
    rgb = LinearToRGB(rgb);
#endif
    return rgb;
}


////////////////////////////////
// DEPTH/TRACE UTILITY
////////////////////////////////

// Non-normalized trace direction from camera
float3 CameraTraceDirection(float2 positionSS)
{
	float3 cameraRight = unity_CameraToWorld._m00_m10_m20;
	float3 cameraUp = unity_CameraToWorld._m01_m11_m21;
	float3 cameraForward = unity_CameraToWorld._m02_m12_m22;
	float2 rayUV = (positionSS * 2) - float2(1, 1);
	float fov = 1.0 / unity_CameraProjection._m11;
	float aspect = _ScreenParams.x / _ScreenParams.y;
	return cameraForward + cameraUp * (rayUV.y * fov) + cameraRight * (rayUV.x * fov * aspect);
}

float Linear01Depth(float depth)
{
	return Linear01Depth(depth, _ZBufferParams);
}

float InverseLinear01Depth(float depth, float4 zBufferParam)
{
	return (1.0 / depth - zBufferParam.y) / zBufferParam.x;
}
float InverseLinear01Depth(float depth)
{
	return InverseLinear01Depth(depth, _ZBufferParams);
}


#endif