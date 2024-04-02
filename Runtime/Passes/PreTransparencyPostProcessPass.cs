using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class PreTransparencyPostProcessPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(PreTransparencyPostProcessPass));
        private static readonly ProfilingSampler gizmos = new("Editor Gizmos");

        private PostProcessStack postProcessStack;
        private TextureHandle rtColorBuffer;
        private TextureHandle rtDepthBuffer;
        private GraphicsFormat colorFormat;


        private void Render(RenderGraphContext context)
        {
            postProcessStack.RenderEditorGizmoEffects(context.cmd, rtColorBuffer, rtDepthBuffer, colorFormat);
        }

        public static void Record(RenderGraph renderGraph, PostProcessStack postProcessStack, in CameraRendererTextures cameraTextures)
        {
            using var _ = new RenderGraphProfilingScope(renderGraph, sampler);

            if (postProcessStack.postProcessEnabled)
            {
                var settings = postProcessStack.settings;
                if (settings.ambientOcclusion.enabled)
                {
                    SSAOPass.Record(renderGraph, postProcessStack, cameraTextures);
                }
            }

            if (postProcessStack.DrawGizmoEffects)
            {
                using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out PreTransparencyPostProcessPass pass, gizmos);
                pass.postProcessStack = postProcessStack;
                pass.rtColorBuffer = cameraTextures.rtColorBuffer;
                pass.rtDepthBuffer = cameraTextures.rtDepthBuffer;
                pass.colorFormat = cameraTextures.colorFormat;
                builder.SetRenderFunc<PreTransparencyPostProcessPass>(static (pass, context) => pass.Render(context));
            }
        }
    }
}