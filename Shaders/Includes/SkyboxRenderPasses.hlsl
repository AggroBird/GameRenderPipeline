#ifndef _GRP_SKYBOX_RENDER_PASSES
#define _GRP_SKYBOX_RENDER_PASSES

#include "BlitPass.hlsl"
#include "Environment.hlsl"
#include "Noise.hlsl"


float3 _SkyboxCubemapRenderForward;
float3 _SkyboxCubemapRenderUp;


// srcmip, seed, strength, blend
float4 _SkyboxCubemapBlurParam;
float _SkyboxCubemapBlurSrcMip;
float _SkyboxCubemapBlurSeed;
float _SkyboxCubemapBlurStrength;


float3 _CloudColorTop;
float3 _CloudColorBottom;

float3 _CloudSampleOffset;
float3 _CloudSampleScale;

// thickness, height, layer height, fade distance
float4 _CloudParam;

// edge accuracy, edge threshold, length max, step
float4 _CloudTraceParam;

float CalculateCloudFade(float3 samplePos)
{
	return saturate((((distance(samplePos, _WorldSpaceCameraPos) - (_CloudTraceParam.z - _CloudParam.w)) / _CloudParam.w)));
}


float3 TexcoordToCubeDir(float2 texcoord)
{
	float2 dir = float2(texcoord.x, 1 - texcoord.y) * 2 - 1;
	float3 right = cross(_SkyboxCubemapRenderUp, _SkyboxCubemapRenderForward);
	return normalize(_SkyboxCubemapRenderUp * dir.y + right * dir.x + _SkyboxCubemapRenderForward);
}

float4 RenderSkyboxCubemapDynamic(BlitVaryings input) : SV_TARGET
{
	float3 direction = TexcoordToCubeDir(input.texcoord);

	float3 skyboxColor = SampleSkyboxProcedural(direction, true);

	#if defined(_SKYBOX_CLOUDS_ON)
	{
		// Simple clouds
		float3 origin = _WorldSpaceCameraPos;
		origin.y = 0;
		float3 sampleOffset = _CloudSampleOffset * _Time.y;
		float3 sampleScale = _CloudSampleScale * 0.05;

		float t0 = ((_CloudParam.y + _CloudParam.z * 0.5) - origin.y) / direction.y;
		float entry = max(t0, 0);

		if (entry > 0)
		{
			float invThickness = 1 - _CloudParam.x;
			float3 coord = origin + direction * entry;
			float3 samplePos = (coord + sampleOffset) * sampleScale;
			float noise = saturate(GradientNoise3D(samplePos, false, 1) * 0.5 + 0.5);
			float p1 = (noise - invThickness);
			if (p1 > 0)
			{
				float c = saturate(p1 * 20);
				float3 cloudColor = (_CloudColorTop + _CloudColorBottom) * 0.5;
				float fade = CalculateCloudFade(coord);
				return float4(lerp(lerp(skyboxColor, cloudColor, c), skyboxColor, fade), 1);
			}
		}
	}
	#endif

	return float4(skyboxColor, 1);
}

// Src mip index
#define SRC_MIP _SkyboxCubemapBlurParam.x
// Extra random seed
#define RANDOM_OFFSET _SkyboxCubemapBlurParam.y
// Random vector radius
#define RANDOM_RADIUS _SkyboxCubemapBlurParam.z
// Dst blend weight
#define DST_BLEND _SkyboxCubemapBlurParam.w

#define ITER_COUNT 32

float2 RandomToUnitCircle(float2 random)
{
	random *= (1.0 / 0xFFFF);
	float angle = random.x * 2 * PI;
	float radius = RANDOM_RADIUS * sqrt(random.y);
	float x = radius * cos(angle);
	float y = radius * sin(angle);
	return float2(x, y);
}
float4 BlurSkyboxCubemap(BlitVaryings input) : SV_TARGET
{
	float3 dir = TexcoordToCubeDir(input.texcoord);
	float3 right = normalize(cross(dir, _SkyboxCubemapRenderForward));
	float3 up = normalize(cross(dir, right));

	float seed = input.texcoord.x * 997 + (input.texcoord.y + 643) * 463 + RANDOM_OFFSET;
	float3 result = float3(0, 0, 0);
	for (int i = 0; i < ITER_COUNT; i++)
	{
		float2 random = Rand3DPCG16(seed).xy;
		seed += random.x * 223;
		random = RandomToUnitCircle(random);
		result += SampleSkyboxCubemap(normalize(dir + up * random.y + right * random.x), SRC_MIP).rgb;
	}
	result /= ITER_COUNT;

	return float4(result, DST_BLEND);
}



float4 RenderSkyboxCubemapStatic(BlitVaryings input) : SV_TARGET
{
	float3 dir = TexcoordToCubeDir(input.texcoord);
	return SampleSkyboxCubemap(dir);
}



TEXTURE2D(_OpaqueDepthBuffer);
SAMPLER(sampler_OpaqueDepthBuffer);

#define DEBUG_TRACE_COUNT 0
#define DEGUG_TRACE_COUNT_MAX 32

float4 RenderSkyboxWorldDynamic(BlitVaryings input, out float outDepth : SV_Depth) : SV_TARGET
{
	float rawDepth = SAMPLE_DEPTH_TEXTURE(_OpaqueDepthBuffer, sampler_OpaqueDepthBuffer, input.texcoord);

	outDepth = rawDepth;

	float linearDepth = Linear01Depth(rawDepth);

	float3 traceDirection = CameraTraceDirection(input.texcoord);

	float3 direction = normalize(traceDirection);
	float3 skyboxColor = SampleSkyboxProcedural(direction);

	#if defined(_SKYBOX_CLOUDS_ON)
	{
		float traceToDir = length(traceDirection);
		float depthWorld = (linearDepth * _ProjectionParams.z) * traceToDir;

		float3 origin = _WorldSpaceCameraPos;

		float t0 = (_CloudParam.y - origin.y) / direction.y;
		float t1 = ((_CloudParam.y + _CloudParam.z) - origin.y) / direction.y;

		float entry = max(min(t0, t1), 0);
		float exit = min(min(max(t0, t1), _CloudTraceParam.z), depthWorld);
		if (exit > 0)
		{
			float3 coord = origin + direction * entry;
			float3 sampleOffset = _CloudSampleOffset * _Time.y;
			float3 sampleScale = _CloudSampleScale * 0.05;
			float invAccuracy = 1 - _CloudTraceParam.x;
			float invThickness = 1 - _CloudParam.x;

			float f1 = entry;
			float f0 = entry;
			float h0 = (origin.y - _CloudParam.y) / _CloudParam.z;
			float p0 = 0;

			#if DEBUG_TRACE_COUNT
			float iterCount = 0;
			#endif

			while (true)
			{
				#if DEBUG_TRACE_COUNT
				iterCount += 1;
				#endif

				float h1 = (coord.y - _CloudParam.y) / _CloudParam.z;
				float grad = pow(h1 * 2 - 1, 4);

				float3 samplePos = (coord + sampleOffset) * sampleScale;
				float noise = saturate(GradientNoise3D(samplePos, false, 1) * 0.5 + 0.5);
				float p1 = (noise - invThickness) - grad;
				if (p1 > 0)
				{
					#if DEBUG_TRACE_COUNT
					break;
					#endif

					p1 /= (abs(p0) + p1);
					h1 = lerp(h1, h0, p1);
					f1 = lerp(f1, f0, p1);

					outDepth = InverseLinear01Depth((f1 / traceToDir) / _ProjectionParams.z);
					float fade = CalculateCloudFade(coord);
					return float4(lerp(lerp(_CloudColorBottom, _CloudColorTop, h1), skyboxColor, fade), 1);
				}
				else if (f1 >= exit)
				{
					break;
				}

				p0 = p1;
				h0 = h1;
				f0 = f1;

				float accuracy = saturate(abs(p1) / _CloudTraceParam.y) * invAccuracy + _CloudTraceParam.x;
				float scaledStep = _CloudTraceParam.w * accuracy;
				scaledStep = min((exit - f1), scaledStep);

				coord += direction * scaledStep;
				f1 += scaledStep;
			}

			#if DEBUG_TRACE_COUNT
			return float4(iterCount / DEGUG_TRACE_COUNT_MAX, 0, 0, 1);
			#endif
		}
	}
	#endif

	return float4(skyboxColor, linearDepth == 1);
}



float4 RenderSkyboxWorldStatic(BlitVaryings input) : SV_TARGET
{
	return SampleSkyboxCubemap(normalize(CameraTraceDirection(input.texcoord)));
}


#endif