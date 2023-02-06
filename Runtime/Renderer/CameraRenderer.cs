using UnityEngine;
using UnityEngine.Rendering;
using RandomStream = System.Random;

namespace AggroBird.GRP
{
    internal sealed partial class CameraRenderer
    {
        private GameRenderPipelineAsset pipelineAsset;
        private ScriptableRenderContext context;
        private Camera camera;
        private int cameraIndex;

        private Lighting lighting = new Lighting();

        private CommandBuffer buffer = new CommandBuffer();
        private CullingResults cullingResults;

        private PostProcessStack postProcessStack = new PostProcessStack();

        private bool useHDR = false;
        internal RenderTextureFormat renderTextureFormat => useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;


        private static readonly ShaderTagId[] defaultShaderTags = { new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("CustomLit") };

        private static readonly string[] projectionKeywords =
        {
            "_PROJECTION_PERSPECTIVE",
            "_PROJECTION_ORTHOGRAPHIC",
        };

        private static readonly string[] colorSpaceKeywords =
        {
            "_COLOR_SPACE_GAMMA",
            "_COLOR_SPACE_LINEAR"
        };

        private static readonly string[] outputNormalsKeywords =
        {
            "_OUTPUT_NORMALS_OFF",
            "_OUTPUT_NORMALS_ON",
        };


        private static readonly string[] fogModeKeywords =
        {
            "_FOG_DISABLED",
            "_FOG_LINEAR",
            "_FOG_EXP",
            "_FOG_EXP2",
        };

        private static readonly string[] skyboxCloudsKeywords =
        {
            "_SKYBOX_CLOUDS_OFF",
            "_SKYBOX_CLOUDS_ON",
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
            cloudDepthBufferId = Shader.PropertyToID("_CloudDepthBuffer"),
            transparentColorBufferId = Shader.PropertyToID("_TransparentColorBuffer"),
            transparentDepthBufferId = Shader.PropertyToID("_TransparentDepthBuffer"),
            ambientLightColorId = Shader.PropertyToID("_AmbientLightColor");

        private enum SkyboxPass
        {
            RenderDynamic,
            RenderStatic,
            Blur,
            RenderWorldDynamic,
            RenderWorldStatic,
        }

        private Texture2D skyboxGradientTexture = default;
        private const int SkyboxGradientTextureBaseSize = 256;
        private const int SkyboxCubemapMipLevels = 8;
        private Color[] skyboxGradientColors = new Color[SkyboxGradientTextureBaseSize];
        private static readonly int
            skyboxStaticCubemapId = Shader.PropertyToID("_SkyboxStaticCubemap"),
            skyboxGradientTextureId = Shader.PropertyToID("_SkyboxGradientTexture"),
            skyboxGroundColorId = Shader.PropertyToID("_SkyboxGroundColor"),
            skyboxCubemapRenderBlurTargetId = Shader.PropertyToID("_SkyboxCubemapRenderBlurTarget"),
            skyboxCubemapBlurParamId = Shader.PropertyToID("_SkyboxCubemapBlurParam"),
            skyboxCubemapRenderForwardId = Shader.PropertyToID("_SkyboxCubemapRenderForward"),
            skyboxCubemapRenderUpId = Shader.PropertyToID("_SkyboxCubemapRenderUp");
        private static readonly int
            cloudColorTopId = Shader.PropertyToID("_CloudColorTop"),
            cloudColorBottomId = Shader.PropertyToID("_CloudColorBottom"),
            cloudSampleOffsetId = Shader.PropertyToID("_CloudSampleOffset"),
            cloudSampleScaleId = Shader.PropertyToID("_CloudSampleScale"),
            cloudParamId = Shader.PropertyToID("_CloudParam"),
            cloudTraceParamId = Shader.PropertyToID("_CloudTraceParam");
        private RenderTexture skyboxCubemapRenderTexture = default;
        private Texture lastSkyboxSourceTexture = null;
        private Material skyboxCubemapRenderMaterial = null;
        private RandomStream skyboxCubemapRandom = new RandomStream(0);
        // For some reason, the cubemap initializes to back in editor
        // so for the first 8 frames, force render
        private int skyboxEditorForceUpdate = Application.isEditor ? 8 : 0;


        private EnvironmentSettings defaultEnvironmentSettings = new EnvironmentSettings();


        public void Render(ScriptableRenderContext context, Camera camera, int cameraIndex, GameRenderPipelineAsset pipelineAsset)
        {
            this.pipelineAsset = pipelineAsset;
            this.context = context;
            this.camera = camera;
            this.cameraIndex = cameraIndex;

            useHDR = pipelineAsset.settings.general.allowHDR && camera.allowHDR;

            PrepareBuffer();
            PrepareSceneWindow();
            if (!Cull(pipelineAsset.settings.shadows.maxDistance))
            {
                return;
            }

            // Light and shadows
            buffer.BeginSample(bufferName);
            ExecuteBuffer();
            lighting.Setup(context, cullingResults, pipelineAsset.settings.shadows, pipelineAsset.settings.general.useLightsPerObject);
            postProcessStack.Setup(context, camera, useHDR);
            buffer.EndSample(bufferName);

            buffer.SetKeywords(colorSpaceKeywords, GameRenderPipeline.linearColorSpace ? 1 : 0);
            buffer.SetKeywords(projectionKeywords, camera.orthographic ? 1 : 0);

            // Render
            Setup();
            {
                EnvironmentSettings environmentSettings;
                if (Environment.main)
                {
                    Environment environment = Environment.main;
                    environmentSettings = environment.environmentSettings;
                    SetupEnvironment(environmentSettings, environment.wasValidated);
                    environment.wasValidated = false;
                }
                else
                {
                    environmentSettings = defaultEnvironmentSettings;
                    SetupEnvironment(defaultEnvironmentSettings, false);
                }

                buffer.SetKeywords(outputNormalsKeywords, postProcessStack.ssaoEnabled ? 1 : 0);
                if (postProcessStack.ssaoEnabled)
                {
                    buffer.SetRenderTarget(new RenderTargetIdentifier[] { opaqueColorBufferId, opaqueNormalBufferId }, opaqueDepthBufferId);
                }
                else
                {
                    buffer.SetRenderTarget(
                        opaqueColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare,
                        opaqueDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                }
                ExecuteBuffer();

                // Opaque
                DrawVisibleGeometry(pipelineAsset.settings.general, defaultShaderTags, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);
                DrawUnsupportedShaders();
                DrawEditorGizmos(GizmoSubset.PreImageEffects);

                // Post process
                postProcessStack.ApplyPreTransparency(opaqueColorBufferId, opaqueNormalBufferId, opaqueDepthBufferId, opaqueColorBufferId);

                // Skybox
                int currentDepthBuffer = opaqueDepthBufferId;
                if (camera.clearFlags == CameraClearFlags.Skybox)
                {
                    using (CommandBufferScope buffer = new CommandBufferScope("Render Skybox"))
                    {
                        EnvironmentSettings.SkyboxSettings skyboxSettings = environmentSettings.skyboxSettings;
                        if (skyboxSettings.useCubemapAsSkybox)
                        {
                            buffer.commandBuffer.SetRenderTarget(
                                opaqueColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                                opaqueDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                            buffer.commandBuffer.SetGlobalTexture(skyboxStaticCubemapId, skyboxSettings.sourceCubemap);

                            buffer.commandBuffer.DrawFullscreenEffect(skyboxCubemapRenderMaterial, (int)SkyboxPass.RenderWorldStatic);

                            // Restore mipped skybox cubemap
                            buffer.commandBuffer.SetGlobalTexture(skyboxStaticCubemapId, skyboxCubemapRenderTexture);
                        }
                        else
                        {
                            buffer.commandBuffer.SetRenderTarget(
                                opaqueColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                                cloudDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                            buffer.commandBuffer.SetGlobalTexture(opaqueDepthBufferId, currentDepthBuffer);

                            buffer.commandBuffer.DrawFullscreenEffect(skyboxCubemapRenderMaterial, (int)SkyboxPass.RenderWorldDynamic);

                            currentDepthBuffer = cloudDepthBufferId;
                        }
                        context.ExecuteCommandBuffer(buffer);
                    }
                }

                // Copy opaque into transparent
                using (CommandBufferScope buffer = new CommandBufferScope("Copy Opaque Render Buffer"))
                {
                    buffer.commandBuffer.BlitFrameBuffer(opaqueColorBufferId, currentDepthBuffer, transparentColorBufferId, transparentDepthBufferId);
                    buffer.commandBuffer.SetGlobalTexture(opaqueColorBufferId, opaqueColorBufferId);
                    buffer.commandBuffer.SetGlobalTexture(opaqueDepthBufferId, currentDepthBuffer);
                    context.ExecuteCommandBuffer(buffer);
                }

                // Transparent
                DrawVisibleGeometry(pipelineAsset.settings.general, defaultShaderTags, SortingCriteria.CommonTransparent, RenderQueueRange.transparent);

                // Post process
                postProcessStack.ApplyPostTransparency(transparentColorBufferId, BuiltinRenderTextureType.CameraTarget);

                // Draw gizmos
                DrawEditorGizmos(GizmoSubset.PostImageEffects);
            }
            Cleanup();
            Submit();
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

        private void SetupEnvironment(EnvironmentSettings settings, bool wasValidated)
        {
            buffer.SetGlobalVector(primaryLightDirectionId, lighting.primaryLightDirection);
            buffer.SetGlobalVector(primaryLightColorId, lighting.primaryLightColor);

            // Fog
            EnvironmentSettings.FogSettings fogSettings = settings.fogSettings;
            bool fogEnabled = fogSettings.enabled && showFog;
            int setFogKeyword = fogEnabled ? (int)fogSettings.fogMode : 0;
            buffer.SetKeywords(fogModeKeywords, setFogKeyword);
            if (fogEnabled)
            {
                Color ambientColor = fogSettings.ambientColor.AdjustedColor();
                buffer.SetGlobalVector(fogAmbientColorId, new Vector4(ambientColor.r, ambientColor.g, ambientColor.b, fogSettings.blend));
                buffer.SetGlobalVector(fogInscatteringColorId, fogSettings.inscatteringColor.AdjustedColor());
                if (fogSettings.overrideLightDirection)
                    buffer.SetGlobalVector(fogLightDirectionId, fogSettings.lightDirection);
                else
                    buffer.SetGlobalVector(fogLightDirectionId, lighting.primaryLightDirection);

                Vector4 fogParam = Vector4.zero;
                switch (fogSettings.fogMode)
                {
                    case FogMode.Linear:
                        float linearFogStart = fogSettings.fogParam.x;
                        float linearFogEnd = fogSettings.fogParam.y;
                        float linearFogRange = linearFogEnd - linearFogStart;
                        fogParam.x = -1.0f / linearFogRange;
                        fogParam.y = linearFogEnd / linearFogRange;
                        break;
                    default:
                        float expFogDensity = fogSettings.fogParam.z;
                        fogParam.x = expFogDensity;
                        break;
                }

                buffer.SetGlobalVector(fogParamId, fogParam);
            }

            // Clouds
            EnvironmentSettings.CloudSettings cloudSettings = settings.cloudSettings;
            bool cloudsEnabled = cloudSettings.enabled && showSkybox;
            buffer.SetKeywords(skyboxCloudsKeywords, cloudsEnabled ? 1 : 0);
            if (cloudsEnabled)
            {
                buffer.SetGlobalVector(cloudColorTopId, cloudSettings.colorTop.AdjustedColor());
                buffer.SetGlobalVector(cloudColorBottomId, cloudSettings.colorBottom.AdjustedColor());
                buffer.SetGlobalVector(cloudSampleOffsetId, cloudSettings.sampleOffset);
                buffer.SetGlobalVector(cloudSampleScaleId, cloudSettings.sampleScale);
                buffer.SetGlobalVector(cloudParamId,
                    new Vector4(cloudSettings.thickness, cloudSettings.height, cloudSettings.layerHeight, cloudSettings.fadeDistance));
                buffer.SetGlobalVector(cloudTraceParamId,
                    new Vector4(cloudSettings.traceEdgeAccuracy, cloudSettings.traceEdgeThreshold, cloudSettings.traceLengthMax, cloudSettings.traceStep));
            }


            // Skybox
            EnvironmentSettings.SkyboxSettings skyboxSettings = settings.skyboxSettings;
            Texture2D useGradientTexture = skyboxSettings.gradientTexture;
            if (!useGradientTexture)
            {
                if (!skyboxGradientTexture)
                {
                    skyboxGradientColors = new Color[SkyboxGradientTextureBaseSize];
                    skyboxGradientTexture = new Texture2D(SkyboxGradientTextureBaseSize, 1);
                    skyboxGradientTexture.wrapMode = TextureWrapMode.Clamp;
                }
                if (skyboxSettings.gradient != null)
                {
                    float step = 1.0f / (SkyboxGradientTextureBaseSize - 1);
                    float t = 0;
                    for (int i = 0; i < SkyboxGradientTextureBaseSize; i++, t += step)
                    {
                        skyboxGradientColors[i] = skyboxSettings.gradient.Evaluate(t);
                    }
                    skyboxGradientTexture.SetPixels(skyboxGradientColors);
                    skyboxGradientTexture.Apply();
                }
                useGradientTexture = skyboxGradientTexture;
            }

            buffer.SetGlobalVector(ambientLightColorId, settings.skyboxSettings.ambientColor.AdjustedColor());
            ExecuteBuffer();

            if (!skyboxCubemapRenderMaterial)
            {
                skyboxCubemapRenderMaterial = new Material(pipelineAsset.skyboxRenderShader);
                skyboxCubemapRenderMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            if (camera.cameraType <= CameraType.SceneView)
            {
                using (CommandBufferScope buffer = new CommandBufferScope("Render Skybox Cubemap"))
                {
                    CommandBuffer skyboxBuffer = buffer.commandBuffer;

                    skyboxBuffer.SetGlobalTexture(skyboxGradientTextureId, useGradientTexture);

                    if (!skyboxSettings.generateSkyboxCubemap)
                    {
                        skyboxBuffer.SetGlobalTexture(skyboxStaticCubemapId, skyboxSettings.sourceCubemap);
                    }
                    else
                    {
                        // Custom dynamic skybox cubemap
                        RenderTextureDescriptor desc = new RenderTextureDescriptor(128, 128, RenderTextureFormat.Default, 0);
                        desc.dimension = TextureDimension.Cube;
                        desc.useMipMap = true;
                        desc.autoGenerateMips = false;
                        desc.mipCount = SkyboxCubemapMipLevels - 1;
                        if (!skyboxCubemapRenderTexture)
                        {
                            skyboxCubemapRenderTexture = new RenderTexture(desc);
                            skyboxCubemapRenderTexture.name = "SkyboxCubemapRenderTexture";
                            skyboxCubemapRenderTexture.filterMode = FilterMode.Trilinear;
                        }
                        skyboxBuffer.GetTemporaryRT(skyboxCubemapRenderBlurTargetId, desc, FilterMode.Bilinear);
                        skyboxBuffer.SetGlobalVector(skyboxGroundColorId, skyboxSettings.groundColor.AdjustedColor());

                        bool useStaticCubemap = skyboxSettings.sourceCubemap;
                        Texture currentSourceTexture = useStaticCubemap ? skyboxSettings.sourceCubemap : skyboxCubemapRenderTexture;
                        // Force render when static cubemap environment settings changed
                        bool forceRenderCubemap = (currentSourceTexture != lastSkyboxSourceTexture || wasValidated);

                        if (skyboxEditorForceUpdate <= 0)
                            lastSkyboxSourceTexture = currentSourceTexture;
                        else
                            skyboxEditorForceUpdate--;

                        if (useStaticCubemap) skyboxBuffer.SetGlobalTexture(skyboxStaticCubemapId, skyboxSettings.sourceCubemap);
                        SkyboxPass skyboxPass = useStaticCubemap ? SkyboxPass.RenderStatic : SkyboxPass.RenderDynamic;
                        for (int face = 0; face < 6; face++)
                        {
                            skyboxBuffer.SetRenderTarget(skyboxCubemapRenderTexture, 0, (CubemapFace)face);
                            SetSkyboxCubemapFaceParameters(skyboxBuffer, (CubemapFace)face);
                            skyboxBuffer.DrawFullscreenEffect(skyboxCubemapRenderMaterial, (int)skyboxPass);
                        }

                        // If we are not force rendering, we only have to do the blur when we are not using a static cubemap
                        // (static cubemap is not bound to change)
                        int iterCount = forceRenderCubemap ? 32 : (useStaticCubemap ? 0 : 1);
                        for (int iter = 0; iter < iterCount; iter++)
                        {
                            float str = 0.1f;
                            float blend = forceRenderCubemap ? 1.0f : 0.1f;
                            forceRenderCubemap = false;
                            for (int srcMip = 0; srcMip < SkyboxCubemapMipLevels - 1; srcMip++)
                            {
                                int dstMip = (srcMip + 1);

                                for (int face = 0; face < 6; face++)
                                {
                                    SetSkyboxCubemapFaceParameters(skyboxBuffer, (CubemapFace)face);
                                    skyboxBuffer.SetGlobalVector(skyboxCubemapBlurParamId, new Vector4(srcMip, skyboxCubemapRandom.Next(0, 1024), str, 1));
                                    skyboxBuffer.SetRenderTarget(skyboxCubemapRenderBlurTargetId, dstMip, (CubemapFace)face);
                                    skyboxBuffer.SetGlobalTexture(skyboxStaticCubemapId, skyboxCubemapRenderTexture);
                                    skyboxBuffer.DrawFullscreenEffect(skyboxCubemapRenderMaterial, (int)SkyboxPass.Blur);
                                }
                                for (int face = 0; face < 6; face++)
                                {
                                    SetSkyboxCubemapFaceParameters(skyboxBuffer, (CubemapFace)face);
                                    skyboxBuffer.SetGlobalVector(skyboxCubemapBlurParamId, new Vector4(dstMip, skyboxCubemapRandom.Next(0, 1024), str, blend));
                                    skyboxBuffer.SetRenderTarget(skyboxCubemapRenderTexture, dstMip, (CubemapFace)face);
                                    skyboxBuffer.SetGlobalTexture(skyboxStaticCubemapId, skyboxCubemapRenderBlurTargetId);
                                    skyboxBuffer.DrawFullscreenEffect(skyboxCubemapRenderMaterial, (int)SkyboxPass.Blur);
                                }
                                str *= 1.5f;
                            }
                        }

                        skyboxBuffer.SetGlobalTexture(skyboxStaticCubemapId, skyboxCubemapRenderTexture);
                        skyboxBuffer.ReleaseTemporaryRT(skyboxCubemapRenderBlurTargetId);
                    }
                    context.ExecuteCommandBuffer(skyboxBuffer);
                }
            }
        }
        private void SetSkyboxCubemapFaceParameters(CommandBuffer buffer, CubemapFace cubemapFace)
        {
            switch (cubemapFace)
            {
                case CubemapFace.PositiveX:
                    buffer.SetGlobalVector(skyboxCubemapRenderForwardId, Vector3.right);
                    buffer.SetGlobalVector(skyboxCubemapRenderUpId, Vector3.up);
                    break;
                case CubemapFace.NegativeX:
                    buffer.SetGlobalVector(skyboxCubemapRenderForwardId, Vector3.left);
                    buffer.SetGlobalVector(skyboxCubemapRenderUpId, Vector3.up);
                    break;
                case CubemapFace.PositiveY:
                    buffer.SetGlobalVector(skyboxCubemapRenderForwardId, Vector3.up);
                    buffer.SetGlobalVector(skyboxCubemapRenderUpId, Vector3.back);
                    break;
                case CubemapFace.NegativeY:
                    buffer.SetGlobalVector(skyboxCubemapRenderForwardId, Vector3.down);
                    buffer.SetGlobalVector(skyboxCubemapRenderUpId, Vector3.forward);
                    break;
                case CubemapFace.PositiveZ:
                    buffer.SetGlobalVector(skyboxCubemapRenderForwardId, Vector3.forward);
                    buffer.SetGlobalVector(skyboxCubemapRenderUpId, Vector3.up);
                    break;
                case CubemapFace.NegativeZ:
                    buffer.SetGlobalVector(skyboxCubemapRenderForwardId, Vector3.back);
                    buffer.SetGlobalVector(skyboxCubemapRenderUpId, Vector3.up);
                    break;
            }
        }

        private void Setup()
        {
            context.SetupCameraProperties(camera);
            CameraClearFlags clearFlags = camera.clearFlags;
            bool clearDepth = clearFlags <= CameraClearFlags.Depth;
            bool clearColor = clearFlags == CameraClearFlags.Color;
            Color backgroundColor = clearFlags == CameraClearFlags.Color ?
                (GameRenderPipeline.linearColorSpace ? camera.backgroundColor.linear : camera.backgroundColor) : Color.clear;

            // Opaque render targets
            buffer.GetTemporaryRT(opaqueColorBufferId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, renderTextureFormat);
            buffer.GetTemporaryRT(opaqueDepthBufferId, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
            buffer.SetRenderTarget(
                opaqueColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                opaqueDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(clearDepth, clearColor, backgroundColor);

            if (postProcessStack.ssaoEnabled)
            {
                // Normal buffer
                buffer.GetTemporaryRT(opaqueNormalBufferId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.RGHalf);
                buffer.SetRenderTarget(opaqueNormalBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                buffer.ClearRenderTarget(true, true, Color.clear);
            }

            // Transparent render target
            buffer.GetTemporaryRT(transparentColorBufferId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, renderTextureFormat);
            buffer.GetTemporaryRT(transparentDepthBufferId, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
            buffer.SetRenderTarget(
                transparentColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                transparentDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(true, true, Color.clear);

            // Cloud depth target
            buffer.GetTemporaryRT(cloudDepthBufferId, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);

            buffer.BeginSample(bufferName);
            ExecuteBuffer();
        }
        private void Cleanup()
        {
            lighting.Cleanup();
            postProcessStack.Cleanup();

            buffer.ReleaseTemporaryRT(opaqueColorBufferId);
            buffer.ReleaseTemporaryRT(opaqueDepthBufferId);
            if (postProcessStack.ssaoEnabled)
            {
                buffer.ReleaseTemporaryRT(opaqueNormalBufferId);
            }
            buffer.ReleaseTemporaryRT(transparentColorBufferId);
            buffer.ReleaseTemporaryRT(transparentDepthBufferId);
            buffer.ReleaseTemporaryRT(cloudDepthBufferId);
        }

        private void Submit()
        {
            buffer.EndSample(bufferName);
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