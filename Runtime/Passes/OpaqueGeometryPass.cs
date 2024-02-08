using PlasticGui;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class OpaqueGeometryPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(OpaqueGeometryPass));

        private static readonly ShaderTagId[] defaultShaderTags = { new("SRPDefaultUnlit"), new("GRPLit") };


        private RendererListHandle list;

        private void Render(RenderGraphContext context)
        {
            context.cmd.DrawRendererList(list);
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults, bool useLightsPerObject)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out OpaqueGeometryPass pass, sampler);
            PerObjectData lightsPerObjectFlags = useLightsPerObject ? (PerObjectData.LightData | PerObjectData.LightIndices) : PerObjectData.None;
            pass.list = builder.UseRendererList(renderGraph.CreateRendererList(new RendererListDesc(defaultShaderTags, cullingResults, camera)
            {
                renderQueueRange = RenderQueueRange.opaque,
                sortingCriteria = SortingCriteria.CommonOpaque,
                rendererConfiguration = PerObjectData.ReflectionProbes | lightsPerObjectFlags,
            }));
            builder.SetRenderFunc<OpaqueGeometryPass>((pass, context) => pass.Render(context));
        }
    }
}