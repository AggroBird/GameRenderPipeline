#ifndef _GRP_SMAA_PASS
#define _GRP_SMAA_PASS

#include "BlitPass.hlsl"
		
#define SMAA_HLSL_4_1 1

TEXTURE2D(_MainTex);
TEXTURE2D(_SMAA_BlendTex);
TEXTURE2D(_SMAA_AreaTex);
TEXTURE2D(_SMAA_SearchTex);

SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);

float4 _SMAA_RTMetrics;

#define SMAA_RT_METRICS _SMAA_RTMetrics
#define SMAA_AREATEX_SELECT(s) s.rg
#define SMAA_SEARCHTEX_SELECT(s) s.a
#define LinearSampler sampler_linear_clamp
#define PointSampler sampler_point_clamp

#include "SMAA.hlsl"

// ----------------------------------------------------------------------------------------
// Edge Detection

struct VaryingsEdge
{
	float4 vertex : SV_POSITION;
	float2 texcoord : TEXCOORD0;
	float4 offsets[3] : TEXCOORD1;
};

VaryingsEdge VertEdge(uint vertexID : SV_VertexID)
{
	VaryingsEdge o;
	SetupBlitVertex(vertexID, o.vertex, o.texcoord);

/*#if UNITY_UV_STARTS_AT_TOP
	o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif*/

	SMAAEdgeDetectionVS(o.texcoord, o.offsets);

	return o;
}

float4 FragEdge(VaryingsEdge i) : SV_Target
{
	return float4(SMAAColorEdgeDetectionPS(i.texcoord, i.offsets, _MainTex), 0.0, 0.0);
}

// ----------------------------------------------------------------------------------------
// Blend Weights Calculation

struct VaryingsBlend
{
	float4 vertex : SV_POSITION;
	float2 texcoord : TEXCOORD0;
	float2 pixcoord : TEXCOORD1;
	float4 offsets[3] : TEXCOORD2;
};

VaryingsBlend VertBlend(uint vertexID : SV_VertexID)
{
	VaryingsBlend o;
	SetupBlitVertex(vertexID, o.vertex, o.texcoord);

/*#if UNITY_UV_STARTS_AT_TOP
	o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif*/

	SMAABlendingWeightCalculationVS(o.texcoord, o.pixcoord, o.offsets);

	return o;
}

float4 FragBlend(VaryingsBlend i) : SV_Target
{
	return SMAABlendingWeightCalculationPS(i.texcoord, i.pixcoord, i.offsets, _MainTex, _SMAA_AreaTex, _SMAA_SearchTex, 0);
}

// ----------------------------------------------------------------------------------------
// Neighborhood Blending

struct VaryingsNeighbor
{
	float4 vertex : SV_POSITION;
	float2 texcoord : TEXCOORD0;
	float4 offset : TEXCOORD1;
};

VaryingsNeighbor VertNeighbor(uint vertexID : SV_VertexID)
{
	VaryingsNeighbor o;
	SetupBlitVertex(vertexID, o.vertex, o.texcoord);

/*#if UNITY_UV_STARTS_AT_TOP
	o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif*/

	SMAANeighborhoodBlendingVS(o.texcoord, o.offset);

	return o;
}

float4 FragNeighbor(VaryingsNeighbor i) : SV_Target
{
	return SMAANeighborhoodBlendingPS(i.texcoord, i.offset, _MainTex, _SMAA_BlendTex);
}

#endif