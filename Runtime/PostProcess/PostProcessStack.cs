using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal partial class PostProcessStack
    {
        private const string bufferName = "Post Process";
        private readonly CommandBuffer buffer = new() { name = bufferName };

        private Material postProcessMaterial = default;
        private Material smaaMaterial = default;

        private ScriptableRenderContext context;
        private Camera camera;

        private PostProcessSettings settings = default;
        private BlendMode srcBlendMode, dstBlendMode;

        private bool postProcessEnabled;

        public bool SSAOEnabled
        {
            get
            {
                if (!postProcessEnabled) return false;
                return settings.ambientOcclusion.enabled;
            }
        }
        public bool OutlineEnabled
        {
            get
            {
                if (!postProcessEnabled) return false;
                return settings.outline.enabled;
            }
        }
        public bool DofEnabled
        {
            get
            {
                if (!postProcessEnabled) return false;

                PostProcessSettings.DepthOfField dof = settings.depthOfField;
                if (!dof.enabled) return false;

                return dof.bokehRadius > 0;
            }
        }
        public bool BloomEnabled
        {
            get
            {
                if (!postProcessEnabled) return false;

                PostProcessSettings.Bloom bloom = settings.bloom;
                if (!bloom.enabled) return false;

                int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
                return bloom.intensity > 0 && width >= bloom.downscaleLimit * 2 && height >= bloom.downscaleLimit * 2;
            }
        }
        public bool SMAAEnabled
        {
            get
            {
                if (!postProcessEnabled) return false;
                return settings.smaa.enabled;
            }
        }
        private bool DrawGizmoEffects => camera.cameraType == CameraType.SceneView && editorGizmoEffects.Count > 0;

        private bool useHDR = false;
        internal RenderTextureFormat RenderTextureFormat => useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;


        public enum Pass
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
            dofConstantScaleId = Shader.PropertyToID("_DOFConstantScale"),
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
            rtMetrics = Shader.PropertyToID("_SMAA_RTMetrics"),
            flipId = Shader.PropertyToID("_SMAA_Flip"),
            flopId = Shader.PropertyToID("_SMAA_Flop"),
            areaTexId = Shader.PropertyToID("_SMAA_AreaTex"),
            searchTexId = Shader.PropertyToID("_SMAA_SearchTex"),
            blendTexId = Shader.PropertyToID("_SMAA_BlendTex");


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

        internal interface IEditorGizmoEffect
        {
            bool Enabled { get; }
            int Priority { get; }

            void Execute(CommandBuffer buffer, RenderTargetIdentifier srcColor, RenderTargetIdentifier srcDepth, RenderTargetIdentifier dstColor);
        }
        internal static readonly List<IEditorGizmoEffect> editorGizmoEffects = new();


        public PostProcessStack()
        {
            bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
            for (int i = 1; i < MaxBloomPyramidLevels * 2; i++)
            {
                Shader.PropertyToID("_BloomPyramid" + i);
            }
        }


        public void Setup(ScriptableRenderContext context, Camera camera, bool useHDR, bool postProcessEnabled)
        {
            this.context = context;
            this.camera = camera;
            this.useHDR = useHDR;

            if (camera.TryGetCameraComponent(out PostProcessCamera postProcessCamera))
            {
                srcBlendMode = postProcessCamera.finalBlendMode.source;
                dstBlendMode = postProcessCamera.finalBlendMode.destination;
                settings = postProcessCamera.postProcessSettingsAsset ? postProcessCamera.postProcessSettingsAsset.settings : null;
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
                postProcessCamera.GetComponents(effectComponentBuffer);
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
                        if (effect != null && effect.enabled)
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

            // Ensure there is a post process material
            if (!postProcessMaterial)
            {
                postProcessMaterial = new(GameRenderPipelineAsset.Instance.Resources.postProcessShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

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

        public void ApplyPreTransparency(RenderTargetIdentifier srcColor, RenderTargetIdentifier srcNormal, RenderTargetIdentifier srcDepth, RenderTargetIdentifier dstColor)
        {
            if (postProcessEnabled)
            {
                if (SSAOEnabled || OutlineEnabled)
                {
                    currentSourceBuffer = srcColor;

                    if (SSAOEnabled)
                    {
                        ApplySSAO(currentSourceBuffer, srcNormal, srcDepth, GetNextBuffer());
                        SwapBuffers();
                    }

                    if (OutlineEnabled)
                    {
                        ApplyOutline(currentSourceBuffer, srcNormal, srcDepth, GetNextBuffer());
                        SwapBuffers();
                    }

                    if (DrawGizmoEffects)
                    {
                        ExecuteEditorEffectsList(srcDepth);
                    }

                    Draw(currentSourceBuffer, dstColor, Pass.Copy);
                    ExecuteBuffer();
                }
            }
            else if (DrawGizmoEffects)
            {
                currentSourceBuffer = srcColor;

                ExecuteEditorEffectsList(srcDepth);

                Draw(currentSourceBuffer, dstColor, Pass.Copy);
                ExecuteBuffer();
            }
        }
        public void ApplyPostTransparency(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
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
                if (SMAAEnabled)
                {
                    ApplySMAA(currentSourceBuffer, GetNextBuffer());
                    SwapBuffers();
                }

                ExecuteCustomEffectsList(PostProcessEffectOrder.BeforeDisplay);

                DrawFinal(currentSourceBuffer, dst);
            }
            else
            {
                buffer.SetGlobalBool(colorGradingEnabledId, false);
                DrawFinal(src, dst);
            }

            ExecuteBuffer();
        }

        private void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        private void ApplySSAO(RenderTargetIdentifier srcColor, RenderTargetIdentifier srcNormal, RenderTargetIdentifier srcDepth, RenderTargetIdentifier dstColor)
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
            buffer.DrawFullscreenEffect(postProcessMaterial, (int)Pass.SSAO);

            // Blur
            Draw(ssaoBufferIds[0], ssaoBufferIds[1], Pass.BlurHorizontal);
            Draw(ssaoBufferIds[1], ssaoBufferIds[0], Pass.BlurVertical);

            // Combine
            buffer.SetGlobalTexture(postProcessCombineTexId, ssaoBufferIds[0]);
            Draw(srcColor, dstColor, Pass.SSAOCombine);

            buffer.ReleaseTemporaryRT(ssaoBufferIds[0]);
            buffer.ReleaseTemporaryRT(ssaoBufferIds[1]);

            buffer.EndSample("SSAO");

            ExecuteBuffer();
        }

        private void ApplyOutline(RenderTargetIdentifier srcColor, RenderTargetIdentifier srcNormal, RenderTargetIdentifier srcDepth, RenderTargetIdentifier dstColor)
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

            Draw(srcColor, dstColor, Pass.Outline);

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
            Draw(src, dofCOCBufferId, Pass.DOFCalculateCOC);
            Draw(src, dofBokeh0Id, Pass.DOFPreFilter);
            Draw(dofBokeh0Id, dofBokeh1Id, Pass.DOFCalculateBokeh);
            Draw(dofBokeh1Id, dofBokeh0Id, Pass.DOFPostFilter);
            buffer.SetGlobalTexture(dofResultId, dofBokeh0Id);
            Draw(src, dst, Pass.DOFCombine);

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
            Draw(src, bloomPrefilterId, Pass.BloomPrefilter);
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
                Draw(fromId, midId, Pass.BlurHorizontal);
                Draw(midId, toId, Pass.BlurVertical);
                fromId = toId;
                toId += 2;
                width /= 2;
                height /= 2;
            }

            buffer.ReleaseTemporaryRT(bloomPrefilterId);

            buffer.SetGlobalBool(bloomBucibicUpsamplingId, bloom.bicubicUpsampling);

            // Select final pass
            Pass combinePass, finalPass;
            float finalIntensity;
            if (bloom.mode == PostProcessSettings.Bloom.Mode.Additive)
            {
                // Additive
                combinePass = finalPass = Pass.BloomAdd;
                buffer.SetGlobalFloat(bloomIntensityId, 1f);
                finalIntensity = bloom.intensity;
            }
            else
            {
                // Scatter
                combinePass = Pass.BloomScatter;
                finalPass = Pass.BloomScatterFinal;
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

        private void ApplyColorGrading(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            int lutHeight = (int)settings.general.colorLUTResolution;
            int lutWidth = lutHeight * lutHeight;

            buffer.BeginSample("Render Color Grading LUT");
            {
                // Color adjustment
                PostProcessSettings.ColorAdjustments colorAdjustments = settings.colorAdjustments;
                buffer.SetGlobalVector(colorAdjustmentsId, new(
                    Mathf.Pow(2f, colorAdjustments.postExposure),
                    colorAdjustments.contrast * 0.01f + 1f,
                    colorAdjustments.hueShift * (1f / 360f),
                    colorAdjustments.saturation * 0.01f + 1f
                ));
                buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);

                // White balance
                PostProcessSettings.WhiteBalance whiteBalance = settings.whiteBalance;
                buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature, whiteBalance.tint));

                // Split toning
                PostProcessSettings.SplitToning splitToning = settings.splitToning;
                Color splitColor = splitToning.shadows;
                splitColor.a = splitToning.balance * 0.01f;
                buffer.SetGlobalColor(splitToningShadowsId, splitColor);
                buffer.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);

                // Channel mixer
                PostProcessSettings.ChannelMixer channelMixer = settings.channelMixer;
                buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
                buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
                buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);

                // Shadow midtones
                PostProcessSettings.ShadowsMidtonesHighlights smh = settings.shadowsMidtonesHighlights;
                buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
                buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
                buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
                buffer.SetGlobalVector(smhRangeId, new(smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd));

                // Vignette
                buffer.SetGlobalVector(vignetteParamId, new(settings.vignette.enabled ? 1f : 0f, camera.aspect, settings.vignette.falloff));

                buffer.GetTemporaryRT(colorGradingLUTId, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
                buffer.SetGlobalVector(colorGradingLUTParametersId, new(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)));
                PostProcessSettings.ToneMapping.Mode mode = settings.toneMapping.mode;
                Pass pass = Pass.ColorGradingNone + (int)mode;
                buffer.SetGlobalBool(colorGradingLUTInLogCId, useHDR && pass != Pass.ColorGradingNone);
                Draw(src, colorGradingLUTId, pass);
            }
            buffer.EndSample("Render Color Grading LUT");

            buffer.BeginSample("Apply Color Grading LUT");
            {
                buffer.SetGlobalVector(colorGradingLUTParametersId, new(1f / lutWidth, 1f / lutHeight, lutHeight - 1f));
                Draw(src, dst, Pass.Final);
                buffer.ReleaseTemporaryRT(colorGradingLUTId);
            }
            buffer.EndSample("Apply Color Grading LUT");

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

            buffer.GetTemporaryRT(flipId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);//, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None, camera.allowDynamicResolution);
            buffer.GetTemporaryRT(flopId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);//, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None, camera.allowDynamicResolution);

            buffer.SetGlobalTexture(areaTexId, GameRenderPipelineAsset.Instance.Resources.smaaAreaTexture);
            buffer.SetGlobalTexture(searchTexId, GameRenderPipelineAsset.Instance.Resources.smaaSearchTexture);
            buffer.SetGlobalVector(rtMetrics, new Vector4(1.0f / camera.pixelWidth, 1.0f / camera.pixelHeight, camera.pixelWidth, camera.pixelHeight));

            buffer.BlitFrameBuffer(src, flipId, smaaMaterial, (int)SMAAPass.EdgeDetection + (int)settings.smaa.quality, true);
            buffer.BlitFrameBuffer(flipId, flopId, smaaMaterial, (int)SMAAPass.BlendWeights + (int)settings.smaa.quality, true);
            buffer.SetGlobalTexture(blendTexId, flopId);
            buffer.BlitFrameBuffer(src, dst, smaaMaterial, (int)SMAAPass.NeighborhoodBlending);

            buffer.ReleaseTemporaryRT(flipId);
            buffer.ReleaseTemporaryRT(flopId);

            buffer.EndSample("SMAA");

            ExecuteBuffer();
        }

        private void Draw(RenderTargetIdentifier src, RenderTargetIdentifier dst, Pass pass)
        {
            buffer.SetGlobalTexture(postProcessInputTexId, src);
            buffer.SetRenderTarget(dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.DrawFullscreenEffect(postProcessMaterial, (int)pass);
        }
        private void DrawFinal(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            buffer.BeginSample("Final Blit");

            buffer.SetGlobalFloat(finalSrcBlendId, (float)srcBlendMode);
            buffer.SetGlobalFloat(finalDstBlendId, (float)dstBlendMode);
            buffer.SetGlobalTexture(postProcessInputTexId, src);
            buffer.SetRenderTarget(dst, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            buffer.SetViewport(camera.pixelRect);
            buffer.DrawFullscreenEffect(postProcessMaterial, (int)Pass.Copy);

            buffer.EndSample("Final Blit");
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
                    if (string.IsNullOrEmpty(name)) name = PostProcessEffect.DefaultEffectName;
                    buffer.BeginSample(name);
                    try
                    {
                        effect.Execute(buffer, currentSourceBuffer, GetNextBuffer());
                        context.ExecuteCommandBuffer(buffer);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                    buffer.Clear();
                    buffer.EndSample(name);
                    SwapBuffers();
                }
            }
        }

        private void ExecuteEditorEffectsList(RenderTargetIdentifier srcDepth)
        {
            foreach (IEditorGizmoEffect effect in editorGizmoEffects)
            {
                if (!effect.Enabled) continue;

                try
                {
                    effect.Execute(buffer, currentSourceBuffer, srcDepth, GetNextBuffer());
                    context.ExecuteCommandBuffer(buffer);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
                buffer.Clear();
                SwapBuffers();
            }
        }
    }
}