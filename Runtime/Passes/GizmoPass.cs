using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class GizmoPass
    {
#if UNITY_EDITOR
        private static readonly ProfilingSampler sampler = new(nameof(GizmoPass));

        private Camera camera;
        private TextureHandle rtDepthBuffer;

        public void Render(RenderGraphContext context)
        {
            var buffer = context.cmd;
            buffer.BlitDepthBuffer(rtDepthBuffer, BuiltinRenderTextureType.CameraTarget);
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            if (Handles.ShouldRenderGizmos())
            {
                context.renderContext.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.renderContext.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }
#endif

        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph renderGraph, Camera camera, in CameraRendererTextures textures)
        {
#if UNITY_EDITOR
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out GizmoPass pass, sampler);
            pass.camera = camera;
            pass.rtDepthBuffer = builder.ReadTexture(textures.rtDepthBuffer);
            builder.SetRenderFunc<GizmoPass>(static (pass, context) => pass.Render(context));
#endif
        }
    }
}