﻿using System;
using System.Collections.Generic;
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
        ApplyColorGrading,
        FXAALow,
        FXAAMedium,
        FXAAHigh,
        FinalRescale,
    }

    internal sealed class PostProcessStack
    {
        public Camera camera;
        public bool useHDR = false;
        public BicubicRescalingMode bicubicRescalingMode;

        public PostProcessSettings settings = default;
        public BlendMode srcBlendMode, dstBlendMode;

        public bool postProcessEnabled;

        public Material PostProcessMaterial { get; private set; }

        internal interface IEditorGizmoEffect
        {
            bool Enabled { get; }
            int Priority { get; }

            void Execute(CommandBuffer buffer, RenderTargetIdentifier color, RenderTargetIdentifier depth);
        }
        internal static readonly List<IEditorGizmoEffect> editorGizmoEffects = new();

        public bool DrawGizmoEffects => camera.cameraType == CameraType.SceneView && editorGizmoEffects.Count > 0;


        internal static readonly int
            postProcessInputTexId = Shader.PropertyToID("_PostProcessInputTex"),
            finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend"),
            finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");

        private static readonly Rect fullViewRect = new(0f, 0f, 1f, 1f);


        public void Setup(Camera camera, bool useHDR, BicubicRescalingMode bicubicRescalingMode, bool postProcessEnabled, ref float renderScale)
        {
            this.camera = camera;
            this.useHDR = useHDR;
            this.bicubicRescalingMode = bicubicRescalingMode;

            if (TryGetPostProcessComponent(camera, out PostProcessComponent postProcessComponent))
            {
                var blendMode = postProcessComponent.GetFinalBlendMode();
                srcBlendMode = blendMode.source;
                dstBlendMode = blendMode.destination;
                settings = postProcessComponent.GetPostProcessSettings();
                if (settings == null) postProcessEnabled = false;
                renderScale = postProcessComponent.GetRenderScale(renderScale);
            }
            else
            {
                srcBlendMode = BlendMode.One;
                dstBlendMode = BlendMode.Zero;
                postProcessEnabled = false;
            }

            if (!PostProcessMaterial)
            {
                PostProcessMaterial = new(GameRenderPipelineAsset.Instance.Resources.postProcessShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            this.postProcessEnabled = postProcessEnabled;
        }
        public void Cleanup()
        {

        }


        public void Draw(RenderGraphContext context, RenderTargetIdentifier src, RenderTargetIdentifier dst, Material material, int pass)
        {
            var buffer = context.cmd;
            buffer.SetGlobalTexture(postProcessInputTexId, src);
            buffer.SetRenderTarget(dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.DrawFullscreenEffect(material, pass);
        }
        public void Draw(RenderGraphContext context, RenderTargetIdentifier src, RenderTargetIdentifier dst, PostProcessPass pass)
        {
            Draw(context, src, dst, PostProcessMaterial, (int)pass);
        }
        public void DrawFinal(RenderGraphContext context, TextureHandle src, Material material, int pass)
        {
            var buffer = context.cmd;
            buffer.SetGlobalFloat(finalSrcBlendId, (float)srcBlendMode);
            buffer.SetGlobalFloat(finalDstBlendId, (float)dstBlendMode);
            buffer.SetGlobalTexture(postProcessInputTexId, src);
            buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, dstBlendMode == BlendMode.Zero && camera.rect == fullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            buffer.SetViewport(camera.pixelRect);
            buffer.DrawFullscreenEffect(material, pass);
        }
        public void DrawFinal(RenderGraphContext context, TextureHandle rtColorBuffer, PostProcessPass pass)
        {
            DrawFinal(context, rtColorBuffer, PostProcessMaterial, (int)pass);
        }


        private bool TryGetPostProcessComponent(Camera camera, out PostProcessComponent postProcessComponent)
        {
            if (camera.TryGetComponent(out postProcessComponent) && postProcessComponent.enabled && postProcessComponent.gameObject.activeInHierarchy)
            {
                return true;
            }
#if UNITY_EDITOR
            else if (camera.cameraType == CameraType.SceneView)
            {
                // Try to get current main camera component for editor scene view
                var activeCameraComponents = PostProcessCameraComponent.activeCameraComponents;
                for (int i = 0; i < activeCameraComponents.Count;)
                {
                    var component = activeCameraComponents[i];
                    if (!component)
                    {
                        int last = activeCameraComponents.Count - 1;
                        if (i == last)
                        {
                            activeCameraComponents.RemoveAt(i);
                        }
                        else
                        {
                            activeCameraComponents.RemoveAt(last);
                        }
                        continue;
                    }

                    if (component.enabled && component.gameObject.activeInHierarchy)
                    {
                        if (component.TryGetComponent(out camera) && camera.CompareTag(Tags.MainCameraTag))
                        {
                            postProcessComponent = component;
                            return true;
                        }
                    }

                    i++;
                }
            }
#endif

            // Find active post process
            var activePostProcessSceneComponents = PostProcessSceneComponent.activeSceneComponents;
            int highestPriority = int.MinValue;
            for (int i = 0; i < activePostProcessSceneComponents.Count;)
            {
                var component = activePostProcessSceneComponents[i];
                if (!component)
                {
                    int last = activePostProcessSceneComponents.Count - 1;
                    if (i == last)
                    {
                        activePostProcessSceneComponents.RemoveAt(i);
                    }
                    else
                    {
                        activePostProcessSceneComponents.RemoveAt(last);
                    }
                    continue;
                }

                if (component.enabled && component.gameObject.activeInHierarchy && component.priority > highestPriority)
                {
                    postProcessComponent = component;
                    highestPriority = component.priority;
                }

                i++;
            }

            return postProcessComponent;
        }


        public void RenderEditorGizmoEffects(CommandBuffer buffer, TextureHandle color, TextureHandle depth)
        {
            editorGizmoEffects.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            foreach (IEditorGizmoEffect effect in editorGizmoEffects)
            {
                if (!effect.Enabled) continue;

                string name = effect.GetType().Name;

                buffer.BeginSample(name);
                try
                {
                    effect.Execute(buffer, color, depth);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                buffer.EndSample(name);
            }
        }
    }

    /*internal partial class PostProcessStack
    {
        private CommandBuffer buffer;

        private Material postProcessMaterial = default;
        private Material smaaMaterial = default;
        private Material fxaaMaterial = default;

        private RenderGraphContext context;
        private Camera camera;

        private PostProcessSettings settings = default;
        private BlendMode srcBlendMode, dstBlendMode;

        private bool postProcessEnabled;

        public bool DofEnabled
        {
            get
            {
                PostProcessSettings.DepthOfField dof = settings.depthOfField;
                if (!dof.enabled) return false;

                return dof.bokehRadius > 0;
            }
        }
        public bool BloomEnabled
        {
            get
            {
                PostProcessSettings.Bloom bloom = settings.bloom;
                if (!bloom.enabled) return false;

                int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
                return bloom.intensity > 0 && width >= bloom.downscaleLimit * 2 && height >= bloom.downscaleLimit * 2;
            }
        }
        private bool DrawGizmoEffects => camera.cameraType == CameraType.SceneView && editorGizmoEffects.Count > 0;

        private bool useHDR = false;
        internal RenderTextureFormat RenderTextureFormat => useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;


        private static readonly int[] postProcessRenderTargetIds = new int[] { Shader.PropertyToID("_PostProcessRenderTarget0"), Shader.PropertyToID("_PostProcessRenderTarget1") };
        private readonly bool[] buffersAllocated = new bool[2] { false, false };

        private static readonly int
            bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling"),
            bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"),
            bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
            bloomIntensityId = Shader.PropertyToID("_BloomIntensity"),
            ssaoParametersId = Shader.PropertyToID("_SSAOParameters"),
            dofCOCBufferId = Shader.PropertyToID("_DOFCOCBuffer"),
            dofResultId = Shader.PropertyToID("_DOFResult"),
            dofBokeh0Id = Shader.PropertyToID("_DOFBokeh0"),
            dofBokeh1Id = Shader.PropertyToID("_DOFBokeh1"),
            dofFocusDistanceId = Shader.PropertyToID("_DOFFocusDistance"),
            dofFocusRangeId = Shader.PropertyToID("_DOFFocusRange"),
            dofBokehRadiusId = Shader.PropertyToID("_DOFBokehRadius"),
            outlineColorId = Shader.PropertyToID("_OutlineColor"),
            outlineParamId = Shader.PropertyToID("_OutlineParam"),
            outlineDepthFadeId = Shader.PropertyToID("_OutlineDepthFade"),
            colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments"),
            whiteBalanceId = Shader.PropertyToID("_WhiteBalance"),
            splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows"),
            splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights"),
            channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed"),
            channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen"),
            channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue"),
            smhShadowsId = Shader.PropertyToID("_SMHShadows"),
            smhMidtonesId = Shader.PropertyToID("_SMHMidtones"),
            smhHighlightsId = Shader.PropertyToID("_SMHHighlights"),
            smhRangeId = Shader.PropertyToID("_SMHRange"),
            colorFilterId = Shader.PropertyToID("_ColorFilter"),
            colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT"),
            colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"),
            colorGradingLUTInLogCId = Shader.PropertyToID("_ColorGradingLUTInLogC"),
            colorGradingEnabledId = Shader.PropertyToID("_ColorGradingEnabled"),
            vignetteParamId = Shader.PropertyToID("_VignetteParam"),
            postProcessInputTexId = Shader.PropertyToID("_PostProcessInputTex"),
            postProcessDepthTexId = Shader.PropertyToID("_PostProcessDepthTex"),
            postProcessNormalTexId = Shader.PropertyToID("_PostProcessNormalTex"),
            postProcessCombineTexId = Shader.PropertyToID("_PostProcessCombineTex"),
            finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend"),
            finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");
        private const int MaxBloomPyramidLevels = 16;
        private readonly int bloomPyramidId;
        private readonly int[] ssaoBufferIds = new int[2] { Shader.PropertyToID("_SSAOBuffer0"), Shader.PropertyToID("_SSAOBuffer1") };

        private enum SMAAPass
        {
            EdgeDetection = 0,
            BlendWeights = 3,
            NeighborhoodBlending = 6
        }

        private static readonly int
            smaaRtMetrics = Shader.PropertyToID("_SMAA_RTMetrics"),
            smaaFlipId = Shader.PropertyToID("_SMAA_Flip"),
            smaaFlopId = Shader.PropertyToID("_SMAA_Flop"),
            smaaAreaTexId = Shader.PropertyToID("_SMAA_AreaTex"),
            smaaSearchTexId = Shader.PropertyToID("_SMAA_SearchTex"),
            smaaBlendTexId = Shader.PropertyToID("_SMAA_BlendTex");



        private RenderTargetIdentifier currentSourceBuffer;
        private int currentBufferIndex = 0;
        private RenderTargetIdentifier GetNextBuffer()
        {
            int next = (currentBufferIndex + 1) & 1;

            if (!buffersAllocated[next])
            {
                buffer.GetTemporaryRT(postProcessRenderTargetIds[next], camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat);
                buffersAllocated[next] = true;
            }

            return postProcessRenderTargetIds[next];
        }
        private void SwapBuffers()
        {
            currentBufferIndex = (currentBufferIndex + 1) & 1;
            currentSourceBuffer = postProcessRenderTargetIds[currentBufferIndex];
        }


        private List<PostProcessEffect>[] customEffects = default;
        private readonly List<PostProcessEffect> effectComponentBuffer = new();



        public PostProcessStack()
        {
            bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
            for (int i = 1; i < MaxBloomPyramidLevels * 2; i++)
            {
                Shader.PropertyToID("_BloomPyramid" + i);
            }
        }


        private bool TryGetPostProcessComponent(Camera camera, out PostProcessComponent postProcessComponent)
        {
            if (camera.TryGetComponent(out postProcessComponent) && postProcessComponent.enabled)
            {
                return true;
            }
            else if (camera.cameraType == CameraType.SceneView)
            {
#if UNITY_EDITOR
                // Try to get current main camera component for editor scene view
                var activeCameraComponents = PostProcessCameraComponent.activeCameraComponents;
                for (int i = 0; i < activeCameraComponents.Count;)
                {
                    var postProcessCameraComponent = activeCameraComponents[i];
                    if (!postProcessCameraComponent)
                    {
                        int last = activeCameraComponents.Count - 1;
                        if (i == last)
                        {
                            activeCameraComponents.RemoveAt(i);
                        }
                        else
                        {
                            postProcessCameraComponent = activeCameraComponents[last];
                            activeCameraComponents.RemoveAt(last);
                        }
                        continue;
                    }

                    if (postProcessCameraComponent.TryGetComponent(out Camera cameraComponent) && cameraComponent.CompareTag(Tags.MainCameraTag))
                    {
                        postProcessComponent = postProcessCameraComponent;
                        return true;
                    }

                    i++;
                }
#endif
            }

            // Find active post process
            var activePostProcessComponents = PostProcessComponent.activePostProcessComponents;
            for (int i = 0; i < activePostProcessComponents.Count;)
            {
                postProcessComponent = activePostProcessComponents[i];
                if (!postProcessComponent)
                {
                    int last = activePostProcessComponents.Count - 1;
                    if (i == last)
                    {
                        activePostProcessComponents.RemoveAt(i);
                    }
                    else
                    {
                        postProcessComponent = activePostProcessComponents[last];
                        activePostProcessComponents.RemoveAt(last);
                    }
                    continue;
                }
                break;
            }
            return postProcessComponent;
        }

        private static bool normalBufferInvalidWarningEmitted = false;
        private void ValidateNormalBuffer(RenderTargetIdentifier buffer)
        {
            if (buffer == default)
            {
                if (!normalBufferInvalidWarningEmitted)
                {
                    normalBufferInvalidWarningEmitted = true;
                    Debug.LogWarning("Pre transparency post process requires the geometry normal output buffer");
                }
            }
            else
            {
                normalBufferInvalidWarningEmitted = false;
            }
        }

        public void Setup(Camera camera, bool useHDR, bool postProcessEnabled)
        {
            this.camera = camera;
            this.useHDR = useHDR;

            if (TryGetPostProcessComponent(camera, out PostProcessComponent postProcessComponent))
            {
                var blendMode = postProcessComponent.GetFinalBlendMode();
                srcBlendMode = blendMode.source;
                dstBlendMode = blendMode.destination;
                settings = postProcessComponent.GetPostProcessSettings();
                if (settings == null) postProcessEnabled = false;
            }
            else
            {
                srcBlendMode = BlendMode.One;
                dstBlendMode = BlendMode.Zero;
                postProcessEnabled = false;
            }

            this.postProcessEnabled = postProcessEnabled;

            if (postProcessEnabled)
            {
                // Get custom effects
                postProcessComponent.GetComponents(effectComponentBuffer);
                if (effectComponentBuffer.Count > 0)
                {
                    if (customEffects == null)
                    {
                        customEffects = new List<PostProcessEffect>[PostProcessEffect.OrderCount];
                        for (int i = 0; i < customEffects.Length; i++)
                        {
                            customEffects[i] = new();
                        }
                    }

                    foreach (PostProcessEffect effect in effectComponentBuffer)
                    {
                        if (effect.enabled)
                        {
                            GetCustomEffectsList(effect.Order).Add(effect);
                        }
                    }

                    for (int i = 0; i < customEffects.Length; i++)
                    {
                        if (customEffects[i].Count > 1)
                        {
                            customEffects[i].Sort((x, y) => x.Priority.CompareTo(y.Priority));
                        }
                    }
                }
            }

            editorGizmoEffects.Sort((x, y) => x.Priority.CompareTo(y.Priority));


            currentBufferIndex = 0;
        }

        public void Cleanup()
        {
            bool anyReleased = false;
            for (int i = 0; i < 2; i++)
            {
                if (buffersAllocated[i])
                {
                    buffer.ReleaseTemporaryRT(postProcessRenderTargetIds[i]);
                    buffersAllocated[i] = false;
                    anyReleased = true;
                }
            }
            if (anyReleased)
            {
                ExecuteBuffer();
            }

            ClearCustomEffectsList();
        }

        public void ApplyPreTransparency(RenderGraphContext context, RenderTargetIdentifier srcColor, RenderTargetIdentifier srcDepth, RenderTargetIdentifier srcNormal, RenderTargetIdentifier dstColor)
        {
            this.context = context;
            buffer = context.cmd;

            if (postProcessEnabled)
            {
                // TODO: Change to render graph
                ValidateNormalBuffer(srcNormal);

                currentSourceBuffer = srcColor;

                if (settings.ambientOcclusion.enabled)
                {
                    ApplySSAO(currentSourceBuffer, srcDepth, srcNormal, GetNextBuffer());
                    SwapBuffers();
                }

                if (settings.outline.enabled)
                {
                    ApplyOutline(currentSourceBuffer, srcDepth, srcNormal, GetNextBuffer());
                    SwapBuffers();
                }

                if (DrawGizmoEffects)
                {
                    ExecuteEditorEffectsList(srcDepth);
                }

                Draw(currentSourceBuffer, dstColor, PostProcessPass.Copy);
                ExecuteBuffer();
            }
            else if (DrawGizmoEffects)
            {
                currentSourceBuffer = srcColor;

                ExecuteEditorEffectsList(srcDepth);

                Draw(currentSourceBuffer, dstColor, PostProcessPass.Copy);
                ExecuteBuffer();
            }
        }
        public void ApplyPostTransparency(RenderGraphContext context, RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            this.context = context;
            buffer = context.cmd;

            if (postProcessEnabled)
            {
                currentSourceBuffer = src;

                // Depth of field
                if (DofEnabled)
                {
                    ApplyDOF(currentSourceBuffer, GetNextBuffer());
                    SwapBuffers();
                }

                ExecuteCustomEffectsList(PostProcessEffectOrder.BeforeBloom);

                // Bloom
                if (BloomEnabled)
                {
                    ApplyBloom(currentSourceBuffer, GetNextBuffer());
                    SwapBuffers();
                }

                ExecuteCustomEffectsList(PostProcessEffectOrder.BeforeColorGrading);

                // Color grading
                buffer.SetGlobalBool(colorGradingEnabledId, true);
                ApplyColorGrading(currentSourceBuffer, GetNextBuffer());
                SwapBuffers();

                ExecuteCustomEffectsList(PostProcessEffectOrder.BeforeAntiAlias);

                // SMAA
                if (settings.antiAlias.enabled)
                {
                    switch (settings.antiAlias.algorithm)
                    {
                        case PostProcessSettings.AntiAlias.Algorithm.FXAA:
                            ApplyFXAA(currentSourceBuffer, GetNextBuffer());
                            break;
                        default:
                            ApplySMAA(currentSourceBuffer, GetNextBuffer());
                            break;
                    }
                    SwapBuffers();
                }

                ExecuteCustomEffectsList(PostProcessEffectOrder.BeforeDisplay);

                Draw(currentSourceBuffer, dst, PostProcessPass.Copy);
            }
            else
            {
                buffer.SetGlobalBool(colorGradingEnabledId, false);
            }

            ExecuteBuffer();
        }

        private void ExecuteBuffer()
        {
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        private void ApplySSAO(RenderTargetIdentifier srcColor, RenderTargetIdentifier srcDepth, RenderTargetIdentifier srcNormal, RenderTargetIdentifier dstColor)
        {
            buffer.BeginSample("SSAO");

            int width = camera.pixelWidth, height = camera.pixelHeight;
            buffer.GetTemporaryRT(ssaoBufferIds[0], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            buffer.GetTemporaryRT(ssaoBufferIds[1], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);

            // AO
            PostProcessSettings.AmbientOcclusion ao = settings.ambientOcclusion;
            buffer.SetGlobalVector(ssaoParametersId, new(ao.sampleCount, ao.radius, ao.intensity, 0));
            buffer.SetGlobalTexture(postProcessNormalTexId, srcNormal);
            buffer.SetGlobalTexture(postProcessDepthTexId, srcDepth);
            buffer.SetRenderTarget(ssaoBufferIds[0], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.DrawFullscreenEffect(postProcessMaterial, (int)PostProcessPass.SSAO);

            // Blur
            Draw(ssaoBufferIds[0], ssaoBufferIds[1], PostProcessPass.BlurHorizontal);
            Draw(ssaoBufferIds[1], ssaoBufferIds[0], PostProcessPass.BlurVertical);

            // Combine
            buffer.SetGlobalTexture(postProcessCombineTexId, ssaoBufferIds[0]);
            Draw(srcColor, dstColor, PostProcessPass.SSAOCombine);

            buffer.ReleaseTemporaryRT(ssaoBufferIds[0]);
            buffer.ReleaseTemporaryRT(ssaoBufferIds[1]);

            buffer.EndSample("SSAO");

            ExecuteBuffer();
        }

        private void ApplyOutline(RenderTargetIdentifier srcColor, RenderTargetIdentifier srcDepth, RenderTargetIdentifier srcNormal, RenderTargetIdentifier dstColor)
        {
            buffer.BeginSample("Outline");

            buffer.SetGlobalColor(outlineColorId, settings.outline.color);
            buffer.SetGlobalVector(outlineParamId, new(settings.outline.normalIntensity, settings.outline.normalBias, settings.outline.depthIntensity, settings.outline.depthBias));
            if (settings.outline.useDepthFade)
            {
                float range = Mathf.Max(0.001f, settings.outline.depthFadeEnd - settings.outline.depthFadeBegin);
                buffer.SetGlobalVector(outlineDepthFadeId, new(settings.outline.depthFadeBegin, range, 0, 0));
            }
            else
            {
                buffer.SetGlobalVector(outlineDepthFadeId, new(camera.farClipPlane, 1, 0, 0));
            }
            buffer.SetGlobalTexture(postProcessDepthTexId, srcDepth);
            buffer.SetGlobalTexture(postProcessNormalTexId, srcNormal);

            Draw(srcColor, dstColor, PostProcessPass.Outline);

            buffer.EndSample("Outline");

            ExecuteBuffer();
        }

        private void ApplyDOF(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            buffer.BeginSample("Depth of Field");

            PostProcessSettings.DepthOfField depthOfField = settings.depthOfField;
            buffer.SetGlobalFloat(dofFocusDistanceId, depthOfField.focusDistance);
            buffer.SetGlobalFloat(dofFocusRangeId, depthOfField.focusRange);
            buffer.SetGlobalFloat(dofBokehRadiusId, depthOfField.bokehRadius);

            buffer.GetTemporaryRT(dofCOCBufferId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.RHalf);

#if true
            int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
#else
            int width = camera.pixelWidth, height = camera.pixelHeight;
#endif
            RenderTextureFormat format = RenderTextureFormat;
            buffer.GetTemporaryRT(dofBokeh0Id, width, height, 0, FilterMode.Bilinear, format);
            buffer.GetTemporaryRT(dofBokeh1Id, width, height, 0, FilterMode.Bilinear, format);

            buffer.SetGlobalTexture(dofCOCBufferId, dofCOCBufferId);
            Draw(src, dofCOCBufferId, PostProcessPass.DOFCalculateCOC);
            Draw(src, dofBokeh0Id, PostProcessPass.DOFPreFilter);
            Draw(dofBokeh0Id, dofBokeh1Id, PostProcessPass.DOFCalculateBokeh);
            Draw(dofBokeh1Id, dofBokeh0Id, PostProcessPass.DOFPostFilter);
            buffer.SetGlobalTexture(dofResultId, dofBokeh0Id);
            Draw(src, dst, PostProcessPass.DOFCombine);

            buffer.ReleaseTemporaryRT(dofBokeh0Id);
            buffer.ReleaseTemporaryRT(dofBokeh1Id);
            buffer.ReleaseTemporaryRT(dofCOCBufferId);

            buffer.EndSample("Depth of Field");

            ExecuteBuffer();
        }

        private void ApplyBloom(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            buffer.BeginSample("Bloom");

            PostProcessSettings.Bloom bloom = settings.bloom;
            int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;

            // Threshold
            Vector4 threshold;
            threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
            threshold.y = threshold.x * bloom.thresholdKnee;
            threshold.z = 2f * threshold.y;
            threshold.w = 0.25f / (threshold.y + 0.00001f);
            threshold.y -= threshold.x;
            buffer.SetGlobalVector(bloomThresholdId, threshold);

            // Prefilter
            RenderTextureFormat format = RenderTextureFormat;
            buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
            Draw(src, bloomPrefilterId, PostProcessPass.BloomPrefilter);
            width /= 2;
            height /= 2;

            // Downsample
            int fromId = bloomPrefilterId, toId = bloomPyramidId + 1;
            int i;
            for (i = 0; i < bloom.maxIterations; i++)
            {
                if (width < bloom.downscaleLimit || height < bloom.downscaleLimit)
                {
                    break;
                }

                int midId = toId - 1;
                buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
                buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
                Draw(fromId, midId, PostProcessPass.BlurHorizontal);
                Draw(midId, toId, PostProcessPass.BlurVertical);
                fromId = toId;
                toId += 2;
                width /= 2;
                height /= 2;
            }

            buffer.ReleaseTemporaryRT(bloomPrefilterId);

            buffer.SetGlobalBool(bloomBucibicUpsamplingId, bloom.bicubicUpsampling);

            // Select final pass
            PostProcessPass combinePass, finalPass;
            float finalIntensity;
            if (bloom.mode == PostProcessSettings.Bloom.Mode.Additive)
            {
                // Additive
                combinePass = finalPass = PostProcessPass.BloomAdd;
                buffer.SetGlobalFloat(bloomIntensityId, 1f);
                finalIntensity = bloom.intensity;
            }
            else
            {
                // Scatter
                combinePass = PostProcessPass.BloomScatter;
                finalPass = PostProcessPass.BloomScatterFinal;
                buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
                finalIntensity = Mathf.Min(bloom.intensity, 0.95f);
            }

            if (i > 1)
            {
                // Combine downsamples
                buffer.ReleaseTemporaryRT(fromId - 1);
                toId -= 5;
                for (i -= 1; i > 0; i--)
                {
                    buffer.SetGlobalTexture(postProcessCombineTexId, toId + 1);
                    Draw(fromId, toId, combinePass);
                    buffer.ReleaseTemporaryRT(fromId);
                    buffer.ReleaseTemporaryRT(toId + 1);
                    fromId = toId;
                    toId -= 2;
                }
            }
            else
            {
                buffer.ReleaseTemporaryRT(bloomPyramidId);
            }

            buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
            buffer.SetGlobalTexture(postProcessCombineTexId, src);

            Draw(fromId, dst, finalPass);
            buffer.ReleaseTemporaryRT(fromId);

            buffer.EndSample("Bloom");

            ExecuteBuffer();
        }

        private void ApplySMAA(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            buffer.BeginSample("SMAA");

            if (!smaaMaterial)
            {
                smaaMaterial = new(GameRenderPipelineAsset.Instance.Resources.smaaShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            buffer.GetTemporaryRT(smaaFlipId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);//, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None, camera.allowDynamicResolution);
            buffer.GetTemporaryRT(smaaFlopId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);//, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None, camera.allowDynamicResolution);

            buffer.SetGlobalTexture(smaaAreaTexId, GameRenderPipelineAsset.Instance.Resources.smaaAreaTexture);
            buffer.SetGlobalTexture(smaaSearchTexId, GameRenderPipelineAsset.Instance.Resources.smaaSearchTexture);
            buffer.SetGlobalVector(smaaRtMetrics, new Vector4(1.0f / camera.pixelWidth, 1.0f / camera.pixelHeight, camera.pixelWidth, camera.pixelHeight));

            buffer.BlitFrameBuffer(src, smaaFlipId, smaaMaterial, (int)SMAAPass.EdgeDetection + (int)settings.antiAlias.quality, true);
            buffer.BlitFrameBuffer(smaaFlipId, smaaFlopId, smaaMaterial, (int)SMAAPass.BlendWeights + (int)settings.antiAlias.quality, true);
            buffer.SetGlobalTexture(smaaBlendTexId, smaaFlopId);
            buffer.BlitFrameBuffer(src, dst, smaaMaterial, (int)SMAAPass.NeighborhoodBlending);

            buffer.ReleaseTemporaryRT(smaaFlipId);
            buffer.ReleaseTemporaryRT(smaaFlopId);

            buffer.EndSample("SMAA");

            ExecuteBuffer();
        }

        private void ApplyFXAA(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            buffer.BeginSample("FXAA");

            if (!fxaaMaterial)
            {
                fxaaMaterial = new(GameRenderPipelineAsset.Instance.Resources.fxaaShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            buffer.SetGlobalVector(fxaaInverseScreenSizeId, new Vector4(1.0f / camera.pixelWidth, 1.0f / camera.pixelHeight));

            buffer.BlitFrameBuffer(src, dst, fxaaMaterial, (int)settings.antiAlias.quality);

            buffer.EndSample("FXAA");

            ExecuteBuffer();
        }


        private List<PostProcessEffect> GetCustomEffectsList(PostProcessEffectOrder order) => customEffects[(int)order];
        private void ClearCustomEffectsList()
        {
            if (customEffects != null)
            {
                for (int i = 0; i < customEffects.Length; i++)
                {
                    customEffects[i].Clear();
                }
            }
        }
        private void ExecuteCustomEffectsList(PostProcessEffectOrder order)
        {
            if (customEffects != null)
            {
                foreach (PostProcessEffect effect in customEffects[(int)order])
                {
                    string name = effect.EffectName;
                    if (string.IsNullOrEmpty(name)) name = effect.GetType().Name;

                    buffer.BeginSample(name);
                    try
                    {
                        effect.Execute(buffer, currentSourceBuffer, GetNextBuffer());
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                    buffer.EndSample(name);
                    ExecuteBuffer();
                    SwapBuffers();
                }
            }
        }


        private void ExecuteEditorEffectsList(RenderTargetIdentifier srcDepth)
        {
            foreach (IEditorGizmoEffect effect in editorGizmoEffects)
            {
                if (!effect.Enabled) continue;

                string name = effect.GetType().Name;

                buffer.BeginSample(name);
                try
                {
                    effect.Execute(buffer, currentSourceBuffer, srcDepth, GetNextBuffer());
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
                buffer.EndSample(name);
                ExecuteBuffer();
                SwapBuffers();
            }
        }
    }*/
}