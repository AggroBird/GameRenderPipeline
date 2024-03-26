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

        }

        public static void Record(RenderGraph renderGraph, PostProcessStack postProcessStack, bool outputNormals, in CameraRendererTextures cameraTextures)
        {
            //using var _ = new RenderGraphProfilingScope(renderGraph, sampler);



            /*using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out PreTransparencyPostProcessPass pass, sampler);
            pass.postProcessStack = postProcessStack;
            pass.rtColorBuffer = builder.ReadWriteTexture(cameraTextures.rtColorBuffer);
            pass.rtDepthBuffer = builder.ReadTexture(cameraTextures.rtDepthBuffer);
            if (outputNormals)
            {
                pass.rtNormalBuffer = builder.ReadTexture(cameraTextures.rtNormalBuffer);
            }
            builder.SetRenderFunc<PreTransparencyPostProcessPass>(static (pass, context) => pass.Render(context));*/
        }
    }
}