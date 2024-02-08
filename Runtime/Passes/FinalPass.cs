using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class FinalPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(FinalPass));

        private CameraRenderer renderer;

        void Render(RenderGraphContext context)
        {
            renderer.DrawFinal(CameraRenderer.ColorBufferId, BuiltinRenderTextureType.CameraTarget);
            renderer.ExecuteBuffer();
        }

        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out FinalPass pass, sampler);
            pass.renderer = renderer;
            //pass.finalBlendMode = finalBlendMode;
            builder.SetRenderFunc<FinalPass>((pass, context) => pass.Render(context));
        }
    }
}
