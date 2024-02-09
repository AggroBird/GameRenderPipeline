using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal enum Pass
    {
        Copy,
        BlurHorizontal,
        BlurVertical,
        BloomPrefilter,
        BloomAdd,
        BloomScatter,
        BloomScatterFinal,
        SSAO,
        SSAOCombine,
        DOFCalculateCOC,
        DOFCalculateBokeh,
        DOFPreFilter,
        DOFPostFilter,
        DOFCombine,
        Outline,
        ColorGradingNone,
        ColorGradingACES,
        ColorGradingNeutral,
        ColorGradingReinhard,
        Final,
    }

    internal sealed partial class CameraRenderer
    {
        private GameRenderPipelineAsset pipelineAsset;
        private ScriptableRenderContext context;
        public Camera Camera { get; private set; }

        private readonly Lighting lighting = new();

        private CommandBuffer buffer;
        public CommandBuffer Buffer => buffer;
        private CullingResults cullingResults;

        private readonly PostProcessStack postProcessStack = new();
        public PostProcessStack PostProcessStack => postProcessStack;

        private bool useHDR = false;
        internal RenderTextureFormat RenderTextureFormat => useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;


        private static readonly GlobalKeyword orthographicKeyword = GlobalKeyword.Create("_PROJECTION_ORTHOGRAPHIC");

        private static readonly GlobalKeyword colorSpaceLinearKeyword = GlobalKeyword.Create("_COLOR_SPACE_LINEAR");

        public static readonly GlobalKeyword OutputNormalsKeyword = GlobalKeyword.Create("_OUTPUT_NORMALS_ENABLED");


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
            opaqueNormalBufferId = Shader.PropertyToID("_OpaqueNormalBuffer"),
            ambientLightColorId = Shader.PropertyToID("_AmbientLightColor");

        public static int ColorBufferId => rtColorBufferId;
        public static int DepthBufferId => rtDepthBufferId;
        public static int NormalBufferId => rtNormalBufferId;
        public static int OpaqueColorBufferId => opaqueColorBufferId;
        public static int OpaqueDepthBufferId => opaqueDepthBufferId;
        public static int OpaqueNormalBufferId => opaqueNormalBufferId;

        private static readonly int
            skyboxStaticCubemapId = Shader.PropertyToID("_SkyboxStaticCubemap"),
            skyboxGradientTextureId = Shader.PropertyToID("_SkyboxGradientTexture"),
            skyboxGroundColorId = Shader.PropertyToID("_SkyboxGroundColor"),
            skyboxAnimTimeId = Shader.PropertyToID("_SkyboxAnimTime");

        public Material DefaultSkyboxMaterial { get; private set; }
        public static int SkyboxStaticCubemapId => skyboxStaticCubemapId;

        private static readonly int
            postProcessInputTexId = Shader.PropertyToID("_PostProcessInputTex"),
            finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend"),
            finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");

        private static readonly Rect fullViewRect = new(0f, 0f, 1f, 1f);

        private Material postProcessMaterial = null;

        public bool OutputNormals { get; private set; }
        public bool OutputOpaque { get; private set; }

        private float skyboxAnimTimeOffset;

        private readonly EnvironmentSettings defaultEnvironmentSettings = new();


        public CameraRenderer()
        {
            skyboxAnimTimeOffset = Random.Range(0f, 1000f);
        }

        public void Render(RenderGraph renderGraph, ScriptableRenderContext context, Camera camera, int cameraIndex, GameRenderPipelineAsset pipelineAsset)
        {
            this.pipelineAsset = pipelineAsset;
            this.context = context;
            Camera = camera;

            // Ensure there is a post process material
            if (!postProcessMaterial)
            {
                postProcessMaterial = new(GameRenderPipelineAsset.Instance.Resources.postProcessShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            PrepareSceneWindow();
            if (!Cull(pipelineAsset.Settings.shadows.maxDistance))
            {
                return;
            }

            var generalSettings = pipelineAsset.Settings.general;
            useHDR = generalSettings.allowHDR && camera.allowHDR;

            postProcessStack.Setup(camera, useHDR, ShowPostProcess);

            OutputOpaque = generalSettings.outputOpaqueRenderTargets;
            OutputNormals = postProcessStack.SSAOEnabled || postProcessStack.OutlineEnabled || (OutputOpaque && generalSettings.outputOpaqueNormalBuffer);

            buffer = CommandBufferPool.Get();
            buffer.SetKeyword(colorSpaceLinearKeyword, GameRenderPipeline.LinearColorSpace);
            buffer.SetKeyword(orthographicKeyword, camera.orthographic);
            ProfilingSampler cameraSampler = ProfilingSampler.Get(camera.cameraType);
            var renderGraphParameters = new RenderGraphParameters
            {
                commandBuffer = buffer,
                currentFrameIndex = Time.frameCount,
                executionName = cameraSampler.name,
                rendererListCulling = true,
                scriptableRenderContext = context,
            };
            using (renderGraph.RecordAndExecute(renderGraphParameters))
            {
                using var _ = new RenderGraphProfilingScope(renderGraph, cameraSampler);

                LightingPass.Record(renderGraph, this, lighting, cullingResults, pipelineAsset.Settings);

                SetupPass.Record(renderGraph, this);

                OpaqueGeometryPass.Record(renderGraph, Camera, cullingResults, generalSettings.useLightsPerObject);

                GetEnvironmentSettings(out EnvironmentSettings environmentSettings);
                SkyboxPass.Record(renderGraph, this, environmentSettings);

                PreTransparencyPostProcessPass.Record(renderGraph, this);

                if (OutputOpaque)
                {
                    CopyOpaqueBuffersPass.Record(renderGraph, this);
                }

                TransparentGeometryPass.Record(renderGraph, Camera, cullingResults, generalSettings.useLightsPerObject);

                UnsupportedShadersPass.Record(renderGraph, this, camera, cullingResults);

                PostTransparencyPostProcessPass.Record(renderGraph, this);

                FinalPass.Record(renderGraph, this);

                PreFXGizmoPass.Record(renderGraph, this);
                PostFXGizmoPass.Record(renderGraph, this);
            }

            Cleanup();
            Submit();

            CommandBufferPool.Release(buffer);
        }

        public void RestoreRenderTargets()
        {
            buffer.SetKeyword(OutputNormalsKeyword, OutputNormals);
            if (OutputNormals)
            {
                // Bind normal buffer as second output
                buffer.SetRenderTarget(new RenderTargetIdentifier[] { rtColorBufferId, rtNormalBufferId }, rtDepthBufferId);
            }
            else
            {
                RestoreDefaultRenderTargets();
            }
        }
        public void RestoreDefaultRenderTargets()
        {
            buffer.SetRenderTarget(
                rtColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare,
                rtDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
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
            if (TryGetEnvironmentComponent(Camera, out EnvironmentComponent environmentComponent))
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
            if (Camera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
            {
                scriptableCullingParameters.shadowDistance = Mathf.Min(maxShadowDistance, Camera.farClipPlane);
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
            if (!DefaultSkyboxMaterial)
            {
                DefaultSkyboxMaterial = new(pipelineAsset.Resources.skyboxRenderShader)
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

        public void Setup()
        {
            context.SetupCameraProperties(Camera);
            CameraClearFlags clearFlags = (Camera.cameraType == CameraType.Preview || Camera.cameraType == CameraType.SceneView) ? CameraClearFlags.SolidColor : Camera.clearFlags;
            bool clearDepth = clearFlags <= CameraClearFlags.Depth;
            bool clearColor = clearFlags == CameraClearFlags.Color;
            Color backgroundColor = clearFlags == CameraClearFlags.Color ? Camera.backgroundColor.ColorSpaceAdjusted() : Color.clear;

            // Render targets
            buffer.GetTemporaryRT(rtColorBufferId, Camera.pixelWidth, Camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat);
            buffer.GetTemporaryRT(rtDepthBufferId, Camera.pixelWidth, Camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
            buffer.SetRenderTarget(
                rtColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                rtDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(clearDepth, clearColor, backgroundColor);

            if (OutputNormals)
            {
                // Normal buffer
                buffer.GetTemporaryRT(rtNormalBufferId, Camera.pixelWidth, Camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.RGHalf);
                buffer.SetRenderTarget(rtNormalBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                buffer.ClearRenderTarget(true, true, Color.clear);
            }

            // Opaque render target
            if (OutputOpaque)
            {
                buffer.GetTemporaryRT(opaqueColorBufferId, Camera.pixelWidth, Camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat);
                buffer.GetTemporaryRT(opaqueDepthBufferId, Camera.pixelWidth, Camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                buffer.SetRenderTarget(
                    opaqueColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    opaqueDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                buffer.ClearRenderTarget(true, true, Color.clear);
            }

            RestoreRenderTargets();

            ExecuteBuffer();
        }
        private void Cleanup()
        {
            lighting.Cleanup();
            postProcessStack.Cleanup();

            buffer.ReleaseTemporaryRT(rtColorBufferId);
            buffer.ReleaseTemporaryRT(rtDepthBufferId);
            if (OutputNormals)
            {
                buffer.ReleaseTemporaryRT(rtNormalBufferId);
            }
            if (OutputOpaque)
            {
                buffer.ReleaseTemporaryRT(opaqueColorBufferId);
                buffer.ReleaseTemporaryRT(opaqueDepthBufferId);
            }
        }

        private void Submit()
        {
            ExecuteBuffer();
            context.Submit();
        }

        private void ExecuteBuffer(CommandBuffer commandBuffer)
        {
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }
        public void ExecuteBuffer()
        {
            ExecuteBuffer(buffer);
        }

        public void DrawFinal(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            var srcBlend = BlendMode.One;
            var dstBlend = BlendMode.Zero;

            buffer.SetGlobalFloat(finalSrcBlendId, (float)srcBlend);
            buffer.SetGlobalFloat(finalDstBlendId, (float)dstBlend);
            buffer.SetGlobalTexture(postProcessInputTexId, src);
            buffer.SetRenderTarget(dst, dstBlend == BlendMode.Zero && Camera.rect == fullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            buffer.SetViewport(Camera.pixelRect);
            buffer.DrawFullscreenEffect(postProcessMaterial, (int)Pass.Copy);
            buffer.SetGlobalFloat(finalSrcBlendId, 1);
            buffer.SetGlobalFloat(finalDstBlendId, 0);
        }
    }
}
