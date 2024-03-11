using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal readonly struct PrimaryDirectionalLightInfo
    {
        public PrimaryDirectionalLightInfo(Vector3 direction, Color color)
        {
            this.direction = direction;
            this.color = color;
        }

        public readonly Vector3 direction;
        public readonly Color color;
    }

    internal sealed class LightingPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(LightingPass));

        private Camera camera;
        private GameRenderPipelineSettings settings;

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

        private static readonly int globalShadowStrengthId = Shader.PropertyToID("_GlobalShadowStrength");

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
        private bool useLightsPerObject;
        private int directionalLightCount;
        private int otherLightCount;
        private ExperimentalSettings.CellShading cellShadingSettings;
        private ShowFlags showFlags;
        private float globalShadowStrength;

        private readonly Shadows shadows = new();

        private Vector3 primaryLightDirection;
        private Color primaryLightColor;


        public void Setup(CullingResults cullingResults, GameRenderPipelineSettings settings, ShowFlags showFlags, out PrimaryDirectionalLightInfo primaryDirectionalLightInfo)
        {
            this.cullingResults = cullingResults;
            useLightsPerObject = settings.general.useLightsPerObject;
            cellShadingSettings = settings.experimental.cellShading;
            this.showFlags = showFlags;
            globalShadowStrength = settings.shadows.globalShadowStrength;

            shadows.Setup(cullingResults, settings.shadows);
            SetupLights();

            primaryDirectionalLightInfo = new(primaryLightDirection, primaryLightColor);
        }

        private void Render(RenderGraphContext context)
        {
            CommandBuffer buffer = context.cmd;

            buffer.SetGlobalInt(directionalLightCountId, directionalLightCount);
            buffer.SetGlobalInt(otherLightCountId, otherLightCount);
            if (directionalLightCount > 0)
            {
                buffer.SetGlobalVectorArray(directionalLightColorsId, directionalLightColors);
                buffer.SetGlobalVectorArray(directionalLightDirectionsId, directionalLightDirections);
                buffer.SetGlobalVectorArray(directionalLightShadowDataId, directionalLightShadowData);
            }
            if (otherLightCount > 0)
            {
                buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
                buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
                buffer.SetGlobalVectorArray(otherLightDirectionsId, otherLightDirections);
                buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
                buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
            }

            bool useCellShading = cellShadingSettings.enabled && (showFlags & ShowFlags.PostProcess) != ShowFlags.None;
            buffer.SetKeyword(cellShadingKeyword, useCellShading);
            if (useCellShading)
            {
                TextureUtility.RenderGradientToTexture(ref cellShadingFalloffTexture, cellShadingSettings.falloff);
                buffer.SetGlobalTexture(cellShadingFalloffId, cellShadingFalloffTexture);
            }

            buffer.SetGlobalFloat(globalShadowStrengthId, globalShadowStrength);

            context.renderContext.SetupCameraProperties(camera);
            shadows.Render(context);
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        private void SetupLights()
        {
            directionalLightCount = 0;
            otherLightCount = 0;
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

            if (useLightsPerObject)
            {
                for (; i < indexMap.Length; i++)
                {
                    indexMap[i] = -1;
                }
                cullingResults.SetLightIndexMap(indexMap);
                indexMap.Dispose();
            }
        }


        private void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight)
        {
            directionalLightColors[index] = visibleLight.finalColor;
            directionalLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
            directionalLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, visibleIndex);

            if (index == 0)
            {
                primaryLightDirection = directionalLightDirections[index];
                primaryLightColor = visibleLight.finalColor;
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

        public static ShadowTextures Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults, GameRenderPipelineSettings settings, ShowFlags showFlags, out PrimaryDirectionalLightInfo primaryDirectionalLightInfo)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out LightingPass pass, sampler);
            pass.camera = camera;
            pass.Setup(cullingResults, settings, showFlags, out primaryDirectionalLightInfo);
            pass.settings = settings;
            builder.SetRenderFunc<LightingPass>(static (pass, context) => pass.Render(context));
            builder.AllowPassCulling(false);
            return pass.shadows.GetRenderTextures(renderGraph, builder);
        }
    }
}