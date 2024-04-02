using UnityEngine;
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
        private Vector2Int sceneViewSize;


        private void Render(RenderGraphContext context)
        {
            var buffer = context.cmd;
            postProcessStack.RenderEditorGizmoEffects(buffer, rtColorBuffer, rtDepthBuffer);
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
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
                pass.rtColorBuffer = builder.ReadWriteTexture(cameraTextures.rtColorBuffer);
                pass.rtDepthBuffer = builder.ReadTexture(cameraTextures.rtDepthBuffer);
                pass.colorFormat = cameraTextures.colorFormat;
                pass.sceneViewSize = cameraTextures.bufferSize;
                builder.SetRenderFunc<PreTransparencyPostProcessPass>(static (pass, context) => pass.Render(context));
            }
        }
    }
}