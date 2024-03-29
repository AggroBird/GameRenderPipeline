using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class CopyOpaqueBuffersPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(CopyOpaqueBuffersPass));

        private GeneralSettings.OpaqueBufferOutputs opaqueBufferOutputs;
        private TextureHandle rtColorBuffer;
        private TextureHandle rtDepthBuffer;
        private TextureHandle rtNormalBuffer;
        private TextureHandle opaqueColorBuffer;
        private TextureHandle opaqueDepthBuffer;

        private void Render(RenderGraphContext context)
        {
            // TODO: Use buffer.CopyTexture

            var buffer = context.cmd;

            if (opaqueBufferOutputs.And(GeneralSettings.OpaqueBufferOutputs.ColorAndDepth))
            {
                context.cmd.BlitFrameBuffer(rtColorBuffer, rtDepthBuffer, opaqueColorBuffer, opaqueDepthBuffer);
                context.cmd.SetGlobalTexture(CameraRenderer.OpaqueColorBufferId, opaqueColorBuffer);
                context.cmd.SetGlobalTexture(CameraRenderer.OpaqueDepthBufferId, opaqueDepthBuffer);
            }
            else
            {
                if (opaqueBufferOutputs.And(GeneralSettings.OpaqueBufferOutputs.Color))
                {
                    context.cmd.BlitFrameBuffer(rtColorBuffer, opaqueColorBuffer);
                    context.cmd.SetGlobalTexture(CameraRenderer.OpaqueColorBufferId, opaqueColorBuffer);
                }
                if (opaqueBufferOutputs.And(GeneralSettings.OpaqueBufferOutputs.Depth))
                {
                    context.cmd.BlitFrameBuffer(rtDepthBuffer, opaqueDepthBuffer);
                    context.cmd.SetGlobalTexture(CameraRenderer.OpaqueDepthBufferId, opaqueDepthBuffer);
                }
            }

            if (opaqueBufferOutputs.And(GeneralSettings.OpaqueBufferOutputs.Normal))
            {
                context.cmd.SetGlobalTexture(CameraRenderer.OpaqueNormalBufferId, rtNormalBuffer);
            }

            // Transparent does not output normals, switch back to default render targets
            context.cmd.SetKeyword(CameraRenderer.OutputNormalsKeyword, false);
            buffer.SetRenderTarget(
                rtColorBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare,
                rtDepthBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);

            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public static void Record(RenderGraph renderGraph, GeneralSettings.OpaqueBufferOutputs opaqueBufferOutputs, in CameraRendererTextures cameraTextures)
        {
            if (opaqueBufferOutputs != GeneralSettings.OpaqueBufferOutputs.None)
            {
                using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out CopyOpaqueBuffersPass pass, sampler);
                pass.opaqueBufferOutputs = opaqueBufferOutputs;
                pass.rtColorBuffer = builder.ReadTexture(cameraTextures.rtColorBuffer);
                pass.rtDepthBuffer = builder.ReadTexture(cameraTextures.rtDepthBuffer);
                if (opaqueBufferOutputs.And(GeneralSettings.OpaqueBufferOutputs.Color))
                {
                    pass.opaqueColorBuffer = builder.WriteTexture(cameraTextures.opaqueColorBuffer);
                }
                if (opaqueBufferOutputs.And(GeneralSettings.OpaqueBufferOutputs.Depth))
                {
                    pass.opaqueDepthBuffer = builder.WriteTexture(cameraTextures.opaqueDepthBuffer);
                }
                if (opaqueBufferOutputs.And(GeneralSettings.OpaqueBufferOutputs.Normal))
                {
                    pass.rtNormalBuffer = builder.ReadTexture(cameraTextures.rtNormalBuffer);
                }
                builder.SetRenderFunc<CopyOpaqueBuffersPass>(static (pass, context) => pass.Render(context));
            }
        }
    }
}