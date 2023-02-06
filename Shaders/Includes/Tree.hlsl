#ifndef _GRP_TREE
#define _GRP_TREE

#if defined(_TREE_MATERIAL)

UNITY_INSTANCING_BUFFER_START(UnityTerrain)
UNITY_DEFINE_INSTANCED_PROP(float4, _TreeInstanceColor)
UNITY_DEFINE_INSTANCED_PROP(float4, _TreeInstanceScale)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _TerrainEngineBendTree)
UNITY_DEFINE_INSTANCED_PROP(float4, _SquashPlaneNormal)
UNITY_DEFINE_INSTANCED_PROP(float, _SquashAmount)
UNITY_INSTANCING_BUFFER_END(UnityTerrain)

void ApplyTreeProperties(inout float4 positionOS)
{
	positionOS.xyz *= UNITY_ACCESS_INSTANCED_PROP(UnityTerrain, _TreeInstanceScale).xyz;
	//float3 bent = mul(UNITY_ACCESS_INSTANCED_PROP(UnityTerrain, _TerrainEngineBendTree), float4(positionOS.xyz, 0.0)).xyz;
	//positionOS.xyz = lerp(positionOS.xyz, bent, input.color.w);

	float4 squashPlaneNormal = UNITY_ACCESS_INSTANCED_PROP(UnityTerrain, _SquashPlaneNormal);
	float3 planeNormal = squashPlaneNormal.xyz;
	float3 projectedVertex = positionOS.xyz - (dot(planeNormal.xyz, positionOS.xyz) + squashPlaneNormal.w) * planeNormal;
	positionOS = float4(lerp(projectedVertex, positionOS.xyz, UNITY_ACCESS_INSTANCED_PROP(UnityTerrain, _SquashAmount)), 1);
}

void ApplyTreeInstanceColor(inout float4 color)
{
	color.xyz *= UNITY_ACCESS_INSTANCED_PROP(UnityTerrain, _TreeInstanceColor).xyz;
}

#endif

#endif