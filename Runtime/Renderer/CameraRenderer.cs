using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal enum PostProcessPass
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

    internal readonly ref struct CameraRendererTextures
    {
        public readonly TextureHandle rtColorBuffer;
        public readonly TextureHandle rtDepthBuffer;
        public readonly TextureHandle rtNormalBuffer;
        public readonly TextureHandle opaqueColorBuffer;
        public readonly TextureHandle opaqueDepthBuffer;

        public CameraRendererTextures(TextureHandle rtColorBuffer, TextureHandle rtDepthBuffer, TextureHandle rtNormalBuffer, TextureHandle opaqueColorBuffer, TextureHandle opaqueDepthBuffer)
        {
            this.rtColorBuffer = rtColorBuffer;
            this.rtDepthBuffer = rtDepthBuffer;
            this.rtNormalBuffer = rtNormalBuffer;
            this.opaqueColorBuffer = opaqueColorBuffer;
            this.opaqueDepthBuffer = opaqueDepthBuffer;
        }
    }

    internal readonly ref struct ShadowTextures
    {
        public readonly TextureHandle directionalAtlas;
        public readonly TextureHandle otherAtlas;

        public ShadowTextures(TextureHandle directionalAtlas, TextureHandle otherAtlas)
        {
            this.directionalAtlas = directionalAtlas;
            this.otherAtlas = otherAtlas;
        }
    }

    internal sealed partial class CameraRenderer
    {
        private GameRenderPipelineAsset pipelineAsset;
        private ScriptableRenderContext context;
        private Camera camera;

        private CommandBuffer buffer;
        public CommandBuffer Buffer => buffer;

        private readonly PostProcessStack postProcessStack = new();
        public PostProcessStack PostProcessStack => postProcessStack;


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
            opaqueColorBufferId = Shader.PropertyToID("_OpaqueColorBuffer"),
            opaqueDepthBufferId = Shader.PropertyToID("_OpaqueDepthBuffer"),
            opaqueNormalBufferId = Shader.PropertyToID("_OpaqueNormalBuffer"),
            ambientLightColorId = Shader.PropertyToID("_AmbientLightColor");

        public static int OpaqueColorBufferId => opaqueColorBufferId;
        public static int OpaqueDepthBufferId => opaqueDepthBufferId;
        public static int OpaqueNormalBufferId => opaqueNormalBufferId;

        private static readonly int
            skyboxGradientTextureId = Shader.PropertyToID("_SkyboxGradientTexture"),
            skyboxGroundColorId = Shader.PropertyToID("_SkyboxGroundColor");

        private Material defaultSkyboxMaterial;

        private Material postProcessMaterial = null;

        private bool outputNormals;
        private bool outputOpaque;

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
            this.camera = camera;

            // Ensure there is a post process material
            if (!postProcessMaterial)
            {
                postProcessMaterial = new(GameRenderPipelineAsset.Instance.Resources.postProcessShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            PrepareSceneWindow();
            if (!this.camera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
            {
                return;
            }
            scriptableCullingParameters.shadowDistance = Mathf.Min(pipelineAsset.Settings.shadows.maxDistance, this.camera.farClipPlane);
            CullingResults cullingResults = context.Cull(ref scriptableCullingParameters);

            var generalSettings = pipelineAsset.Settings.general;
            bool useHDR = generalSettings.allowHDR && camera.allowHDR;

            postProcessStack.Setup(camera, useHDR, ShowPostProcess);

            outputOpaque = generalSettings.outputOpaqueRenderTargets;
            outputNormals = outputOpaque && generalSettings.outputOpaqueNormalBuffer;

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

                var shadowTextures = LightingPass.Record(renderGraph, camera, cullingResults, pipelineAsset.Settings, showFlags, out PrimaryDirectionalLightInfo primaryDirectionalLightInfo);

                var cameraTextures = SetupPass.Record(renderGraph, camera, outputOpaque, outputNormals, useHDR, generalSettings.depthBufferBits);

                OpaqueGeometryPass.Record(renderGraph, this.camera, cullingResults, generalSettings.useLightsPerObject, cameraTextures, shadowTextures);

                GetEnvironmentSettings(out EnvironmentSettings environmentSettings, primaryDirectionalLightInfo);
                if (camera.clearFlags == CameraClearFlags.Skybox && ShowSkybox)
                {
                    SkyboxPass.Record(renderGraph, defaultSkyboxMaterial, environmentSettings, cameraTextures);
                }

                PreTransparencyPostProcessPass.Record(renderGraph, postProcessStack, outputNormals, cameraTextures);

                if (outputOpaque)
                {
                    CopyOpaqueBuffersPass.Record(renderGraph, outputNormals, cameraTextures);
                }

                TransparentGeometryPass.Record(renderGraph, this.camera, cullingResults, generalSettings.useLightsPerObject, outputOpaque, outputNormals, cameraTextures, shadowTextures);

                UnsupportedShadersPass.Record(renderGraph, camera, cullingResults, cameraTextures);

                PostTransparencyPostProcessPass.Record(renderGraph, postProcessStack, cameraTextures);

                FinalPass.Record(renderGraph, camera, postProcessMaterial, cameraTextures);

                GizmoPass.Record(renderGraph, camera, cameraTextures);
            }

            postProcessStack.Cleanup();

            context.ExecuteCommandBuffer(buffer);
            context.Submit();

            CommandBufferPool.Release(buffer);
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
        private void GetEnvironmentSettings(out EnvironmentSettings environmentSettings, in PrimaryDirectionalLightInfo primaryDirectionalLightInfo)
        {
            if (TryGetEnvironmentComponent(camera, out EnvironmentComponent environmentComponent))
            {
                var settings = environmentComponent.GetEnvironmentSettings();
                if (settings != null)
                {
                    environmentSettings = settings;
                    SetupEnvironment(environmentSettings, environmentComponent.modified, primaryDirectionalLightInfo);
                    environmentComponent.modified = false;
                    return;
                }
            }

            environmentSettings = defaultEnvironmentSettings;
            SetupEnvironment(environmentSettings, false, primaryDirectionalLightInfo);
        }

        private void SetupEnvironment(EnvironmentSettings settings, bool environmentModified, in PrimaryDirectionalLightInfo primaryDirectionalLightInfo)
        {
            settings.UpdateEnvironment();

            buffer.SetGlobalVector(primaryLightDirectionId, primaryDirectionalLightInfo.direction);
            buffer.SetGlobalVector(primaryLightColorId, primaryDirectionalLightInfo.color);

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
                    buffer.SetGlobalVector(fogLightDirectionId, primaryDirectionalLightInfo.direction);

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

        private void ExecuteBuffer(CommandBuffer commandBuffer)
        {
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }
        public void ExecuteBuffer()
        {
            ExecuteBuffer(buffer);
        }
    }
}
