using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class SetupPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(SetupPass));

        private CameraRenderer renderer;

        private void Render(RenderGraphContext context) => renderer.Setup();

        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out SetupPass pass, sampler);
            pass.renderer = renderer;
            builder.SetRenderFunc<SetupPass>((pass, context) => pass.Render(context));
        }
    }
}
