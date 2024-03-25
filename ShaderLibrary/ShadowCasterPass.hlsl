#ifndef _GRP_SHADOW_CASTER_PASS
#define _GRP_SHADOW_CASTER_PASS

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

bool _ShadowPancaking;

Varyings ShadowCasterPassVertex(Attributes input)
{
	Varyings output;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	
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

	output.texcoord = TransformTexcoord(input.texcoord, PER_MATERIAL_PROP(_MainTex_ST));

	output.color = input.color;

	return output;
}

void ShadowCasterPassFragment(Varyings input)
{
	UNITY_SETUP_INSTANCE_ID(input);

	ClipLOD(input.positionCS.xy, unity_LODFade.x);

	InputConfig config = GetInputConfig(input.texcoord);
	
    half alpha = GetAlpha(config) * input.color.a;

#if defined(_SHADOWS_CLIP)
	clip(alpha - GetCutoff(config));
#elif defined(_SHADOWS_DITHER)
	half dither = InterleavedGradientNoise(input.positionCS.xy, 0);
	clip(alpha - dither);
#endif
}

#endif