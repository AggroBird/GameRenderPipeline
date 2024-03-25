#ifndef _GRP_TERRAIN_GRASS_PASS
#define _GRP_TERRAIN_GRASS_PASS

#include "Lighting.hlsl"

// Grass: Vertex attribute usage
// color        - .xyz = color, .w = wave scale
// normal       - normal
// tangent.xy   - billboard extrusion
// texcoord     - UV coords

struct Attributes
{
	float4 positionOS : POSITION;
	float2 texcoord : TEXCOORD0;
    half3 normalOS : NORMAL;
	half4 tangentOS : TANGENT;
	half4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float3 positionWS : TEXCOORD0;
    half3 normalWS : NORMAL;
	float2 texcoord : TEXCOORD1;
    half4 color : COLOR;
	FOG_ATTRIBUTE(2)
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

void TerrainBillboardGrass(inout float3 pos, float2 offset)
{
	float3 grasspos = pos.xyz - _CameraPosition.xyz;
	if (dot(grasspos, grasspos) > _WaveAndDistance.w)
		offset = 0.0;
	pos.xyz += offset.x * _CameraRight.xyz;
	pos.y += offset.y;
}

Varyings GrassVertex(Attributes input)
{
	Varyings output;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);

#if defined(GRASS_BILLBOARD)
	TerrainBillboardGrass(input.positionOS.xyz, input.tangentOS.xy);
#endif

	VertexPositions vertexPositions = GetVertexPositions(input.positionOS);
	output.positionCS = vertexPositions.positionCS;
	output.positionWS = vertexPositions.positionWS;

	output.normalWS = TransformObjectToWorldNormal(input.normalOS);

	output.texcoord = input.texcoord;

	output.color = input.color;
#if defined(_COLOR_SPACE_LINEAR)
	output.color = float4(SRGBToLinear(input.color.rgb), input.color.a);
#else
	output.color = input.color;
#endif
	
	float3 offset = input.positionOS.xyz - _CameraPosition.xyz;
	output.color.a = saturate(2 * (_WaveAndDistance.w - dot(offset, offset)) * _CameraPosition.w);

	TRANSFER_FOG(output, vertexPositions);

	return output;
}

FragmentOutput GrassFragment(Varyings input)
{
	UNITY_SETUP_INSTANCE_ID(input);

    half4 diffuse = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);

	diffuse.a *= input.color.a;
	AlphaDiscard(diffuse.a, _Cutoff);
	diffuse.rgb *= input.color.rgb;

	half metallic = 0;
	half smoothness = 0;
    half fresnel = 0;
	Surface surface = MakeSurface(diffuse, input.positionWS, input.normalWS, metallic, smoothness, fresnel, input.positionCS.xy);

	BRDF brdf = GetBRDF(surface);
	GlobalIllumination gi = GetGlobalIllumination(surface, brdf);
    half3 lit = GRP_LIGHT_GET_TOTAL_FUNC(surface, brdf, gi);

	APPLY_FOG(input, lit);

    return MakeFragmentOutput(half4(lit, surface.alpha), surface.normal);
}

#endif