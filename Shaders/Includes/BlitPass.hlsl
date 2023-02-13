#ifndef _GRP_BLIT_PASS
#define _GRP_BLIT_PASS

#include "Common.hlsl"

struct BlitVaryings
{
	float4 positionCS : SV_POSITION;
	float2 texcoord : TEXCOORD0;
	float3 viewSpaceDir : TEXCOORD1;
};

void SetupBlitVertex(uint vertexID, out float4 positionCS, out float2 texcoord)
{
	positionCS = float4(
		vertexID <= 1 ? -1.0 : 3.0,
		vertexID == 1 ? 3.0 : -1.0,
		0.0, 1.0
		);
	texcoord = float2(
		vertexID <= 1 ? 0.0 : 2.0,
		vertexID == 1 ? 2.0 : 0.0
		);
	if (_ProjectionParams.x < 0.0)
	{
		texcoord.y = 1.0 - texcoord.y;
	}
}
void SetupBlitVertex(uint vertexID, out float4 positionCS, out float2 texcoord, out float3 viewSpaceDir)
{
	SetupBlitVertex(vertexID, positionCS, texcoord);
	viewSpaceDir = mul(unity_CameraInvProjection, positionCS).xyz;
}

BlitVaryings BlitVertex(uint vertexID : SV_VertexID)
{
	BlitVaryings output;
	SetupBlitVertex(vertexID, output.positionCS, output.texcoord, output.viewSpaceDir);
	return output;
}

#endif