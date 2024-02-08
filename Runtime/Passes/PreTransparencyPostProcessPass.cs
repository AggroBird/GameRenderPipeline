using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class PreTransparencyPostProcessPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(PreTransparencyPostProcessPass));

        private CameraRenderer renderer;

        private void Render(RenderGraphContext context)
        {
            renderer.PostProcessStack.ApplyPreTransparency(context, CameraRenderer.ColorBufferId, CameraRenderer.NormalBufferId, CameraRenderer.DepthBufferId, CameraRenderer.ColorBufferId);
        }

        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out PreTransparencyPostProcessPass pass, sampler);
            pass.renderer = renderer;
            builder.SetRenderFunc<PreTransparencyPostProcessPass>((pass, context) => pass.Render(context));
        }
    }
}