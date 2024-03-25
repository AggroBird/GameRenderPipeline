#ifndef _GRP_UNLIT_PASS
#define _GRP_UNLIT_PASS

struct Attributes
{
	float4 positionOS : POSITION;
	float2 texcoord : TEXCOORD0;
    half4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 texcoord : TEXCOORD0;
    half4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
	Varyings output;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	VertexPositions vertexPositions = GetVertexPositions(input.positionOS);
	output.positionCS = vertexPositions.positionCS;

	output.texcoord = TransformTexcoord(input.texcoord, PER_MATERIAL_PROP(_MainTex_ST));

	output.color = input.color;

	return output;
}

half4 UnlitPassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	
    ClipLOD(input.positionCS.xy, unity_LODFade.x);

	InputConfig config = GetInputConfig(input.texcoord);

    half4 diffuse = GetDiffuse(config) * input.color;
	diffuse.rgb += GetEmission(config);
	ClipAlpha(diffuse.a, config);

    return half4(diffuse.rgb, GetFinalAlpha(diffuse.a));
}


#endif