using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed partial class CameraRenderer
    {
        private GameRenderPipelineAsset pipelineAsset;
        private ScriptableRenderContext context;
        private Camera camera;

        private readonly Lighting lighting = new();

        private readonly CommandBuffer buffer = new();
        private CullingResults cullingResults;

        private readonly PostProcessStack postProcessStack = new();

        private bool useHDR = false;
        internal RenderTextureFormat RenderTextureFormat => useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;


        private static readonly ShaderTagId[] defaultShaderTags = { new("SRPDefaultUnlit"), new("GRPLit") };

        private static readonly GlobalKeyword orthographicKeyword = GlobalKeyword.Create("_PROJECTION_ORTHOGRAPHIC");

        private static readonly GlobalKeyword colorSpaceLinearKeyword = GlobalKeyword.Create("_COLOR_SPACE_LINEAR");

        private static readonly GlobalKeyword outputNormalsKeyword = GlobalKeyword.Create("_OUTPUT_NORMALS_ENABLED");


        internal static readonly GlobalKeyword[] fogModeKeywords =
        {
            GlobalKeyword.Create("_FOG_LINEAR"),
            GlobalKeyword.Create("_FOG_EXP"),
            GlobalKeyword.Create("_FOG_EXP2"),
        };


        private static readonly int
            primaryLightDirectionId = Shader.PropertyToID("_PrimaryLightDirection"),
            primaryLightColorId = Shader.PropertyToID("_PrimaryLightColor");

        private static readonly int
            fogAmbientColorId = Shader.PropertyToID("_FogAmbientColor"),
            fogInscatteringColorId = Shader.PropertyToID("_FogInscatteringColor"),
            fogLightDirectionId = Shader.PropertyToID("_FogLightDirection"),
            fogParamId = Shader.PropertyToID("_FogParam");

        private static readonly int
            rtColorBufferId = Shader.PropertyToID("_RTColorBuffer"),
            rtDepthBufferId = Shader.PropertyToID("_RTDepthBuffer"),
            rtNormalBufferId = Shader.PropertyToID("_RTNormalBuffer"),
            opaqueColorBufferId = Shader.PropertyToID("_OpaqueColorBuffer"),
            opaqueDepthBufferId = Shader.PropertyToID("_OpaqueDepthBuffer"),
            ambientLightColorId = Shader.PropertyToID("_AmbientLightColor");

        private static readonly int
            skyboxStaticCubemapId = Shader.PropertyToID("_SkyboxStaticCubemap"),
            skyboxGradientTextureId = Shader.PropertyToID("_SkyboxGradientTexture"),
            skyboxGroundColorId = Shader.PropertyToID("_SkyboxGroundColor"),
            skyboxAnimTimeId = Shader.PropertyToID("_SkyboxAnimTime");

        private Material defaultSkyboxMaterial = null;

        private bool outputNormals;
        private bool outputOpaque;

        private float skyboxAnimTimeOffset;

        private readonly EnvironmentSettings defaultEnvironmentSettings = new();


        public CameraRenderer()
        {
            skyboxAnimTimeOffset = Random.Range(0f, 1000f);
        }

        public void Render(ScriptableRenderContext context, Camera camera, int cameraIndex, GameRenderPipelineAsset pipelineAsset)
        {
            this.pipelineAsset = pipelineAsset;
            this.context = context;
            this.camera = camera;

            PrepareBuffer();
            PrepareSceneWindow();
            if (!Cull(pipelineAsset.Settings.shadows.maxDistance))
            {
                return;
            }

            useHDR = pipelineAsset.Settings.general.allowHDR && camera.allowHDR;

            // Light and shadows
            buffer.BeginSample(BufferName);
            ExecuteBuffer();
            lighting.Setup(context, cullingResults, pipelineAsset.Settings);
            postProcessStack.Setup(context, camera, useHDR, ShowPostProcess);
            buffer.EndSample(BufferName);

            outputNormals = postProcessStack.SSAOEnabled || postProcessStack.OutlineEnabled;
            outputOpaque = pipelineAsset.Settings.general.outputOpaqueRenderTargets;

            buffer.SetKeyword(colorSpaceLinearKeyword, GameRenderPipeline.LinearColorSpace);
            buffer.SetKeyword(orthographicKeyword, camera.orthographic);

            // Render
            Setup();
            {
                buffer.SetKeyword(outputNormalsKeyword, outputNormals);
                RestoreDefaultRenderTargets();
                ExecuteBuffer();

                GetEnvironmentSettings(out EnvironmentSettings environmentSettings);

                // Opaque
                DrawVisibleGeometry(pipelineAsset.Settings.general, defaultShaderTags, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);
                DrawUnsupportedShaders();

                // Skybox
                if (camera.clearFlags == CameraClearFlags.Skybox)
                {
                    using (CommandBufferScope buffer = new("Render Skybox"))
                    {
                        switch (environmentSettings.skyboxSettings.skyboxSource)
                        {
                            case EnvironmentSettings.SkyboxSource.Material:
                            case EnvironmentSettings.SkyboxSource.Gradient:
                                bool useDefault = environmentSettings.skyboxSettings.skyboxSource == EnvironmentSettings.SkyboxSource.Gradient || !environmentSettings.skyboxSettings.skyboxMaterial;
                                Material mat = useDefault ? defaultSkyboxMaterial : environmentSettings.skyboxSettings.skyboxMaterial;
                                buffer.commandBuffer.DrawFullscreenEffect(mat, 0);
                                break;
                            case EnvironmentSettings.SkyboxSource.Cubemap:
                                buffer.commandBuffer.SetGlobalTexture(skyboxStaticCubemapId, environmentSettings.skyboxSettings.skyboxCubemap);
                                buffer.commandBuffer.DrawFullscreenEffect(defaultSkyboxMaterial, 1);
                                break;
                        }
                        context.ExecuteCommandBuffer(buffer);
                    }
                }

                DrawEditorGizmosPreImageEffects();

                // Post process
                postProcessStack.ApplyPreTransparency(rtColorBufferId, rtNormalBufferId, rtDepthBufferId, rtColorBufferId);

                // Copy render targets into opaque buffers
                if (outputOpaque)
                {
                    using (CommandBufferScope buffer = new("Copy Opaque Render Buffer"))
                    {
                        buffer.commandBuffer.BlitFrameBuffer(rtColorBufferId, rtDepthBufferId, opaqueColorBufferId, opaqueDepthBufferId);
                        buffer.commandBuffer.SetGlobalTexture(opaqueColorBufferId, opaqueColorBufferId);
                        buffer.commandBuffer.SetGlobalTexture(opaqueDepthBufferId, opaqueDepthBufferId);
                        context.ExecuteCommandBuffer(buffer);
                    }
                    RestoreDefaultRenderTargets();
                    ExecuteBuffer();
                }

                // Transparent
                DrawVisibleGeometry(pipelineAsset.Settings.general, defaultShaderTags, SortingCriteria.CommonTransparent, RenderQueueRange.transparent);

                // Post process
                postProcessStack.ApplyPostTransparency(rtColorBufferId, BuiltinRenderTextureType.CameraTarget);

                // Draw gizmos
                DrawEditorGizmosPostImageEffects();
            }
            Cleanup();
            Submit();
        }

        private void RestoreDefaultRenderTargets()
        {
            if (outputNormals)
            {
                buffer.SetRenderTarget(new RenderTargetIdentifier[] { rtColorBufferId, rtNormalBufferId }, rtDepthBufferId);
            }
            else
            {
                buffer.SetRenderTarget(
                    rtColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare,
                    rtDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            }
        }

        private bool TryGetEnvironmentComponent(Camera camera, out EnvironmentComponent environmentComponent)
        {
            if (camera.TryGetComponent(out environmentComponent) && environmentComponent.enabled)
            {
                return true;
            }
            else
            {
                if (camera.cameraType == CameraType.SceneView)
                {
#if UNITY_EDITOR
                    // Try to get current main camera component
                    var activeCameraComponents = EnvironmentCameraComponent.activeCameraComponents;
                    for (int i = 0; i < activeCameraComponents.Count;)
                    {
                        if (!activeCameraComponents[i])
                        {
                            int last = activeCameraComponents.Count - 1;
                            if (i == last)
                            {
                                activeCameraComponents.RemoveAt(i);
                            }
                            else
                            {
                                activeCameraComponents[i] = activeCameraComponents[last];
                                activeCameraComponents.RemoveAt(last);
                            }
                            continue;
                        }

                        if (activeCameraComponents[i].enabled && activeCameraComponents[i].TryGetComponent(out Camera cameraComponent) && cameraComponent.CompareTag(Tags.MainCameraTag))
                        {
                            environmentComponent = activeCameraComponents[i];
                            return true;
                        }

                        i++;
                    }
#endif
                }
            }

            environmentComponent = EnvironmentComponent.activeSceneEnvironment;
            return environmentComponent && environmentComponent.enabled;
        }
        private void GetEnvironmentSettings(out EnvironmentSettings environmentSettings)
        {
            if (TryGetEnvironmentComponent(camera, out EnvironmentComponent environmentComponent))
            {
                var settings = environmentComponent.GetEnvironmentSettings();
                if (settings != null)
                {
                    environmentSettings = settings;
                    SetupEnvironment(environmentSettings, environmentComponent.modified);
                    environmentComponent.modified = false;
                    return;
                }
            }

            environmentSettings = defaultEnvironmentSettings;
            SetupEnvironment(environmentSettings, false);
        }

        private bool Cull(float maxShadowDistance)
        {
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
            {
                scriptableCullingParameters.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
                cullingResults = context.Cull(ref scriptableCullingParameters);
                return true;
            }
            return false;
        }

        private void SetupEnvironment(EnvironmentSettings settings, bool environmentModified)
        {
            settings.UpdateEnvironment();

            buffer.SetGlobalVector(primaryLightDirectionId, lighting.PrimaryLightDirection);
            buffer.SetGlobalVector(primaryLightColorId, lighting.PrimaryLightColor);

            // Fog
            EnvironmentSettings.FogSettings fogSettings = settings.fogSettings;
            bool fogEnabled = fogSettings.enabled && ShowFog;
            int setFogKeyword = fogEnabled ? (int)fogSettings.fogMode : 0;
            buffer.SetKeywords(fogModeKeywords, setFogKeyword - 1);
            if (fogEnabled)
            {
                Color ambientColor = fogSettings.ambientColor.ColorSpaceAdjusted();
                buffer.SetGlobalVector(fogAmbientColorId, new(ambientColor.r, ambientColor.g, ambientColor.b, fogSettings.blend));
                buffer.SetGlobalVector(fogInscatteringColorId, fogSettings.inscatteringColor.ColorSpaceAdjusted());
                if (fogSettings.overrideLightDirection)
                    buffer.SetGlobalVector(fogLightDirectionId, fogSettings.lightDirection);
                else
                    buffer.SetGlobalVector(fogLightDirectionId, lighting.PrimaryLightDirection);

                Vector4 fogParam = Vector4.zero;
                switch (fogSettings.fogMode)
                {
                    case EnvironmentSettings.FogMode.Linear:
                        float linearFogStart = fogSettings.linearStart;
                        float linearFogEnd = fogSettings.linearEnd;
                        float linearFogRange = linearFogEnd - linearFogStart;
                        fogParam.x = -1.0f / linearFogRange;
                        fogParam.y = linearFogEnd / linearFogRange;
                        break;
                    default:
                        float expFogDensity = fogSettings.density;
                        fogParam.x = expFogDensity;
                        break;
                }

                buffer.SetGlobalVector(fogParamId, fogParam);
            }

            // Skybox
            if (!defaultSkyboxMaterial)
            {
                defaultSkyboxMaterial = new(pipelineAsset.Resources.skyboxRenderShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            buffer.SetGlobalTexture(skyboxGradientTextureId, settings.SkyboxGradientTexture);
            buffer.SetGlobalVector(skyboxGroundColorId, settings.skyboxSettings.groundColor.ColorSpaceAdjusted());

            buffer.SetGlobalFloat(skyboxAnimTimeId, Time.time + skyboxAnimTimeOffset);

#if UNITY_EDITOR
            if (environmentModified)
            {
                foreach (var reflectionProbe in Object.FindObjectsOfType<ReflectionProbe>())
                {
                    if (reflectionProbe.mode == ReflectionProbeMode.Realtime)
                    {
                        reflectionProbe.RenderProbe();
                    }
                }
            }
#endif

            buffer.SetGlobalVector(ambientLightColorId, settings.skyboxSettings.ambientColor.ColorSpaceAdjusted());
            ExecuteBuffer();
        }

        private void Setup()
        {
            context.SetupCameraProperties(camera);
            CameraClearFlags clearFlags = (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.SceneView) ? CameraClearFlags.SolidColor : camera.clearFlags;
            bool clearDepth = clearFlags <= CameraClearFlags.Depth;
            bool clearColor = clearFlags == CameraClearFlags.Color;
            Color backgroundColor = clearFlags == CameraClearFlags.Color ? camera.backgroundColor.ColorSpaceAdjusted() : Color.clear;

            // Render targets
            buffer.GetTemporaryRT(rtColorBufferId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat);
            buffer.GetTemporaryRT(rtDepthBufferId, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
            buffer.SetRenderTarget(
                rtColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                rtDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(clearDepth, clearColor, backgroundColor);

            if (outputNormals)
            {
                // Normal buffer
                buffer.GetTemporaryRT(rtNormalBufferId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.RGHalf);
                buffer.SetRenderTarget(rtNormalBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                buffer.ClearRenderTarget(true, true, Color.clear);
            }

            // Opaque render target
            if (outputOpaque)
            {
                buffer.GetTemporaryRT(opaqueColorBufferId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat);
                buffer.GetTemporaryRT(opaqueDepthBufferId, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                buffer.SetRenderTarget(
                    opaqueColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    opaqueDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                buffer.ClearRenderTarget(true, true, Color.clear);
            }

            buffer.BeginSample(BufferName);
            ExecuteBuffer();
        }
        private void Cleanup()
        {
            lighting.Cleanup();
            postProcessStack.Cleanup();

            buffer.ReleaseTemporaryRT(rtColorBufferId);
            buffer.ReleaseTemporaryRT(rtDepthBufferId);
            if (outputNormals)
            {
                buffer.ReleaseTemporaryRT(rtNormalBufferId);
            }
            if (outputOpaque)
            {
                buffer.ReleaseTemporaryRT(opaqueColorBufferId);
                buffer.ReleaseTemporaryRT(opaqueDepthBufferId);
            }
        }

        private void Submit()
        {
            buffer.EndSample(BufferName);
            ExecuteBuffer();
            context.Submit();
        }

        private void ExecuteBuffer(CommandBuffer commandBuffer)
        {
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }
        private void ExecuteBuffer()
        {
            ExecuteBuffer(buffer);
        }

        private void DrawVisibleGeometry(GeneralSettings generalSettings, ShaderTagId[] shaderTagIds, SortingCriteria sortingCriteria, RenderQueueRange renderQueueRange)
        {
            PerObjectData lightsPerObjectFlags = generalSettings.useLightsPerObject ? (PerObjectData.LightData | PerObjectData.LightIndices) : PerObjectData.None;
            var sortingSettings = new SortingSettings(camera) { criteria = sortingCriteria };
            var drawingSettings = new DrawingSettings(shaderTagIds[0], sortingSettings)
            {
                enableDynamicBatching = generalSettings.useDynamicBatching,
                enableInstancing = generalSettings.useGPUInstancing,
                perObjectData = PerObjectData.ReflectionProbes | lightsPerObjectFlags
            };
            for (int i = 1; i < shaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, shaderTagIds[i]);
            }
            var filteringSettings = new FilteringSettings(renderQueueRange);
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }
    }
}
