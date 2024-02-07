using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class Lighting
    {
        private const string bufferName = "Lighting";
        private readonly CommandBuffer buffer = new() { name = bufferName };

        private const int
            MaxDirectionalLightCount = 4,
            MaxOtherLightCount = 64;

        private static readonly int
            directionalLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
            directionalLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
            directionalLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
            directionalLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

        private readonly static Vector4[]
            directionalLightColors = new Vector4[MaxDirectionalLightCount],
            directionalLightDirections = new Vector4[MaxDirectionalLightCount],
            directionalLightShadowData = new Vector4[MaxDirectionalLightCount];

        private static readonly int
            otherLightCountId = Shader.PropertyToID("_OtherLightCount"),
            otherLightColorsId = Shader.PropertyToID("_OtherLightColors"),
            otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions"),
            otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections"),
            otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles"),
            otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");

        private readonly static Vector4[]
            otherLightColors = new Vector4[MaxOtherLightCount],
            otherLightPositions = new Vector4[MaxOtherLightCount],
            otherLightDirections = new Vector4[MaxOtherLightCount],
            otherLightSpotAngles = new Vector4[MaxOtherLightCount],
            otherLightShadowData = new Vector4[MaxOtherLightCount];

        internal static readonly GlobalKeyword lightsPerObjectKeyword = GlobalKeyword.Create("_LIGHTS_PER_OBJECT");

        private static readonly int
            hatchingDarkId = Shader.PropertyToID("_Hatching_Dark"),
            hatchingBrightId = Shader.PropertyToID("_Hatching_Bright"),
            hatchingScaleId = Shader.PropertyToID("_Hatching_Scale"),
            hatchingIntensityId = Shader.PropertyToID("_Hatching_Intensity");

        internal static readonly GlobalKeyword cellShadingKeyword = GlobalKeyword.Create("_CELL_SHADING_ENABLED");

        private static readonly int cellShadingFalloffId = Shader.PropertyToID("_CellShading_Falloff");

        private Texture2D cellShadingFalloffTexture = default;


        private CullingResults cullingResults;

        private readonly Shadows shadows = new();

        public Vector3 PrimaryLightDirection { get; private set; }
        public Color PrimaryLightColor { get; private set; }


        public void Setup(Camera camera, ScriptableRenderContext context, CullingResults cullingResults, GameRenderPipelineSettings settings)
        {
            this.cullingResults = cullingResults;

            buffer.BeginSample(bufferName);
            shadows.Setup(context, cullingResults, settings.shadows);
            SetupLights(settings.general.useLightsPerObject);
            SetupCellShading(settings.experimental.cellShading);
            context.SetupCameraProperties(camera);
            shadows.Render();
            buffer.EndSample(bufferName);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public void Cleanup()
        {
            shadows.Cleanup();
        }

        private void SetupLights(bool useLightsPerObject)
        {
            int directionalLightCount = 0, otherLightCount = 0;
            NativeArray<int> indexMap = useLightsPerObject ? cullingResults.GetLightIndexMap(Allocator.Temp) : default;
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            int i = 0;
            for (; i < visibleLights.Length; i++)
            {
                int newIndex = -1;
                VisibleLight visibleLight = visibleLights[i];

                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                        if (directionalLightCount < MaxDirectionalLightCount)
                        {
                            SetupDirectionalLight(directionalLightCount++, i, ref visibleLight);
                        }
                        break;
                    case LightType.Point:
                        if (otherLightCount < MaxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            SetupPointLight(otherLightCount++, i, ref visibleLight);
                        }
                        break;
                    case LightType.Spot:
                        if (otherLightCount < MaxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            SetupSpotLight(otherLightCount++, i, ref visibleLight);
                        }
                        break;
                }
                if (useLightsPerObject)
                {
                    indexMap[i] = newIndex;
                }
            }

            buffer.SetKeyword(lightsPerObjectKeyword, useLightsPerObject);
            if (useLightsPerObject)
            {
                for (; i < indexMap.Length; i++)
                {
                    indexMap[i] = -1;
                }
                cullingResults.SetLightIndexMap(indexMap);
                indexMap.Dispose();
            }


            buffer.SetGlobalInt(directionalLightCountId, directionalLightCount);
            if (directionalLightCount > 0)
            {
                buffer.SetGlobalVectorArray(directionalLightColorsId, directionalLightColors);
                buffer.SetGlobalVectorArray(directionalLightDirectionsId, directionalLightDirections);
                buffer.SetGlobalVectorArray(directionalLightShadowDataId, directionalLightShadowData);
            }

            buffer.SetGlobalInt(otherLightCountId, otherLightCount);
            if (otherLightCount > 0)
            {
                buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
                buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
                buffer.SetGlobalVectorArray(otherLightDirectionsId, otherLightDirections);
                buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
                buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
            }
        }


        private void SetupCellShading(ExperimentalSettings.CellShading settings)
        {
            buffer.SetKeyword(cellShadingKeyword, settings.enabled);
            if (settings.enabled)
            {
                TextureUtility.RenderGradientToTexture(ref cellShadingFalloffTexture, settings.falloff);
                buffer.SetGlobalTexture(cellShadingFalloffId, cellShadingFalloffTexture);
            }
        }

        private void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight)
        {
            directionalLightColors[index] = visibleLight.finalColor;
            directionalLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
            directionalLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, visibleIndex);

            if (index == 0)
            {
                PrimaryLightDirection = directionalLightDirections[index];
                PrimaryLightColor = visibleLight.finalColor;
            }
        }

        private void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight)
        {
            otherLightColors[index] = visibleLight.finalColor;
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            otherLightPositions[index] = position;
            otherLightSpotAngles[index] = new Vector4(0f, 1f);
            otherLightShadowData[index] = shadows.ReserveOtherShadows(visibleLight.light, visibleIndex);
        }

        private void SetupSpotLight(int index, int visibleIndex, ref VisibleLight visibleLight)
        {
            otherLightColors[index] = visibleLight.finalColor;
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            otherLightPositions[index] = position;
            otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

            Light light = visibleLight.light;
            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
            otherLightShadowData[index] = shadows.ReserveOtherShadows(visibleLight.light, visibleIndex);
        }
    }
}