#ifndef _GRP_LIT_PASS
#define _GRP_LIT_PASS

#include "Lighting.hlsl"
#include "Tree.hlsl"

struct Attributes
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float2 texcoord : TEXCOORD0;
	float4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float3 positionWS : TEXCOORD0;
	float3 normalWS : NORMAL;
	float2 texcoord : TEXCOORD1;
	float4 color : COLOR;
#if defined(_HAS_NORMAL_TEXTURE)
	float4 tangentWS : TANGENT;
#endif
	FOG_ATTRIBUTE(2)
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input)
{
	Varyings output;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);

#if defined(_TREE_MATERIAL)
	ApplyTreeProperties(input.positionOS);
#endif

	VertexPositions vertexPositions = GetVertexPositions(input.positionOS);
	output.positionWS = vertexPositions.positionWS;
	output.positionCS = vertexPositions.positionCS;

	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_HAS_NORMAL_TEXTURE)
	output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif

	output.texcoord = input.texcoord;

	output.color = input.color;

#if defined(_TREE_MATERIAL)
	ApplyTreeInstanceColor(output.color);
#endif

	TRANSFER_FOG(output, vertexPositions);

	return output;
}

FragmentOutput LitPassFragment(Varyings input)
{
	UNITY_SETUP_INSTANCE_ID(input);

	ClipLOD(input.positionCS.xy, unity_LODFade.x);

	InputConfig config = GetInputConfig(input.texcoord);

	float4 diffuse = GetDiffuse(config) * input.color;
	ClipAlpha(diffuse.a, config);

	float metallic = GetMetallic(config);
	float smoothness = GetSmoothness(config);
	float fresnel = GetFresnel(config);
	Surface surface = MakeSurface(diffuse, input.positionWS, input.normalWS, metallic, smoothness, fresnel, input.positionCS.xy);

#if defined(_HAS_NORMAL_TEXTURE)
	surface.normal = NormalTangentToWorld(GetNormal(config), input.normalWS, input.tangentWS);
#endif

	BRDF brdf = GetBRDF(surface);
	GlobalIllumination gi = GetGlobalIllumination(surface, brdf);
	float3 lit = GRP_LIGHT_GET_TOTAL_FUNC(surface, brdf, gi) + GetEmission(config);

#if defined(_HATCHING_ENABLED)
	ApplyHatching(lit, diffuse.rgb, input.positionWS, input.texcoord);
#endif

	APPLY_FOG(input, lit);

	float4 result = float4(lit, GetFinalAlpha(surface.alpha));

	return MakeFragmentOutput(result, surface.normal);
}


#endif