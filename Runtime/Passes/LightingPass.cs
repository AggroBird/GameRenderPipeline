using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class LightingPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(LightingPass));

        private CameraRenderer renderer;
        private Lighting lighting;
        private CullingResults cullingResults;
        private GameRenderPipelineSettings settings;

        private void Render(RenderGraphContext context) => lighting.Setup(renderer.Camera, context, cullingResults, settings);

        public static void Record(RenderGraph renderGraph, CameraRenderer renderer, Lighting lighting, CullingResults cullingResults, GameRenderPipelineSettings settings)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out LightingPass pass, sampler);
            pass.renderer = renderer;
            pass.lighting = lighting;
            pass.cullingResults = cullingResults;
            pass.settings = settings;
            builder.SetRenderFunc<LightingPass>((pass, context) => pass.Render(context));
        }
    }
}