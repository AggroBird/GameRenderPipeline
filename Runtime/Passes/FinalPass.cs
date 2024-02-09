using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class FinalPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(FinalPass));

        private static readonly int
            postProcessInputTexId = Shader.PropertyToID("_PostProcessInputTex"),
            finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend"),
            finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");

        private static readonly Rect fullViewRect = new(0f, 0f, 1f, 1f);

        private TextureHandle rtColorBuffer;
        private Camera camera;
        private Material postProcessMaterial;

        void Render(RenderGraphContext context)
        {
            var srcBlend = BlendMode.One;
            var dstBlend = BlendMode.Zero;

            var buffer = context.cmd;
            buffer.SetGlobalFloat(finalSrcBlendId, (float)srcBlend);
            buffer.SetGlobalFloat(finalDstBlendId, (float)dstBlend);
            buffer.SetGlobalTexture(postProcessInputTexId, rtColorBuffer);
            buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, dstBlend == BlendMode.Zero && camera.rect == fullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            buffer.SetViewport(camera.pixelRect);
            buffer.DrawFullscreenEffect(postProcessMaterial, (int)PostProcessPass.Copy);
            buffer.SetGlobalFloat(finalSrcBlendId, 1);
            buffer.SetGlobalFloat(finalDstBlendId, 0);
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public static void Record(RenderGraph renderGraph, Camera camera, Material postProcessMaterial, in CameraRendererTextures textures)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out FinalPass pass, sampler);
            pass.rtColorBuffer = builder.ReadTexture(textures.rtColorBuffer);
            pass.camera = camera;
            pass.postProcessMaterial = postProcessMaterial;
            //pass.finalBlendMode = finalBlendMode;
            builder.SetRenderFunc<FinalPass>(static (pass, context) => pass.Render(context));
        }
    }
}
