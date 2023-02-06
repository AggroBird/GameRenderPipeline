#ifndef _GRP_UNLIT_PASS
#define _GRP_UNLIT_PASS

struct Attributes
{
	float3 positionOS : POSITION;
	float2 texcoord : TEXCOORD0;
	float4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 texcoord : TEXCOORD0;
	float4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
	Varyings output;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	VertexPositions vertexPositions = GetVertexPositions(input.positionOS);
	output.positionCS = vertexPositions.positionCS;

	output.texcoord = TransformTexcoord(input.texcoord, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST));

	output.color = input.color;

	return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);

	InputConfig config = GetInputConfig(input.texcoord);

	float4 diffuse = GetDiffuse(config) * input.color;
	diffuse.rgb += GetEmission(config);
	ClipAlpha(diffuse.a, config);

	return float4(diffuse.rgb, GetFinalAlpha(diffuse.a));
}


#endif