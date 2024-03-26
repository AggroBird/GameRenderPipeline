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
            colorGradingEnabledId = Shader.PropertyToID("_ColorGradingEnabled");

        private static readonly int fxaaInverseScreenSizeId =
            Shader.PropertyToID("_FXAA_InverseScreenSize");

        private static readonly GraphicsFormat outputColorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);

        private bool applyFXAA;
        private PostProcessStack postProcessStack;
        private TextureHandle rtColorBuffer;
        private TextureHandle colorGradingOutput;
        private TextureHandle colorLUT;
        private Vector2Int bufferSize;
        private Vector2Int cameraPixelSize;


        private void Render(RenderGraphContext context)
        {
            var buffer = context.cmd;
            var settings = postProcessStack.settings;

            if (postProcessStack.postProcessEnabled)
            {
                buffer.SetGlobalBool(colorGradingEnabledId, true);
                buffer.SetGlobalTexture(colorGradingLUTId, colorLUT);

                if (applyFXAA)
                {
                    postProcessStack.Draw(context, rtColorBuffer, colorGradingOutput, PostProcessPass.Final);

                    // Apply FXAA after color grading
                    buffer.SetGlobalVector(fxaaInverseScreenSizeId, new Vector4(1.0f / cameraPixelSize.x, 1.0f / cameraPixelSize.y));
                    postProcessStack.DrawFinal(context, colorGradingOutput, postProcessStack.FXAAMaterial, (int)settings.antiAlias.quality);
                }
                else
                {
                    postProcessStack.DrawFinal(context, rtColorBuffer, PostProcessPass.Final);
                }
            }
            else
            {
                buffer.SetGlobalBool(colorGradingEnabledId, false);

                postProcessStack.DrawFinal(context, rtColorBuffer, PostProcessPass.Final);
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
            pass.cameraPixelSize = new Vector2Int(postProcessStack.camera.pixelWidth, postProcessStack.camera.pixelHeight);

            if (postProcessStack.postProcessEnabled)
            {
                pass.colorLUT = builder.ReadTexture(colorLUT);
            }

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

            pass.rtColorBuffer = builder.ReadTexture(rtColorBuffer);

            builder.SetRenderFunc<PostTransparencyPostProcessPass>(static (pass, context) => pass.Render(context));
        }
    }
}