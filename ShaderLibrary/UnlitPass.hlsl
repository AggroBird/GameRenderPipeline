#ifndef _GRP_UNLIT_PASS
#define _GRP_UNLIT_PASS

struct Attributes
{
    float4 positionOS : POSITION;
    half3 normalOS : NORMAL;
	float2 texcoord : TEXCOORD0;
    half4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    half3 normalWS : NORMAL;
    float2 texcoord : TEXCOORD0;
    half4 color : COLOR;
#if defined(_INCLUDE_POSITION_OS)
	float4 positionOS : TEXCOORD1;
#endif
#if defined(_INCLUDE_POSITION_WS)
	float3 positionWS : TEXCOORD2;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
	Varyings output;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);

    VertexPositions vertexPositions = GetVertexPositions(input.positionOS);
#if defined(_INCLUDE_POSITION_OS)
	output.positionOS = input.positionOS;
#endif
#if defined(_INCLUDE_POSITION_WS)
	output.positionWS = vertexPositions.positionWS;
#endif
	output.positionCS = vertexPositions.positionCS;
	
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	
	output.texcoord = TransformTexcoord(input.texcoord, PER_MATERIAL_PROP(_MainTex_ST));

	output.color = input.color;

	return output;
}

FragmentOutput UnlitPassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	
    ClipLOD(input.positionCS.xy, unity_LODFade.x);

	InputConfig config = GetInputConfig(input.texcoord);

    half4 diffuse = GetDiffuse(config) * input.color;
	diffuse.rgb += GetEmission(config);
	ClipAlpha(diffuse.a, config);
	
    half4 result = half4(diffuse.rgb, GetFinalAlpha(diffuse.a));

    return MakeFragmentOutput(result, input.normalWS);
}


#endif