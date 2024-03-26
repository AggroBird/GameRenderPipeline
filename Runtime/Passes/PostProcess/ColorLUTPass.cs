using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class ColorLUTPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(ColorLUTPass));

        static readonly GraphicsFormat colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);

        private PostProcessStack postProcessStack;
        private TextureHandle colorLUT;
        private int lutWidth, lutHeight;

        private static readonly int
            colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments"),
            colorFilterId = Shader.PropertyToID("_ColorFilter"),
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
            colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"),
            colorGradingLUTInLogCId = Shader.PropertyToID("_ColorGradingLUTInLogC");


        private void Render(RenderGraphContext context)
        {
            var buffer = context.cmd;
            var settings = postProcessStack.settings;

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

            buffer.SetGlobalVector(colorGradingLUTParametersId, new(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)));
            PostProcessSettings.ToneMapping.Mode mode = settings.toneMapping.mode;
            PostProcessPass pass = PostProcessPass.ColorGradingNone + (int)mode;
            buffer.SetGlobalBool(colorGradingLUTInLogCId, postProcessStack.useHDR && pass != PostProcessPass.ColorGradingNone);

            buffer.SetRenderTarget(colorLUT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.DrawFullscreenEffect(postProcessStack.PostProcessMaterial, (int)pass);

            // Set for final sample
            buffer.SetGlobalVector(colorGradingLUTParametersId, new(1f / lutWidth, 1f / lutHeight, lutHeight - 1f));

            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public static TextureHandle Record(RenderGraph renderGraph, PostProcessStack postProcessStack)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out ColorLUTPass pass, sampler);
            pass.postProcessStack = postProcessStack;

            int lutHeight = pass.lutHeight = (int)postProcessStack.settings.general.colorLUTResolution;
            int lutWidth = pass.lutWidth = lutHeight * lutHeight;
            var desc = new TextureDesc(lutWidth, lutHeight)
            {
                colorFormat = colorFormat,
                name = "Color LUT"
            };
            pass.colorLUT = builder.WriteTexture(renderGraph.CreateTexture(desc));
            builder.SetRenderFunc<ColorLUTPass>(static (pass, context) => pass.Render(context));

            return pass.colorLUT;
        }
    }
}