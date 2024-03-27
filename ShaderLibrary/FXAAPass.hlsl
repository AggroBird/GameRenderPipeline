#ifndef _GRP_FXAA_PASS
#define _GRP_FXAA_PASS

#include "BlitPass.hlsl"

#define FXAA_PC 1
#define FXAA_HLSL_5 1

#ifndef FXAA_360_OPT
#define FXAA_360_OPT 0
#endif

float4 fxaaConsolePosPos;
float4 fxaaConsoleRcpFrameOpt;
float4 fxaaConsoleRcpFrameOpt2;
float4 fxaaConsole360RcpFrameOpt2;
float fxaaQualitySubpix;
float fxaaQualityEdgeThreshold;
float fxaaQualityEdgeThresholdMin;
float fxaaConsoleEdgeSharpness;
float fxaaConsoleEdgeThreshold;
float fxaaConsoleEdgeThresholdMin;
float4 fxaaConsole360ConstDir;

#include "FXAA_311.hlsl"

float2 _FXAA_InverseScreenSize;

float4 FXAAFrag(BlitVaryings i) : SV_Target
{
    FxaaTex TextureAndSampler;
    TextureAndSampler.tex = _PostProcessInputTex;
    TextureAndSampler.smpl = sampler_linear_clamp;
    TextureAndSampler.UVMinMax = float4(0, 0, 1, 1);
    
    return FxaaPixelShader(
		i.texcoord,
        fxaaConsolePosPos,
		TextureAndSampler,
		TextureAndSampler,
		TextureAndSampler,
		_FXAA_InverseScreenSize,
		fxaaConsoleRcpFrameOpt,
		fxaaConsoleRcpFrameOpt2,
		fxaaConsole360RcpFrameOpt2,
		fxaaQualitySubpix,
		fxaaQualityEdgeThreshold,
		fxaaQualityEdgeThresholdMin,
		fxaaConsoleEdgeSharpness,
		fxaaConsoleEdgeThreshold,
		fxaaConsoleEdgeThresholdMin,
		fxaaConsole360ConstDir);
}

#endif