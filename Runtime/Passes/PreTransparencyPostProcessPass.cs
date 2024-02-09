using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class PreTransparencyPostProcessPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(PreTransparencyPostProcessPass));

        private PostProcessStack postProcessStack;
        private TextureHandle rtColorBuffer;
        private TextureHandle rtDepthBuffer;
        private TextureHandle rtNormalBuffer;

        private void Render(RenderGraphContext context)
        {
            postProcessStack.ApplyPreTransparency(context, rtColorBuffer, rtDepthBuffer, rtNormalBuffer, rtColorBuffer);
        }

        public static void Record(RenderGraph renderGraph, PostProcessStack stack, bool outputNormals, in CameraRendererTextures cameraTextures)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out PreTransparencyPostProcessPass pass, sampler);
            pass.postProcessStack = stack;
            pass.rtColorBuffer = builder.ReadWriteTexture(cameraTextures.rtColorBuffer);
            pass.rtDepthBuffer = builder.ReadTexture(cameraTextures.rtDepthBuffer);
            if (outputNormals)
            {
                pass.rtNormalBuffer = builder.ReadTexture(cameraTextures.rtNormalBuffer);
            }
            builder.SetRenderFunc<PreTransparencyPostProcessPass>(static (pass, context) => pass.Render(context));
        }
    }
}