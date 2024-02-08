using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class CopyOpaqueBuffersPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(CopyOpaqueBuffersPass));

        private CameraRenderer renderer;

        private void Render(RenderGraphContext context)
        {
            context.cmd.BlitFrameBuffer(CameraRenderer.ColorBufferId, CameraRenderer.DepthBufferId, CameraRenderer.OpaqueColorBufferId, CameraRenderer.OpaqueDepthBufferId);
            context.cmd.SetGlobalTexture(CameraRenderer.OpaqueColorBufferId, CameraRenderer.OpaqueColorBufferId);
            context.cmd.SetGlobalTexture(CameraRenderer.OpaqueDepthBufferId, CameraRenderer.OpaqueDepthBufferId);
            if (renderer.OutputNormals)
            {
                context.cmd.SetGlobalTexture(CameraRenderer.OpaqueNormalBufferId, CameraRenderer.NormalBufferId);
            }
            // Transparent does not output normals, use only the default render targets
            if (renderer.OutputOpaque || renderer.OutputNormals)
            {
                context.cmd.SetKeyword(CameraRenderer.OutputNormalsKeyword, false);
                renderer.RestoreDefaultRenderTargets();
            }
            renderer.ExecuteBuffer();
        }

        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out CopyOpaqueBuffersPass pass, sampler);
            pass.renderer = renderer;
            builder.SetRenderFunc<CopyOpaqueBuffersPass>((pass, context) => pass.Render(context));
        }
    }
}
