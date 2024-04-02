using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class SSAOPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(SSAOPass));

        private static readonly int
            ssaoParametersId = Shader.PropertyToID("_SSAOParameters");

        private static readonly int
            postProcessDepthTexId = Shader.PropertyToID("_PostProcessDepthTex"),
            postProcessNormalTexId = Shader.PropertyToID("_PostProcessNormalTex"),
            postProcessCombineTexId = Shader.PropertyToID("_PostProcessCombineTex");

        private PostProcessStack postProcessStack;
        private TextureHandle rtColorBuffer;
        private TextureHandle rtDepthBuffer;
        private TextureHandle rtNormalBuffer;
        private TextureHandle ssaoBuffer0;
        private TextureHandle ssaoBuffer1;
        private TextureHandle outputBuffer;


        private void Render(RenderGraphContext context)
        {
            var buffer = context.cmd;

            // AO
            PostProcessSettings.AmbientOcclusion ao = postProcessStack.settings.ambientOcclusion;
            buffer.SetGlobalVector(ssaoParametersId, new(ao.sampleCount, ao.radius, ao.intensity, 0));
            buffer.SetGlobalTexture(postProcessNormalTexId, rtNormalBuffer);
            buffer.SetGlobalTexture(postProcessDepthTexId, rtDepthBuffer);
            buffer.SetRenderTarget(ssaoBuffer0, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.DrawFullscreenEffect(postProcessStack.PostProcessMaterial, (int)PostProcessPass.SSAO);

            // Blur
            postProcessStack.Draw(context, ssaoBuffer0, ssaoBuffer1, PostProcessPass.BlurHorizontal);
            postProcessStack.Draw(context, ssaoBuffer1, ssaoBuffer0, PostProcessPass.BlurVertical);

            // Combine
            buffer.SetGlobalTexture(postProcessCombineTexId, ssaoBuffer0);
            postProcessStack.Draw(context, rtColorBuffer, outputBuffer, PostProcessPass.SSAOCombine);
            buffer.CopyOrBlitTexture(outputBuffer, rtColorBuffer);

            // Restore render target
            buffer.SetRenderTarget(rtColorBuffer, rtDepthBuffer);
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public static void Record(RenderGraph renderGraph, PostProcessStack postProcessStack, in CameraRendererTextures cameraTextures)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out SSAOPass pass, sampler);
            pass.postProcessStack = postProcessStack;
            pass.rtColorBuffer = builder.ReadWriteTexture(cameraTextures.rtColorBuffer);
            pass.rtDepthBuffer = builder.ReadTexture(cameraTextures.rtDepthBuffer);
            pass.rtNormalBuffer = builder.ReadTexture(cameraTextures.rtNormalBuffer);

            pass.ssaoBuffer0 = builder.CreateTransientTexture(new TextureDesc(cameraTextures.bufferSize.x, cameraTextures.bufferSize.y)
            {
                name = "SSAO Buffer 0",
                filterMode = FilterMode.Bilinear,
                colorFormat = GraphicsFormat.R16_SFloat,
            });
            pass.ssaoBuffer1 = builder.CreateTransientTexture(new TextureDesc(cameraTextures.bufferSize.x, cameraTextures.bufferSize.y)
            {
                name = "SSAO Buffer 1",
                filterMode = FilterMode.Bilinear,
                colorFormat = GraphicsFormat.R16_SFloat,
            });
            pass.outputBuffer = builder.CreateTransientTexture(new TextureDesc(cameraTextures.bufferSize.x, cameraTextures.bufferSize.y)
            {
                name = "SSAO Output Buffer",
                filterMode = FilterMode.Bilinear,
                colorFormat = SystemInfo.GetGraphicsFormat(postProcessStack.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
            });

            builder.SetRenderFunc<SSAOPass>(static (pass, context) => pass.Render(context));
        }
    }
}