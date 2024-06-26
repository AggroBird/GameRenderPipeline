#ifndef _GRP_LIT_PASS
#define _GRP_LIT_PASS

#include "Lighting.hlsl"

struct Attributes
{
	float4 positionOS : POSITION;
    half3 normalOS : NORMAL;
    half4 tangentOS : TANGENT;
	float2 texcoord : TEXCOORD0;
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
#if defined(_HAS_NORMAL_TEXTURE)
	half4 tangentWS : TANGENT;
#endif
	FOG_ATTRIBUTE(2)
#if defined(_INCLUDE_POSITION_OS)
	float4 positionOS : TEXCOORD3;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input)
{
	Varyings output;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	
    VertexPositions vertexPositions = GetVertexPositions(input.positionOS);
#if defined(_INCLUDE_POSITION_OS)
	output.positionOS = input.positionOS;
#endif
	output.positionWS = vertexPositions.positionWS;
	output.positionCS = vertexPositions.positionCS;

	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_HAS_NORMAL_TEXTURE)
	output.tangentWS = half4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif

	output.texcoord = input.texcoord;

	output.color = input.color;
	
	TRANSFER_FOG(output, vertexPositions);

	return output;
}

FragmentOutput LitPassFragment(Varyings input)
{
	UNITY_SETUP_INSTANCE_ID(input);

	ClipLOD(input.positionCS.xy, unity_LODFade.x);

	InputConfig config = GetInputConfig(input.texcoord);

    half4 diffuse = GetDiffuse(config) * input.color;
	ClipAlpha(diffuse.a, config);

	SurfaceInfo surfaceInfo = GetSurfaceInfo(config);
	Surface surface = MakeSurface(diffuse, input.positionWS, input.normalWS, surfaceInfo.metallic, surfaceInfo.smoothness, surfaceInfo.fresnel, input.positionCS.xy);
    surface.shadowStrength *= PER_MATERIAL_PROP(_IndividualShadowStrength);

#if defined(_HAS_NORMAL_TEXTURE)
	surface.normal = NormalTangentToWorld(GetNormal(config), input.normalWS, input.tangentWS);
#endif

	BRDF brdf = GetBRDF(surface);
	GlobalIllumination gi = GetGlobalIllumination(surface, brdf);
    half3 lit = GRP_LIGHT_GET_TOTAL_FUNC(surface, brdf, gi) + GetEmission(config);
	
	APPLY_FOG(input, lit);

    half4 result = half4(lit, GetFinalAlpha(surface.alpha));

	return MakeFragmentOutput(result, surface.normal);
}


#endif