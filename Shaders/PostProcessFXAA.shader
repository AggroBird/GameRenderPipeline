Shader "Hidden/GRP/PostProcessFXAA"
{
	Properties
	{

	}
	Subshader
	{
		Cull Off ZWrite Off ZTest Always

        // 0 - Low quality
        Pass
        {
            HLSLPROGRAM
            #pragma vertex BlitVertex
            #pragma fragment FXAAFrag
            #define FXAA_QUALITY__PRESET 15
            #include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/FXAAPass.hlsl"
            ENDHLSL
        }
        
        // 1 - Medium quality
        Pass
        {
            HLSLPROGRAM
            #pragma vertex BlitVertex
            #pragma fragment FXAAFrag
            #define FXAA_QUALITY__PRESET 29
            #include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/FXAAPass.hlsl"
            ENDHLSL
        }
        
        // 2 - High quality
        Pass
        {
            HLSLPROGRAM
            #pragma vertex BlitVertex
            #pragma fragment FXAAFrag
            #define FXAA_QUALITY__PRESET 39
            #include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/FXAAPass.hlsl"
            ENDHLSL
        }
	}
}