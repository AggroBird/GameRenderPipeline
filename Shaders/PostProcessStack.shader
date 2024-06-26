﻿Shader "Hidden/GRP/PostProcessStack"
{
	Properties
	{
		
	}
	Subshader
	{
		HLSLINCLUDE
		#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/PostProcessPass.hlsl"
		ENDHLSL

		Cull Off
		ZTest Always
		ZWrite Off

		Pass
		{
			Name "Copy"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment CopyPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Blur Horizontal"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment BlurHorizontalPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Blur Vertical"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment BlurVerticalPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Bloom Prefilter"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment BloomPrefilterPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Bloom Add"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment BloomAddPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Bloom Scatter"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment BloomScatterPassFragment
			ENDHLSL
		}
		
		Pass
		{
			Name "Bloom Scatter Final"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment BloomScatterFinalPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Ambient Occlusion"

			HLSLPROGRAM
			#pragma multi_compile _ _PROJECTION_ORTHOGRAPHIC
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment SSAOPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Ambient Occlusion Combine"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment SSAOCombinePassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Depth of Field Calculate COC"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment DOFCalculateCOCPass
			ENDHLSL
		}

		Pass
		{
			Name "Depth of Field Calculate Bokeh"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment DOFCalculateBokehPass
			ENDHLSL
		}

		Pass
		{
			Name "Depth of Field Pre Filter"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment DOFPreFilterPass
			ENDHLSL
		}

		Pass
		{
			Name "Depth of Field Post Filter"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment DOFPostFilterPass
			ENDHLSL
		}

		Pass
		{
			Name "Depth of Field Combine"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment DOFCombinePass
			ENDHLSL
		}

		Pass
		{
			Name "Outline"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment OutlinePass
			ENDHLSL
		}

		Pass
		{
			Name "Color Grading None"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment ColorGradingNonePassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Color Grading ACES"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment ColorGradingACESPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Color Grading Neutral"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment ColorGradingNeutralPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Color Grading Reinhard"

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment ColorGradingReinhardPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Apply Color Grading"

			Blend [_FinalSrcBlend] [_FinalDstBlend]

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment ApplyColorGradingPassFragment
			ENDHLSL
		}

        Pass
        {
			Name "FXAA Low quality"

			Blend [_FinalSrcBlend] [_FinalDstBlend]

            HLSLPROGRAM
            #pragma vertex BlitVertex
            #pragma fragment FXAAFrag
            #define FXAA_QUALITY__PRESET 15
            #include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/FXAAPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
			Name "FXAA Medium quality"

			Blend [_FinalSrcBlend] [_FinalDstBlend]

            HLSLPROGRAM
            #pragma vertex BlitVertex
            #pragma fragment FXAAFrag
            #define FXAA_QUALITY__PRESET 29
            #include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/FXAAPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
			Name "FXAA High quality"

			Blend [_FinalSrcBlend] [_FinalDstBlend]

            HLSLPROGRAM
            #pragma vertex BlitVertex
            #pragma fragment FXAAFrag
            #define FXAA_QUALITY__PRESET 39
            #include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/FXAAPass.hlsl"
            ENDHLSL
        }

		Pass
		{
			Name "Final Rescale"

			Blend [_FinalSrcBlend] [_FinalDstBlend]

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex BlitVertex
			#pragma fragment FinalRescalePassFragment
			ENDHLSL
		}
	}
}