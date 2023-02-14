#ifndef _GRP_POST_PROCESS_STACK_PASSES
#define _GRP_POST_PROCESS_STACK_PASSES

#include "BlitPass.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

TEXTURE2D(_PostProcessInputTex);
TEXTURE2D(_PostProcessCombineTex);
float4 _PostProcessInputTex_TexelSize;
SAMPLER(sampler_linear_clamp);

TEXTURE2D(_PostProcessNormalTex);
float4 _PostProcessNormalTex_TexelSize;
SAMPLER(sampler_PostProcessNormalTex);

TEXTURE2D(_PostProcessDepthTex);
SAMPLER(sampler_PostProcessDepthTex);


float4 SampleInputTex(float2 texcoord)
{
	return SAMPLE_TEXTURE2D(_PostProcessInputTex, sampler_linear_clamp, texcoord);
}
float4 InputTexelSize()
{
	return _PostProcessInputTex_TexelSize;
}
float4 SampleInputTexBicubic(float2 texcoord)
{
	return SampleTexture2DBicubic(TEXTURE2D_ARGS(_PostProcessInputTex, sampler_linear_clamp), texcoord, InputTexelSize().zwxy, 1.0, 0.0);
}


float3 SampleNormalTex(float2 texcoord)
{
	return UnpackNormalOctRectEncode(SAMPLE_TEXTURE2D(_PostProcessNormalTex, sampler_linear_clamp, texcoord).xy) * float3(1.0, 1.0, -1.0);
}

float SampleDepthTex(float2 texcoord)
{
	return SAMPLE_DEPTH_TEXTURE(_PostProcessDepthTex, sampler_PostProcessDepthTex, texcoord).r;
}
float SampleDepthTexLinear(float2 texcoord)
{
	return Linear01Depth(SampleDepthTex(texcoord));
}
float SampleDepthTexWorld(float2 texcoord)
{
	return SampleDepthTexLinear(texcoord) * _ProjectionParams.z;
}

float4 SampleCombineTex(float2 texcoord)
{
	return SAMPLE_TEXTURE2D(_PostProcessCombineTex, sampler_linear_clamp, texcoord);
}

float3 ReconstructWorldFromDepth(float2 texcoord)
{
	return _WorldSpaceCameraPos + CameraTraceDirection(texcoord) * SampleDepthTexWorld(texcoord);
}

////////////////////////////////
// COPY
////////////////////////////////

float4 CopyPassFragment(BlitVaryings input) : SV_TARGET
{
	return SampleInputTex(input.texcoord);
}



////////////////////////////////
// SIMPLE BLUR
////////////////////////////////

float4 BlurHorizontalPassFragment(BlitVaryings input) : SV_TARGET
{
	float3 color = 0;
	float offsets[] =
	{
		-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
	};
	float weights[] =
	{
		0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
		0.19459459, 0.12162162, 0.05405405, 0.01621622
	};
	for (int i = 0; i < 9; i++)
	{
		float offset = offsets[i] * 2 * InputTexelSize().x;
		color += SampleInputTex(input.texcoord + float2(offset, 0)).rgb * weights[i];
	}
	return float4(color, 1);
}

float4 BlurVerticalPassFragment(BlitVaryings input) : SV_TARGET
{
	float3 color = 0;
	float offsets[] =
	{
		-3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
	};
	float weights[] =
	{
		0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
	};
	for (int i = 0; i < 5; i++)
	{
		float offset = offsets[i] * InputTexelSize().y;
		color += SampleInputTex(input.texcoord + float2(0, offset)).rgb * weights[i];
	}
	return float4(color, 1);
}


////////////////////////////////
// BLOOM
////////////////////////////////

float4 _BloomThreshold;

float3 ApplyBloomThreshold(float3 color)
{
	float brightness = Max3(color.r, color.g, color.b);
	float soft = brightness + _BloomThreshold.y;
	soft = clamp(soft, 0.0, _BloomThreshold.z);
	soft = soft * soft * _BloomThreshold.w;
	float contribution = max(soft, brightness - _BloomThreshold.x);
	contribution /= max(brightness, 0.00001);
	return color * contribution;
}


float4 BloomPrefilterPassFragment(BlitVaryings input) : SV_TARGET
{
	float3 color = 0;
	float weightSum = 0;

	float2 offsets[] =
	{
		float2(0.0, 0.0), float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0)
	};
	for (int i = 0; i < 5; i++)
	{
		float3 c = SampleInputTex(input.texcoord + offsets[i] * InputTexelSize().xy * 2).rgb;
		c = ApplyBloomThreshold(c);
		float w = 1.0 / (Luminance(c) + 1.0);
		color += c * w;
		weightSum += w;
	}
	color /= weightSum;
	return float4(color, 1);
}


bool _BloomBicubicUpsampling;
float _BloomIntensity;

float4 BloomAddPassFragment(BlitVaryings input) : SV_TARGET
{
	float3 lowRes;
	if (_BloomBicubicUpsampling)
	{
		lowRes = SampleInputTexBicubic(input.texcoord).rgb;
	}
	else
	{
		lowRes = SampleInputTex(input.texcoord).rgb;
	}
	float4 highRes = SampleCombineTex(input.texcoord);
	return float4(lowRes * _BloomIntensity + highRes.rgb, highRes.a);
}

float4 BloomScatterPassFragment(BlitVaryings input) : SV_TARGET
{
	float3 lowRes;
	if (_BloomBicubicUpsampling)
	{
		lowRes = SampleInputTexBicubic(input.texcoord).rgb;
	}
	else 
	{
		lowRes = SampleInputTex(input.texcoord).rgb;
	}
	float3 highRes = SampleCombineTex(input.texcoord).rgb;
	return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0);
}

float4 BloomScatterFinalPassFragment(BlitVaryings input) : SV_TARGET
{
	float3 lowRes;
	if (_BloomBicubicUpsampling)
	{
		lowRes = SampleInputTexBicubic(input.texcoord).rgb;
	}
	else
	{
		lowRes = SampleInputTex(input.texcoord).rgb;
	}
	float4 highRes = SampleCombineTex(input.texcoord);
	lowRes += highRes.rgb - ApplyBloomThreshold(highRes.rgb);
	return float4(lerp(highRes.rgb, lowRes, _BloomIntensity), highRes.a);
}


////////////////////////////////
// AMBIENT OCCLUSION
////////////////////////////////

float4 _SSAOParameters;
#define SAMPLE_COUNT _SSAOParameters.x
#define RADIUS _SSAOParameters.y
#define INTENSITY _SSAOParameters.z

#define BUFFER_SIZE _PostProcessNormalTex_TexelSize.zw


float3x3 GetCoordinateConversionParameters(out float2 p11_22, out float2 p13_31)
{
	float3x3 camProj = (float3x3)unity_CameraProjection;

	p11_22 = rcp(float2(camProj._11, camProj._22));
	p13_31 = float2(camProj._13, camProj._23);

	return camProj;
}

float RawToLinearDepth(float rawDepth)
{
#if defined(_PROJECTION_ORTHOGRAPHIC)
	#if UNITY_REVERSED_Z
		return ((_ProjectionParams.z - _ProjectionParams.y) * (1.0 - rawDepth) + _ProjectionParams.y);
	#else
		return ((_ProjectionParams.z - _ProjectionParams.y) * (rawDepth)+_ProjectionParams.y);
	#endif
#else
	return LinearEyeDepth(rawDepth, _ZBufferParams);
#endif
}

float SampleAndGetLinearDepth(float2 texcoord)
{
	float rawDepth = SampleDepthTex(texcoord.xy);
	return RawToLinearDepth(rawDepth);
}

float3 ReconstructViewPos(float2 texcoord, float depth, float2 p11_22, float2 p13_31)
{
#if defined(_PROJECTION_ORTHOGRAPHIC)
	float3 viewPos = float3(((texcoord.xy * 2.0 - 1.0 - p13_31) * p11_22), depth);
#else
	float3 viewPos = float3(depth * ((texcoord.xy * 2.0 - 1.0 - p13_31) * p11_22), depth);
#endif
	return viewPos;
}

void SampleDepthNormalView(float2 texcoord, float2 p11_22, float2 p13_31, out float depth, out float3 normal, out float3 vpos)
{
	depth = SampleAndGetLinearDepth(texcoord);
	vpos = ReconstructViewPos(texcoord, depth, p11_22, p13_31);
	normal = SampleNormalTex(texcoord);
}

// Sample point picker
float2 CosSin(float theta)
{
	float sn, cs;
	sincos(theta, sn, cs);
	return float2(cs, sn);
}
float UVRandom(float u, float v)
{
	float f = dot(float2(12.9898, 78.233), float2(u, v));
	return frac(43758.5453 * sin(f));
}
float3 PickSamplePoint(float2 texcoord, int index)
{
	float2 positionSS = texcoord * BUFFER_SIZE;
	float gn = InterleavedGradientNoise(positionSS, index);
	float u = frac(UVRandom(0.0, index) + gn) * 2.0 - 1.0;
	float theta = (UVRandom(1.0, index) + gn) * TWO_PI;
	return float3(CosSin(theta) * sqrt(1.0 - u * u), u);
}


// Constants
// kContrast determines the contrast of occlusion. This allows users to control over/under
// occlusion. At the moment, this is not exposed to the editor because it's rarely useful.
static const float kContrast = 0.6;

// The constant below controls the geometry-awareness of the bilateral
// filter. The higher value, the more sensitive it is.
static const float kGeometryCoeff = 0.8;

// The constants below are used in the AO estimator. Beta is mainly used for suppressing
// self-shadowing noise, and Epsilon is used to prevent calculation underflow. See the
// paper (Morgan 2011 http://goo.gl/2iz3P) for further details of these constants.
static const float kBeta = 0.002;
#define EPSILON         1.0e-4


float4 SSAOPassFragment(BlitVaryings input) : SV_TARGET
{
	float2 texcoord = input.texcoord;

	// Parameters used in coordinate conversion
	float2 p11_22, p13_31;
	float3x3 camProj = GetCoordinateConversionParameters(p11_22, p13_31);

	// Get the depth, normal and view position for this fragment
	float depth_o;
	float3 norm_o;
	float3 vpos_o;
	SampleDepthNormalView(texcoord, p11_22, p13_31, depth_o, norm_o, vpos_o);

	float rcpSampleCount = rcp(SAMPLE_COUNT);
	float ao = 0.0;
	for (int s = 0; s < int(SAMPLE_COUNT); s++)
	{
		// Sample point
		float3 v_s1 = PickSamplePoint(texcoord, s);

		// Make it distributed between [0, _Radius]
		v_s1 *= sqrt((s + 1.0) * rcpSampleCount) * RADIUS;

		v_s1 = faceforward(v_s1, -norm_o, v_s1);
		float3 vpos_s1 = vpos_o + v_s1;

		// Reproject the sample point
		float3 spos_s1 = mul(camProj, vpos_s1);
#if defined(_PROJECTION_ORTHOGRAPHIC)
		float2 uv_s1_01 = clamp((spos_s1.xy + 1.0) * 0.5, 0.0, 1.0);
#else
		float2 uv_s1_01 = clamp((spos_s1.xy * rcp(vpos_s1.z) + 1.0) * 0.5, 0.0, 1.0);
#endif

		// Depth at the sample point
		float depth_s1 = SampleAndGetLinearDepth(uv_s1_01);

		// Relative position of the sample point
		float3 vpos_s2 = ReconstructViewPos(uv_s1_01, depth_s1, p11_22, p13_31);
		float3 v_s2 = vpos_s2 - vpos_o;

		// Estimate the obscurance value
		float a1 = max(dot(v_s2, norm_o) - kBeta * depth_o, 0.0);
		float a2 = dot(v_s2, v_s2) + EPSILON;
		ao += a1 * rcp(a2);
	}

	// Intensity normalization
	ao *= RADIUS;

	// Apply contrast
	ao = PositivePow(ao * INTENSITY * rcpSampleCount, kContrast);

	ao = 1 - ao;

	return float4(ao, 0, 0, 1);
}


float4 SSAOCombinePassFragment(BlitVaryings input) : SV_TARGET
{
	float4 color = SampleInputTex(input.texcoord);
	float ao = SampleCombineTex(input.texcoord).r;
	// Saturate the ao to prevent shading emissive materials
	return float4(color.rgb - saturate(color.rgb * (1 - ao)), color.a);
}


////////////////////////////////
// DEPTH OF FIELD
////////////////////////////////

float _DOFFocusDistance = 5;
float _DOFFocusRange = 10;
float _DOFBokehRadius = 4;

TEXTURE2D(_DOFCOCBuffer);
SAMPLER(sampler_DOFCOCBuffer);
TEXTURE2D(_DOFResult);
SAMPLER(sampler_DOFResult);

float SampleCOCBuffer(float2 texcoord)
{
	return SAMPLE_TEXTURE2D(_DOFCOCBuffer, sampler_DOFCOCBuffer, texcoord).r;
}

float4 SampleDOFResult(float2 texcoord)
{
	return SAMPLE_TEXTURE2D(_DOFResult, sampler_DOFResult, texcoord);
}


float DOFCalculateCOCPass(BlitVaryings input) : SV_TARGET
{
	float depth = SampleAndGetLinearDepth(input.texcoord);
	float coc = clamp((depth - _DOFFocusDistance) / _DOFFocusRange, -1, 1) * _DOFBokehRadius;
	return coc;
}

#if defined(BOKEH_KERNEL_SMALL)
static const int kSampleCount = 16;
static const float2 kDiskKernel[kSampleCount] =
{
	float2(0, 0),
	float2(0.54545456, 0),
	float2(0.16855472, 0.5187581),
	float2(-0.44128203, 0.3206101),
	float2(-0.44128197, -0.3206102),
	float2(0.1685548, -0.5187581),
	float2(1, 0),
	float2(0.809017, 0.58778524),
	float2(0.30901697, 0.95105654),
	float2(-0.30901703, 0.9510565),
	float2(-0.80901706, 0.5877852),
	float2(-1, 0),
	float2(-0.80901694, -0.58778536),
	float2(-0.30901664, -0.9510566),
	float2(0.30901712, -0.9510565),
	float2(0.80901694, -0.5877853),
};
#elif defined(BOKEH_KERNEL_MEDIUM)
static const int kSampleCount = 22;
static const float2 kDiskKernel[kSampleCount] =
{
	float2(0, 0),
	float2(0.53333336, 0),
	float2(0.3325279, 0.4169768),
	float2(-0.11867785, 0.5199616),
	float2(-0.48051673, 0.2314047),
	float2(-0.48051673, -0.23140468),
	float2(-0.11867763, -0.51996166),
	float2(0.33252785, -0.4169769),
	float2(1, 0),
	float2(0.90096885, 0.43388376),
	float2(0.6234898, 0.7818315),
	float2(0.22252098, 0.9749279),
	float2(-0.22252095, 0.9749279),
	float2(-0.62349, 0.7818314),
	float2(-0.90096885, 0.43388382),
	float2(-1, 0),
	float2(-0.90096885, -0.43388376),
	float2(-0.6234896, -0.7818316),
	float2(-0.22252055, -0.974928),
	float2(0.2225215, -0.9749278),
	float2(0.6234897, -0.7818316),
	float2(0.90096885, -0.43388376),
};
#else
static const int kSampleCount = 43;
static const float2 kDiskKernel[kSampleCount] = 
{
	float2(0,0),
	float2(0.36363637,0),
	float2(0.22672357,0.28430238),
	float2(-0.08091671,0.35451925),
	float2(-0.32762504,0.15777594),
	float2(-0.32762504,-0.15777591),
	float2(-0.08091656,-0.35451928),
	float2(0.22672352,-0.2843024),
	float2(0.6818182,0),
	float2(0.614297,0.29582983),
	float2(0.42510667,0.5330669),
	float2(0.15171885,0.6647236),
	float2(-0.15171883,0.6647236),
	float2(-0.4251068,0.53306687),
	float2(-0.614297,0.29582986),
	float2(-0.6818182,0),
	float2(-0.614297,-0.29582983),
	float2(-0.42510656,-0.53306705),
	float2(-0.15171856,-0.66472363),
	float2(0.1517192,-0.6647235),
	float2(0.4251066,-0.53306705),
	float2(0.614297,-0.29582983),
	float2(1,0),
	float2(0.9555728,0.2947552),
	float2(0.82623875,0.5633201),
	float2(0.6234898,0.7818315),
	float2(0.36534098,0.93087375),
	float2(0.07473,0.9972038),
	float2(-0.22252095,0.9749279),
	float2(-0.50000006,0.8660254),
	float2(-0.73305196,0.6801727),
	float2(-0.90096885,0.43388382),
	float2(-0.98883086,0.14904208),
	float2(-0.9888308,-0.14904249),
	float2(-0.90096885,-0.43388376),
	float2(-0.73305184,-0.6801728),
	float2(-0.4999999,-0.86602545),
	float2(-0.222521,-0.9749279),
	float2(0.07473029,-0.99720377),
	float2(0.36534148,-0.9308736),
	float2(0.6234897,-0.7818316),
	float2(0.8262388,-0.56332),
	float2(0.9555729,-0.29475483),
};
#endif

float Weigh(float coc, float radius)
{
	return saturate((coc - radius + 2) / 2);
}
float4 DOFCalculateBokehPass(BlitVaryings input) : SV_TARGET
{
	float coc = SampleCOCBuffer(input.texcoord);

	float3 bgColor = 0, fgColor = 0;
	float bgWeight = 0, fgWeight = 0;
	for (int k = 0; k < kSampleCount; k++)
	{
		float2 offset = kDiskKernel[k] * _DOFBokehRadius;
		float radius = length(offset);
		offset *= InputTexelSize().xy;
		float4 s = SampleInputTex(input.texcoord + offset);

		float bgw = Weigh(max(0, min(s.a, coc)), radius);
		bgColor += s.rgb * bgw;
		bgWeight += bgw;

		float fgw = Weigh(-s.a, radius);
		fgColor += s.rgb * fgw;
		fgWeight += fgw;
	}
	bgColor *= 1 / (bgWeight + (bgWeight == 0));
	fgColor *= 1 / (fgWeight + (fgWeight == 0));
	float bgfg = min(1, fgWeight * 3.14159265359 / kSampleCount);
	return float4(lerp(bgColor, fgColor, bgfg), bgfg);
}

#define DOF_USE_DOWNSAMPLE 1

float Weigh(float3 c)
{
	return 1 / (1 + max(max(c.r, c.g), c.b));
}
float4 DOFPreFilterPass(BlitVaryings input) : SV_TARGET
{
	//float3 s0 = SampleInputTex(input.texcoord + o.xy).rgb;
	//float3 s1 = SampleInputTex(input.texcoord + o.zy).rgb;
	//float3 s2 = SampleInputTex(input.texcoord + o.xw).rgb;
	//float3 s3 = SampleInputTex(input.texcoord + o.zw).rgb;
	//
	//float w0 = Weigh(s0);
	//float w1 = Weigh(s1);
	//float w2 = Weigh(s2);
	//float w3 = Weigh(s3);
	//
	//float3 color = s0 * w0 + s1 * w1 + s2 * w2 + s3 * w3;
	//color /= max(w0 + w1 + w2 + s3, 0.00001);
	float3 color = SampleInputTex(input.texcoord).rgb;

#if defined(DOF_USE_DOWNSAMPLE)
	float4 o = InputTexelSize().xyxy * float2(-0.5, 0.5).xxyy;
	float coc0 = SampleCOCBuffer(input.texcoord + o.xy).r;
	float coc1 = SampleCOCBuffer(input.texcoord + o.zy).r;
	float coc2 = SampleCOCBuffer(input.texcoord + o.xw).r;
	float coc3 = SampleCOCBuffer(input.texcoord + o.zw).r;

	float cocMin = min(min(min(coc0, coc1), coc2), coc3);
	float cocMax = max(max(max(coc0, coc1), coc2), coc3);
	float coc = cocMax >= -cocMin ? cocMax : cocMin;
	return float4(color, coc);
#else
	return float4(color, SampleCOCBuffer(input.texcoord));
#endif
}

float4 DOFPostFilterPass(BlitVaryings input) : SV_TARGET
{
#if defined(DOF_USE_DOWNSAMPLE)
	float4 o = InputTexelSize().xyxy * float2(-0.5, 0.5).xxyy;
	
	float4 s0 = SampleInputTex(input.texcoord + o.xy);
	float4 s1 = SampleInputTex(input.texcoord + o.zy);
	float4 s2 = SampleInputTex(input.texcoord + o.xw);
	float4 s3 = SampleInputTex(input.texcoord + o.zw);
	
	return (s0 + s1 + s2 + s3) * 0.25;
#else
	return SampleInputTex(input.texcoord);
#endif
}

float4 DOFCombinePass(BlitVaryings input) : SV_TARGET
{
	float4 src = SampleInputTex(input.texcoord);
	float coc = SampleCOCBuffer(input.texcoord);
	float4 dof = SampleDOFResult(input.texcoord);

	float dofStrength = smoothstep(0.1, 1, abs(coc));
	float3 color = lerp(src.rgb, dof.rgb, dofStrength + dof.a - dofStrength * dof.a);

	return float4(color, src.a);
}

////////////////////////////////
// Outline
////////////////////////////////

float4 _OutlineColor;
// x = normal intensity, y = normal bias, z = depth intensity, w = depth bias
float4 _OutlineParam;

void Compare(inout float depthOutline, inout float normalOutline, float baseDepth, float3 baseNormal, float2 uv, float2 offset)
{
	float3 neighborNormal = SampleNormalTex(uv + InputTexelSize().xy * offset);
	float neighborDepth = SampleDepthTexWorld(uv + InputTexelSize().xy * offset);

	depthOutline += baseDepth - neighborDepth;

	float3 normalDifference = baseNormal - neighborNormal;
	normalOutline += (normalDifference.r + normalDifference.g + normalDifference.b);
}

float4 OutlinePass(BlitVaryings input) : SV_TARGET
{
	float3 normal = SampleNormalTex(input.texcoord);
	float depth = SampleDepthTexWorld(input.texcoord);

	float depthDifference = 0;
	float normalDifference = 0;

	Compare(depthDifference, normalDifference, depth, normal, input.texcoord, float2(1, 0));
	Compare(depthDifference, normalDifference, depth, normal, input.texcoord, float2(0, 1));
	Compare(depthDifference, normalDifference, depth, normal, input.texcoord, float2(0, -1));
	Compare(depthDifference, normalDifference, depth, normal, input.texcoord, float2(-1, 0));

	depthDifference = pow(saturate(depthDifference * _OutlineParam.z), _OutlineParam.w);
	normalDifference = pow(saturate(normalDifference * _OutlineParam.x), _OutlineParam.y);

	float outline = saturate(normalDifference + depthDifference) * _OutlineColor.a;
	float4 sourceColor = SampleInputTex(input.texcoord);
	return float4(lerp(sourceColor.rgb, _OutlineColor.rgb, outline), sourceColor.a);
}

////////////////////////////////
// COLOR GRADING
////////////////////////////////

float4 _ColorAdjustments;
float4 _ColorFilter;
float4 _WhiteBalance;
float4 _SplitToningShadows, _SplitToningHighlights;
float4 _ChannelMixerRed, _ChannelMixerGreen, _ChannelMixerBlue;
float4 _SMHShadows, _SMHMidtones, _SMHHighlights, _SMHRange;

float Luminance(float3 color, bool useACES)
{
	return useACES ? AcesLuminance(color) : Luminance(color);
}

float3 ColorGradePostExposure(float3 color)
{
	return color * _ColorAdjustments.x;
}

float3 ColorGradeWhiteBalance(float3 color)
{
	color = LinearToLMS(color);
	color *= _WhiteBalance.rgb;
	return LMSToLinear(color);
}

float3 ColorGradingContrast(float3 color, bool useACES)
{
	color = useACES ? ACES_to_ACEScc(unity_to_ACES(color)) : LinearToLogC(color);
	color = (color - ACEScc_MIDGRAY) * _ColorAdjustments.y + ACEScc_MIDGRAY;
	return useACES ? ACES_to_ACEScg(ACEScc_to_ACES(color)) : LogCToLinear(color);
}

float3 ColorGradeColorFilter(float3 color)
{
	return color * _ColorFilter.rgb;
}

float3 ColorGradingHueShift(float3 color)
{
	color = RgbToHsv(color);
	float hue = color.x + _ColorAdjustments.z;
	color.x = RotateHue(hue, 0.0, 1.0);
	return HsvToRgb(color);
}

float3 ColorGradingSaturation(float3 color, bool useACES)
{
	float luminance = Luminance(color, useACES);
	return (color - luminance) * _ColorAdjustments.w + luminance;
}

float3 ColorGradeSplitToning(float3 color, bool useACES)
{
	color = PositivePow(color, 1.0 / 2.2);
	float t = saturate(Luminance(saturate(color), useACES) + _SplitToningShadows.w);
	float3 shadows = lerp(0.5, _SplitToningShadows.rgb, 1.0 - t);
	float3 highlights = lerp(0.5, _SplitToningHighlights.rgb, t);
	color = SoftLight(color, shadows);
	color = SoftLight(color, highlights);
	return PositivePow(color, 2.2);
}

float3 ColorGradingChannelMixer(float3 color)
{
	return mul(
		float3x3(_ChannelMixerRed.rgb, _ChannelMixerGreen.rgb, _ChannelMixerBlue.rgb),
		color
	);
}

float3 ColorGradingShadowsMidtonesHighlights(float3 color, bool useACES)
{
	float luminance = Luminance(color, useACES);
	float shadowsWeight = 1.0 - smoothstep(_SMHRange.x, _SMHRange.y, luminance);
	float highlightsWeight = smoothstep(_SMHRange.z, _SMHRange.w, luminance);
	float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
	return
		color * _SMHShadows.rgb * shadowsWeight +
		color * _SMHMidtones.rgb * midtonesWeight +
		color * _SMHHighlights.rgb * highlightsWeight;
}

float3 ColorGrade(float3 color, bool useACES = false)
{
	color = ColorGradePostExposure(color);
	color = ColorGradeWhiteBalance(color);
	color = ColorGradingContrast(color, useACES);
	color = ColorGradeColorFilter(color);
	color = max(color, 0.0);
	color = ColorGradeSplitToning(color, useACES);
	color = ColorGradingChannelMixer(color);
	color = max(color, 0.0);
	color = ColorGradingShadowsMidtonesHighlights(color, useACES);
	color = ColorGradingHueShift(color);
	color = ColorGradingSaturation(color, useACES);
	return max(useACES ? ACEScg_to_ACES(color) : color, 0.0);
}

float4 _ColorGradingLUTParameters;
bool _ColorGradingLUTInLogC;

float3 GetColorGradedLUT(float2 texcoord, bool useACES = false)
{
	float3 color = GetLutStripValue(texcoord, _ColorGradingLUTParameters);
	return ColorGrade(_ColorGradingLUTInLogC ? LogCToLinear(color) : color, useACES);
}

float4 ColorGradingNonePassFragment(BlitVaryings input) : SV_TARGET
{
	float3 color = GetColorGradedLUT(input.texcoord);
	return float4(color, 1.0);
}

float4 ColorGradingACESPassFragment(BlitVaryings input) : SV_TARGET
{
	float3 color = GetColorGradedLUT(input.texcoord, true);
	color = AcesTonemap(color);
	return float4(color, 1.0);
}

float4 ColorGradingNeutralPassFragment(BlitVaryings input) : SV_TARGET
{
	float3 color = GetColorGradedLUT(input.texcoord);
	color = NeutralTonemap(color);
	return float4(color, 1.0);
}

float4 ColorGradingReinhardPassFragment(BlitVaryings input) : SV_TARGET
{
	float3 color = GetColorGradedLUT(input.texcoord);
	color /= color + 1.0;
	return float4(color, 1.0);
}


////////////////////////////////
// COLOR GRADING LUT
////////////////////////////////
TEXTURE2D(_ColorGradingLUT);
bool _ColorGradingEnabled;


float3 ApplyColorGradingLUT(float3 color)
{
	return ApplyLut2D(
		TEXTURE2D_ARGS(_ColorGradingLUT, sampler_linear_clamp),
		saturate(_ColorGradingLUTInLogC ? LinearToLogC(color) : color),
		_ColorGradingLUTParameters.xyz
	);
}

float4 _VignetteParam;

float3 ApplyVignette(float3 color, float2 texcoord)
{
	float2 coord = (texcoord - 0.5) * float2(_VignetteParam.y, 1) * 2;
	coord *= _VignetteParam.z;
	float tan2Angle = dot(coord, coord);
	float cos4Angle = pow2(rcp(tan2Angle + 1));
	return color * cos4Angle;
}

float4 FinalPassFragment(BlitVaryings input) : SV_TARGET
{
	float4 color = SampleInputTex(input.texcoord);
	if (_ColorGradingEnabled)
	{
		color.rgb = ApplyColorGradingLUT(color.rgb);
	}
	if (_VignetteParam.x > 0)
	{
		color.rgb = ApplyVignette(color.rgb, input.texcoord);
	}
	return color;
}

#endif