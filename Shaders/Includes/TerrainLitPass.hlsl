#ifndef _GRP_TERRAIN_LIT_PASS
#define _GRP_TERRAIN_LIT_PASS

#include "Lighting.hlsl"

struct Attributes
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float2 texcoord : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float3 positionWS : TEXCOORD0;
	float3 normalWS : NORMAL;
	float2 texcoord : TEXCOORD1;
	float4 texSplat01 : TEXCOORD2;
	float4 texSplat23 : TEXCOORD3;
	FOG_ATTRIBUTE(4)
};


void TerrainInstancing(inout float4 positionOS, inout float3 normal, inout float2 uv)
{
#if defined(UNITY_INSTANCING_ENABLED)
	float2 patchVertex = positionOS.xy;
	float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

	float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
	float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

	positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
	positionOS.y = height * _TerrainHeightmapScale.y;

#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
	normal = float3(0, 1, 0);
#else
	normal = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
#endif

	uv = sampleCoords * _TerrainHeightmapRecipSize.zw;
#endif
}


Varyings TerrainLitPassVertex(Attributes input)
{
	Varyings output;

	UNITY_SETUP_INSTANCE_ID(input);
	TerrainInstancing(input.positionOS, input.normalOS, input.texcoord);

	VertexPositions vertexPositions = GetVertexPositions(input.positionOS);
	output.positionWS = vertexPositions.positionWS;
	output.positionCS = vertexPositions.positionCS;

	output.normalWS = TransformObjectToWorldNormal(input.normalOS);

	output.texcoord = input.texcoord;

	output.texSplat01.xy = TransformTexcoord(input.texcoord, _Splat0_ST);
	output.texSplat01.zw = TransformTexcoord(input.texcoord, _Splat1_ST);
	output.texSplat23.xy = TransformTexcoord(input.texcoord, _Splat2_ST);
	output.texSplat23.zw = TransformTexcoord(input.texcoord, _Splat3_ST);

	TRANSFER_FOG(output, vertexPositions);

	return output;
}

FragmentOutput TerrainLitPassFragment(Varyings input)
{
	ClipHoles(input.texcoord);

	InputConfig config = GetTerrainInputConfig(input.texcoord, input.texSplat01, input.texSplat23);

	SplatmapMix(config);

	float4 diffuse = GetDiffuse(config);
	float metallic = GetMetallic(config);
	float smoothness = GetSmoothness(config);
	float fresnel = 1;
	Surface surface = MakeSurface(diffuse, input.positionWS, input.normalWS, metallic, smoothness, fresnel, input.positionCS.xy);

	BRDF brdf = GetBRDF(surface);
	GlobalIllumination gi = GetGlobalIllumination(surface, brdf);
	float3 lit = GetTotalLighting(surface, brdf, gi);

#if defined(_HATCHING_ENABLED)
	ApplyHatching(lit, diffuse.rgb, input.positionWS, input.positionWS.xz);
#endif

	// Terrain additive
	lit *= diffuse.a;

#if defined(TERRAIN_SPLAT_ADDPASS)
	BLEND_FOG(input, lit.rgb);
#else
	APPLY_FOG(input, lit.rgb);
#endif

	return MakeFragmentOutput(float4(lit, 1), surface.normal);
}


struct AttributesLean
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float2 texcoord : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsLean
{
	float4 positionCS : SV_POSITION;
	float2 texcoord : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

bool _ShadowPancaking;

VaryingsLean TerrainShadowCasterPassVertex(AttributesLean input)
{
	VaryingsLean output;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	TerrainInstancing(input.positionOS, input.normalOS, input.texcoord);

	VertexPositions vertexPositions = GetVertexPositions(input.positionOS);
	output.positionCS = vertexPositions.positionCS;

	if (_ShadowPancaking)
	{
#if UNITY_REVERSED_Z
		output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
		output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif
	}

	output.texcoord = input.texcoord;

	return output;
}

void TerrainShadowCasterPassFragment(VaryingsLean input)
{
	ClipHoles(input.texcoord);
}


VaryingsLean DepthOnlyVertex(AttributesLean input)
{
	VaryingsLean output;

    UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	TerrainInstancing(input.positionOS, input.normalOS, input.texcoord);

	VertexPositions vertexPositions = GetVertexPositions(input.positionOS);
	output.positionCS = vertexPositions.positionCS;

	output.texcoord = input.texcoord;

    return output;
}

half4 DepthOnlyFragment(VaryingsLean input) : SV_TARGET
{
	ClipHoles(input.texcoord);
#ifdef SCENESELECTIONPASS
    return half4(_ObjectId, _PassValue, 1.0, 1.0);
#endif
    return 0;
}


#endif