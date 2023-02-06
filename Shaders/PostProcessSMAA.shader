Shader "Hidden/GRP/PostProcessSMAA"
{
	Properties
	{

	}
	Subshader
	{
		Cull Off ZWrite Off ZTest Always

		// 0 - Edge detection (Low)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertEdge
            #pragma fragment FragEdge
            #define SMAA_PRESET_LOW
            #include "Includes/SMAAPass.hlsl"
            ENDHLSL
        }

        // 1 - Edge detection (Medium)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertEdge
            #pragma fragment FragEdge
            #define SMAA_PRESET_MEDIUM
            #include "Includes/SMAAPass.hlsl"
            ENDHLSL
        }

        // 2 - Edge detection (High)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertEdge
            #pragma fragment FragEdge
            #define SMAA_PRESET_HIGH
            #include "Includes/SMAAPass.hlsl"
            ENDHLSL
        }

        // 3 - Blend Weights Calculation (Low)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertBlend
            #pragma fragment FragBlend
            #define SMAA_PRESET_LOW
            #include "Includes/SMAAPass.hlsl"
            ENDHLSL
        }

        // 4 - Blend Weights Calculation (Medium)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertBlend
            #pragma fragment FragBlend
            #define SMAA_PRESET_MEDIUM
            #include "Includes/SMAAPass.hlsl"
            ENDHLSL
        }

        // 5 - Blend Weights Calculation (High)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertBlend
            #pragma fragment FragBlend
            #define SMAA_PRESET_HIGH
            #include "Includes/SMAAPass.hlsl"
            ENDHLSL
        }

        // 6 - Neighborhood Blending
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertNeighbor
            #pragma fragment FragNeighbor
            #include "Includes/SMAAPass.hlsl"
            ENDHLSL
        }
	}
}