#ifndef _GRP_POST_PROCESS_INPUT
#define _GRP_POST_PROCESS_INPUT

#include "BlitPass.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

TEXTURE2D(_PostProcessInputTex);
TEXTURE2D(_PostProcessCombineTex);
float4 _PostProcessInputTex_TexelSize;
SAMPLER(sampler_linear_clamp);

TEXTURE2D(_PostProcessNormalTex);
float4 _PostProcessNormalTex_TexelSize;
SAMPLER(sampler_PostProcessNormalTex);

TEXTURE2D(_PostProcessDepthTex);
SAMPLER(sampler_PostProcessDepthTex);


float4 SampleInputTex(float2 texcoord)
{
	return SAMPLE_TEXTURE2D(_PostProcessInputTex, sampler_linear_clamp, texcoord);
}
float4 InputTexelSize()
{
	return _PostProcessInputTex_TexelSize;
}
float4 SampleInputTexBicubic(float2 texcoord)
{
	return SampleTexture2DBicubic(TEXTURE2D_ARGS(_PostProcessInputTex, sampler_linear_clamp), texcoord, InputTexelSize().zwxy, 1.0, 0.0);
}


float3 SampleNormalTex(float2 texcoord)
{
	return UnpackNormalOctRectEncode(SAMPLE_TEXTURE2D(_PostProcessNormalTex, sampler_linear_clamp, texcoord).xy) * float3(1.0, 1.0, -1.0);
}

float SampleDepthTex(float2 texcoord)
{
	return SAMPLE_DEPTH_TEXTURE(_PostProcessDepthTex, sampler_PostProcessDepthTex, texcoord).r;
}
float SampleDepthTexLinear(float2 texcoord)
{
	return Linear01Depth(SampleDepthTex(texcoord));
}
float SampleDepthTexWorld(float2 texcoord)
{
	return SampleDepthTexLinear(texcoord) * _ProjectionParams.z;
}

float4 SampleCombineTex(float2 texcoord)
{
	return SAMPLE_TEXTURE2D(_PostProcessCombineTex, sampler_linear_clamp, texcoord);
}

float3 ReconstructWorldFromDepth(float2 texcoord)
{
	return _WorldSpaceCameraPos + CameraTraceDirection(texcoord) * SampleDepthTexWorld(texcoord);
}

#endif