using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class PostTransparencyPostProcessPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(PostTransparencyPostProcessPass));

        private PostProcessStack postProcessStack;
        private TextureHandle rtColorBuffer;

        private void Render(RenderGraphContext context)
        {
            postProcessStack.ApplyPostTransparency(context, rtColorBuffer, rtColorBuffer);
        }

        public static void Record(RenderGraph renderGraph, PostProcessStack stack, in CameraRendererTextures cameraTextures)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out PostTransparencyPostProcessPass pass, sampler);
            pass.postProcessStack = stack;
            pass.rtColorBuffer = builder.ReadWriteTexture(cameraTextures.rtColorBuffer);
            builder.SetRenderFunc<PostTransparencyPostProcessPass>(static (pass, context) => pass.Render(context));
        }
    }
}