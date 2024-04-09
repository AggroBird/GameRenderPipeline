using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class OutlinePass
    {
        private static readonly ProfilingSampler sampler = new(nameof(OutlinePass));

        private static readonly int
            outlineColorId = Shader.PropertyToID("_OutlineColor"),
            outlineParamId = Shader.PropertyToID("_OutlineParam"),
            outlineDepthFadeId = Shader.PropertyToID("_OutlineDepthFade");

        private static readonly int
            postProcessDepthTexId = Shader.PropertyToID("_PostProcessDepthTex"),
            postProcessNormalTexId = Shader.PropertyToID("_PostProcessNormalTex"),
            postProcessCombineTexId = Shader.PropertyToID("_PostProcessCombineTex");

        private PostProcessStack postProcessStack;

        private TextureHandle rtColorBuffer;
        private TextureHandle rtDepthBuffer;
        private TextureHandle rtNormalBuffer;
        private TextureHandle outputBuffer;

        /*private PostProcessStack postProcessStack;
        
        private TextureHandle ssaoBuffer0;
        private TextureHandle ssaoBuffer1;
        private TextureHandle outputBuffer;*/


        private void Render(RenderGraphContext context)
        {
            var buffer = context.cmd;
            ref var settings = ref postProcessStack.settings.outline;

            buffer.SetGlobalColor(outlineColorId, settings.color);
            buffer.SetGlobalVector(outlineParamId, new(settings.normalIntensity, settings.normalBias, settings.depthIntensity, settings.depthBias));
            if (settings.useDepthFade)
            {
                float range = Mathf.Max(0.001f, settings.depthFadeEnd - settings.depthFadeBegin);
                buffer.SetGlobalVector(outlineDepthFadeId, new(settings.depthFadeBegin, range, 0, 0));
            }
            else
            {
                buffer.SetGlobalVector(outlineDepthFadeId, new(postProcessStack.camera.farClipPlane, 1, 0, 0));
            }
            buffer.SetGlobalTexture(postProcessDepthTexId, rtDepthBuffer);
            buffer.SetGlobalTexture(postProcessNormalTexId, rtNormalBuffer);

            postProcessStack.Draw(context, rtColorBuffer, outputBuffer, PostProcessPass.Outline);
            buffer.CopyOrBlitTexture(outputBuffer, rtColorBuffer);

            // Restore render target
            buffer.SetRenderTarget(rtColorBuffer);
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public static void Record(RenderGraph renderGraph, PostProcessStack postProcessStack, in CameraRendererTextures cameraTextures)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out OutlinePass pass, sampler);
            pass.postProcessStack = postProcessStack;
            pass.rtColorBuffer = builder.ReadWriteTexture(cameraTextures.rtColorBuffer);
            pass.rtDepthBuffer = builder.ReadTexture(cameraTextures.rtDepthBuffer);
            pass.rtNormalBuffer = builder.ReadTexture(cameraTextures.rtNormalBuffer);

            pass.outputBuffer = builder.CreateTransientTexture(new TextureDesc(cameraTextures.bufferSize.x, cameraTextures.bufferSize.y)
            {
                name = "SSAO Output Buffer",
                filterMode = FilterMode.Bilinear,
                colorFormat = SystemInfo.GetGraphicsFormat(postProcessStack.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
            });

            builder.SetRenderFunc<OutlinePass>(static (pass, context) => pass.Render(context));
        }
    }
}