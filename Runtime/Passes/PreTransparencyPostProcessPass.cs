using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class PreTransparencyPostProcessPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(PreTransparencyPostProcessPass));

        public static void Record(RenderGraph renderGraph, PostProcessStack postProcessStack, in CameraRendererTextures cameraTextures)
        {
            if (postProcessStack.postProcessEnabled)
            {
                var settings = postProcessStack.settings;
                if (settings.ambientOcclusion.enabled)
                {
                    using var _ = new RenderGraphProfilingScope(renderGraph, sampler);

                    SSAOPass.Record(renderGraph, postProcessStack, cameraTextures);
                }
            }
        }
    }
}