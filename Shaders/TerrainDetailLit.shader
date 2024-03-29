﻿Shader "Hidden/GRP/TerrainDetailLit"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	Subshader
	{
		HLSLINCLUDE
		#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/Common.hlsl"
		ENDHLSL

		Pass
		{
			HLSLPROGRAM
			#pragma target 3.5

			#pragma vertex Vert
			#pragma fragment Frag

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			float4 _MainTex_ST;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 texcoord : TEXCOORD0;
				half3 normalOS : NORMAL;
				half4 color : COLOR;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 texcoord : TEXCOORD;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;

				VertexPositions vertexPositions = GetVertexPositions(input.positionOS);
				output.positionCS = vertexPositions.positionCS;

				output.texcoord = input.texcoord;

				return output;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TransformTexcoord(input.texcoord, _MainTex_ST));
			}

			ENDHLSL
		}
	}
	CustomEditor "AggroBird.GameRenderPipeline.Editor.CustomShaderGUI"
}