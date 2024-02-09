using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class CopyOpaqueBuffersPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(CopyOpaqueBuffersPass));

        private bool outputNormals;
        private TextureHandle rtColorBuffer;
        private TextureHandle rtDepthBuffer;
        private TextureHandle rtNormalBuffer;
        private TextureHandle opaqueColorBuffer;
        private TextureHandle opaqueDepthBuffer;

        private void Render(RenderGraphContext context)
        {
            // TODO: Use buffer.CopyTexture

            var buffer = context.cmd;

            context.cmd.BlitFrameBuffer(rtColorBuffer, rtDepthBuffer, opaqueColorBuffer, opaqueDepthBuffer);
            context.cmd.SetGlobalTexture(CameraRenderer.OpaqueColorBufferId, opaqueColorBuffer);
            context.cmd.SetGlobalTexture(CameraRenderer.OpaqueDepthBufferId, opaqueDepthBuffer);
            if (outputNormals)
            {
                context.cmd.SetGlobalTexture(CameraRenderer.OpaqueNormalBufferId, rtNormalBuffer);
            }

            // Transparent does not output normals, use only the default render targets
            context.cmd.SetKeyword(CameraRenderer.OutputNormalsKeyword, false);
            buffer.SetRenderTarget(
                rtColorBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare,
                rtDepthBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);

            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public static void Record(RenderGraph renderGraph, bool outputNormals, in CameraRendererTextures cameraTextures)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out CopyOpaqueBuffersPass pass, sampler);
            pass.outputNormals = outputNormals;
            pass.rtColorBuffer = builder.ReadTexture(cameraTextures.rtColorBuffer);
            pass.rtDepthBuffer = builder.ReadTexture(cameraTextures.rtDepthBuffer);
            if (outputNormals)
            {
                pass.rtNormalBuffer = builder.ReadTexture(cameraTextures.rtNormalBuffer);
            }
            pass.opaqueColorBuffer = builder.WriteTexture(cameraTextures.opaqueColorBuffer);
            pass.opaqueDepthBuffer = builder.WriteTexture(cameraTextures.opaqueDepthBuffer);
            builder.SetRenderFunc<CopyOpaqueBuffersPass>(static (pass, context) => pass.Render(context));
        }
    }
}
