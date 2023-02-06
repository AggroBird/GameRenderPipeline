Shader "GRP/TreeLit"
{
	Properties
	{
		_MainTex("Diffuse Texture", 2D) = "white" {}
		_Color("Diffuse Color", Color) = (1, 1, 1, 1)
		[Space]
		[NoScaleOffset] _EmissionTex("Emission Texture", 2D) = "black" {}
		[HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)
		[Space]
		[NoScaleOffset] _NormalTex("Normal Texture", 2D) = "normal" {}
		_NormalScale("Normal Scale", Range(0, 1)) = 1
		[Space]
		_Metallic("Metallic", Range(0, 1)) = 0
		_Smoothness("Smoothness", Range(0, 1)) = 0.5
		_Fresnel("Fresnel", Range(0, 1)) = 1
		[Space]
		[Toggle(_ALPHATEST_ON)] _Clipping("Alpha Clipping", Float) = 0
		_Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
		[Space]
		[KeywordEnum(On, Clip, Dither, Off)] _Shadows("Cast Shadows", Float) = 0
		[Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows("Receive Shadows", Float) = 1
		[Space]
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Z Test", Float) = 4
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2

		[HideInInspector] _TreeInstanceColor("TreeInstanceColor", Vector) = (1,1,1,1)
		[HideInInspector] _TreeInstanceScale("TreeInstanceScale", Vector) = (1,1,1,1)
		[HideInInspector] _SquashAmount("Squash", Float) = 1
	}
	Subshader
	{
		HLSLINCLUDE
		#pragma multi_compile_instancing
		#pragma multi_compile _ LOD_FADE_CROSSFADE
		#define IS_TREE_MATERIAL
		#include "Includes/LitInput.hlsl"
		#include "Includes/LitPass.hlsl"
		ENDHLSL
		
		UsePass "GRP/Lit/LitPass"

		UsePass "GRP/Lit/ShadowPass"
	}
	Dependency "BillboardShader" = "Diffuse"
	CustomEditor "AggroBird.GRP.Editor.CustomShaderGUI"
}