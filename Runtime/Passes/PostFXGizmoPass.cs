using System.Diagnostics;
using UnityEditor;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class PostFXGizmoPass
    {
#if UNITY_EDITOR
        private static readonly ProfilingSampler sampler = new(nameof(PostFXGizmoPass));

        private CameraRenderer renderer;

        public void Render(RenderGraphContext context)
        {
            if (Handles.ShouldRenderGizmos())
            {
                renderer.Buffer.BlitDepthBuffer(CameraRenderer.DepthBufferId, BuiltinRenderTextureType.CameraTarget);
                renderer.ExecuteBuffer();

                context.renderContext.DrawGizmos(renderer.Camera, GizmoSubset.PostImageEffects);
            }
        }
#endif

        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out PostFXGizmoPass pass, sampler);
                pass.renderer = renderer;
                builder.SetRenderFunc<PostFXGizmoPass>((pass, context) => pass.Render(context));
            }
#endif
        }
    }
}
