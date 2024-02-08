using System.Diagnostics;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class UnsupportedShadersPass
    {
#if UNITY_EDITOR
        private static readonly ProfilingSampler sampler = new(nameof(UnsupportedShadersPass));

        //private static readonly ShaderTagId[] legacyShaderTagIds =
        //{
        //    new("Always"),
        //    new("ForwardBase"),
        //    new("PrepassBase"),
        //    new("Vertex"),
        //    new("VertexLMRGBM"),
        //    new("VertexLM")
        //};
        //
        //private static Material errorMaterial;
        //
        //private RendererListHandle list;

        private CameraRenderer renderer;

        private void Render(RenderGraphContext context)
        {
            renderer.DrawUnsupportedShaders();
            //context.cmd.DrawRendererList(list);
            //context.renderContext.ExecuteCommandBuffer(context.cmd);
            //context.cmd.Clear();
        }
#endif

        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph renderGraph, CameraRenderer renderer, Camera camera, CullingResults cullingResults)
        {
#if UNITY_EDITOR
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out UnsupportedShadersPass pass, sampler);

            //if (!errorMaterial)
            //{
            //    errorMaterial = new(Shader.Find("Hidden/InternalErrorShader"));
            //}

            pass.renderer = renderer;
            //pass.list = renderGraph.CreateRendererList(new RendererListDesc(legacyShaderTagIds, cullingResults, camera)
            //{
            //    renderQueueRange = RenderQueueRange.all,
            //    overrideMaterial = errorMaterial,
            //});
            builder.SetRenderFunc<UnsupportedShadersPass>((pass, context) => pass.Render(context));
#endif
        }
    }
}