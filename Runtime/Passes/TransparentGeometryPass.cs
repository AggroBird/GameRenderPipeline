using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class TransparentGeometryPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(TransparentGeometryPass));

        private static readonly ShaderTagId[] defaultShaderTags = { new("SRPDefaultUnlit"), new("GRPLit") };


        private RendererListHandle list;

        private void Render(RenderGraphContext context)
        {
            context.cmd.DrawRendererList(list);
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults, bool useLightsPerObject, bool outputOpaque, bool outputNormals, in CameraRendererTextures textures)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out TransparentGeometryPass pass, sampler);
            PerObjectData lightsPerObjectFlags = useLightsPerObject ? (PerObjectData.LightData | PerObjectData.LightIndices) : PerObjectData.None;
            pass.list = builder.UseRendererList(renderGraph.CreateRendererList(new RendererListDesc(defaultShaderTags, cullingResults, camera)
            {
                renderQueueRange = RenderQueueRange.transparent,
                sortingCriteria = SortingCriteria.CommonTransparent,
                rendererConfiguration = PerObjectData.ReflectionProbes | lightsPerObjectFlags,
            }));
            builder.ReadWriteTexture(textures.rtColorBuffer);
            builder.ReadWriteTexture(textures.rtDepthBuffer);
            if (outputOpaque)
            {
                builder.ReadTexture(textures.opaqueColorBuffer);
                builder.ReadTexture(textures.opaqueDepthBuffer);
                if (outputNormals)
                {
                    builder.ReadTexture(textures.rtNormalBuffer);
                }
            }
            builder.SetRenderFunc<TransparentGeometryPass>(static (pass, context) => pass.Render(context));
        }
    }
}