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

        public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults, bool useLightsPerObject, GeneralSettings.OpaqueBufferOutputs opaqueBufferOutputs, in CameraRendererTextures cameraTextures, in ShadowTextures shadowTextures)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out TransparentGeometryPass pass, sampler);
            PerObjectData lightsPerObjectFlags = useLightsPerObject ? (PerObjectData.LightData | PerObjectData.LightIndices) : PerObjectData.None;
            pass.list = builder.UseRendererList(renderGraph.CreateRendererList(new RendererListDesc(defaultShaderTags, cullingResults, camera)
            {
                renderQueueRange = RenderQueueRange.transparent,
                sortingCriteria = SortingCriteria.CommonTransparent,
                rendererConfiguration = PerObjectData.ReflectionProbes | lightsPerObjectFlags,
            }));
            builder.ReadWriteTexture(cameraTextures.rtColorBuffer);
            builder.ReadWriteTexture(cameraTextures.rtDepthBuffer);
            if (opaqueBufferOutputs.And(GeneralSettings.OpaqueBufferOutputs.Color))
            {
                builder.ReadTexture(cameraTextures.opaqueColorBuffer);
            }
            if (opaqueBufferOutputs.And(GeneralSettings.OpaqueBufferOutputs.Depth))
            {
                builder.ReadTexture(cameraTextures.opaqueDepthBuffer);
            }
            if (opaqueBufferOutputs.And(GeneralSettings.OpaqueBufferOutputs.Normal))
            {
                builder.ReadTexture(cameraTextures.rtNormalBuffer);
            }
            if (shadowTextures)
            {
                builder.ReadTexture(shadowTextures.directionalAtlas);
                builder.ReadTexture(shadowTextures.otherAtlas);
            }
            builder.SetRenderFunc<TransparentGeometryPass>(static (pass, context) => pass.Render(context));
        }
    }
}