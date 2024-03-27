using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class PostTransparencyPostProcessPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(PostTransparencyPostProcessPass));
        private static readonly ProfilingSampler final = new("Final");

        private static readonly int
            colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT"),
            colorGradingEnabledId = Shader.PropertyToID("_ColorGradingEnabled"),
            vignetteParamId = Shader.PropertyToID("_VignetteParam"),
            bicubicRescaleEnabledId = Shader.PropertyToID("_BicubicRescaleEnabled");

        private static readonly int fxaaInverseScreenSizeId =
            Shader.PropertyToID("_FXAA_InverseScreenSize");

        private static readonly GraphicsFormat outputColorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);

        private enum RescaleMode
        {
            None,
            Linear,
            Bicubic,
        }

        private bool applyFXAA;
        private PostProcessStack postProcessStack;
        private TextureHandle rtColorBuffer;
        private TextureHandle colorGradingOutput;
        private TextureHandle rescaleOutput;
        private TextureHandle colorLUT;
        private Vector2Int bufferSize;
        private RescaleMode rescaleMode;


        private void Render(RenderGraphContext context)
        {
            var buffer = context.cmd;
            var settings = postProcessStack.settings;

            var srcBuffer = rtColorBuffer;
            PostProcessPass finalPass = PostProcessPass.FinalRescale;

            buffer.SetGlobalBool(colorGradingEnabledId, postProcessStack.postProcessEnabled);
            buffer.SetGlobalBool(bicubicRescaleEnabledId, rescaleMode == RescaleMode.Bicubic);

            if (postProcessStack.postProcessEnabled)
            {
                buffer.SetGlobalTexture(colorGradingLUTId, colorLUT);
                buffer.SetGlobalVector(vignetteParamId, new(settings.vignette.enabled ? 1f : 0f, postProcessStack.camera.aspect, settings.vignette.falloff));

                if (applyFXAA)
                {
                    // Apply color grading before FXAA
                    postProcessStack.Draw(context, srcBuffer, colorGradingOutput, PostProcessPass.ApplyColorGrading);
                    srcBuffer = colorGradingOutput;
                    buffer.SetGlobalVector(fxaaInverseScreenSizeId, new Vector4(1.0f / bufferSize.x, 1.0f / bufferSize.y));
                    finalPass = PostProcessPass.FXAALow + (int)settings.antiAlias.quality;
                }
                else
                {
                    finalPass = PostProcessPass.ApplyColorGrading;
                }
            }

            if (rescaleMode == RescaleMode.None)
            {
                postProcessStack.DrawFinal(context, srcBuffer, finalPass);
            }
            else
            {
                postProcessStack.Draw(context, srcBuffer, rescaleOutput, finalPass);
                postProcessStack.DrawFinal(context, rescaleOutput, PostProcessPass.FinalRescale);
            }

            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public static void Record(RenderGraph renderGraph, PostProcessStack postProcessStack, in CameraRendererTextures cameraTextures)
        {
            using var _ = new RenderGraphProfilingScope(renderGraph, sampler);

            TextureHandle rtColorBuffer = default;
            if (postProcessStack.postProcessEnabled && postProcessStack.settings.depthOfField.enabled)
            {
                rtColorBuffer = DepthOfFieldPass.Record(renderGraph, postProcessStack, cameraTextures);
            }
            else
            {
                rtColorBuffer = cameraTextures.rtColorBuffer;
            }

            TextureHandle colorLUT = postProcessStack.postProcessEnabled ? ColorLUTPass.Record(renderGraph, postProcessStack) : default;

            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out PostTransparencyPostProcessPass pass, final);
            pass.postProcessStack = postProcessStack;
            pass.bufferSize = cameraTextures.bufferSize;

            if (postProcessStack.postProcessEnabled)
            {
                pass.colorLUT = builder.ReadTexture(colorLUT);
            }

            pass.rtColorBuffer = builder.ReadTexture(rtColorBuffer);
            var bicubicRescalingMode = postProcessStack.bicubicRescalingMode;
            pass.rescaleMode = pass.bufferSize.x == postProcessStack.camera.pixelWidth ? RescaleMode.None : postProcessStack.bicubicRescalingMode switch
            {
                GeneralSettings.BicubicRescalingMode.UpAndDown => RescaleMode.Bicubic,
                GeneralSettings.BicubicRescalingMode.UpOnly => pass.bufferSize.x < postProcessStack.camera.pixelWidth ? RescaleMode.Bicubic : RescaleMode.Linear,
                _ => RescaleMode.Linear,
            };

            var settings = postProcessStack.settings;
            pass.applyFXAA = postProcessStack.postProcessEnabled && settings.antiAlias.enabled && settings.antiAlias.algorithm == PostProcessSettings.AntiAlias.Algorithm.FXAA;
            if (pass.applyFXAA)
            {
                pass.colorGradingOutput = builder.CreateTransientTexture(new TextureDesc(pass.bufferSize.x, pass.bufferSize.y)
                {
                    colorFormat = outputColorFormat,
                    name = "Color Grading Output",
                });
            }
            if (pass.rescaleMode != RescaleMode.None)
            {
                pass.rescaleOutput = builder.CreateTransientTexture(new TextureDesc(pass.bufferSize.x, pass.bufferSize.y)
                {
                    colorFormat = outputColorFormat,
                    name = "Scaled Result",
                });
            }

            builder.SetRenderFunc<PostTransparencyPostProcessPass>(static (pass, context) => pass.Render(context));
        }
    }
}