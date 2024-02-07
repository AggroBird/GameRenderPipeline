Shader "Hidden/GRP/Template"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)
	}
	Subshader
	{
		HLSLINCLUDE
		#include "Packages/com.aggrobird.gamerenderpipeline/ShaderLibrary/Common.hlsl"
		ENDHLSL

		Tags
		{
			"Queue" = "Geometry"
		}

		Pass
		{
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex Vertex
			#pragma fragment Fragment
			#pragma multi_compile_instancing

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
				UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
			UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
			
			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings Vertex(Attributes input)
			{
				Varyings output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				VertexPositions vertexPositions = GetVertexPositions(input.positionOS);
				output.positionCS = vertexPositions.positionCS;

				output.texcoord = TransformTexcoord(input.texcoord, PER_MATERIAL_PROP(_MainTex_ST));

				return output;
			}

			float4 Fragment(Varyings input) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);

				float4 textureColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);
				float4 color = PER_MATERIAL_PROP(_Color);

				return textureColor * color;
			}

			ENDHLSL
		}
	}
}