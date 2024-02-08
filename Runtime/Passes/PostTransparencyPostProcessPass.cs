using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class PostTransparencyPostProcessPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(PostTransparencyPostProcessPass));

        private CameraRenderer renderer;

        private void Render(RenderGraphContext context)
        {
            renderer.PostProcessStack.ApplyPostTransparency(context, CameraRenderer.ColorBufferId, CameraRenderer.ColorBufferId);
        }

        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out PostTransparencyPostProcessPass pass, sampler);
            pass.renderer = renderer;
            builder.SetRenderFunc<PostTransparencyPostProcessPass>((pass, context) => pass.Render(context));
        }
    }
}