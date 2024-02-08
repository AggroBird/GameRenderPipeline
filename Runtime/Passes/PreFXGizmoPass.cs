using System.Diagnostics;
using UnityEditor;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class PreFXGizmoPass
    {
#if UNITY_EDITOR
        private static readonly ProfilingSampler sampler = new(nameof(PreFXGizmoPass));

        private CameraRenderer renderer;

        public void Render(RenderGraphContext context)
        {
            if (Handles.ShouldRenderGizmos())
            {
                renderer.Buffer.BlitDepthBuffer(CameraRenderer.DepthBufferId, BuiltinRenderTextureType.CameraTarget);
                renderer.ExecuteBuffer();

                context.renderContext.DrawGizmos(renderer.Camera, GizmoSubset.PreImageEffects);
            }
        }
#endif

        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out PreFXGizmoPass pass, sampler);
                pass.renderer = renderer;
                builder.SetRenderFunc<PreFXGizmoPass>((pass, context) => pass.Render(context));
            }
#endif
        }
    }
}