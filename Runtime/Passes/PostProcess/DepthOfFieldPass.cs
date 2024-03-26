using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class DepthOfFieldPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(DepthOfFieldPass));

        static readonly GraphicsFormat colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);

        private static readonly int
            dofCOCBufferId = Shader.PropertyToID("_DOFCOCBuffer"),
            dofResultId = Shader.PropertyToID("_DOFResult"),
            dofFocusDistanceId = Shader.PropertyToID("_DOFFocusDistance"),
            dofFocusRangeId = Shader.PropertyToID("_DOFFocusRange"),
            dofBokehRadiusId = Shader.PropertyToID("_DOFBokehRadius");

        private static readonly int
            postProcessDepthTexId = Shader.PropertyToID("_PostProcessDepthTex");

        private PostProcessStack postProcessStack;
        private TextureHandle rtColorBuffer;
        private TextureHandle rtDepthBuffer;
        private TextureHandle outputBuffer;

        private TextureHandle dofCOCBuffer;
        private TextureHandle dofBokeh0;
        private TextureHandle dofBokeh1;


        private void Render(RenderGraphContext context)
        {
            var buffer = context.cmd;
            var settings = postProcessStack.settings;

            buffer.SetGlobalTexture(postProcessDepthTexId, rtDepthBuffer);

            ref PostProcessSettings.DepthOfField depthOfField = ref settings.depthOfField;
            buffer.SetGlobalFloat(dofFocusDistanceId, depthOfField.focusDistance);
            buffer.SetGlobalFloat(dofFocusRangeId, depthOfField.focusRange);
            buffer.SetGlobalFloat(dofBokehRadiusId, depthOfField.bokehRadius);

            postProcessStack.Draw(context, rtColorBuffer, dofCOCBuffer, PostProcessPass.DOFCalculateCOC);
            buffer.SetGlobalTexture(dofCOCBufferId, dofCOCBuffer);
            postProcessStack.Draw(context, rtColorBuffer, dofBokeh0, PostProcessPass.DOFPreFilter);
            postProcessStack.Draw(context, dofBokeh0, dofBokeh1, PostProcessPass.DOFCalculateBokeh);
            postProcessStack.Draw(context, dofBokeh1, dofBokeh0, PostProcessPass.DOFPostFilter);
            buffer.SetGlobalTexture(dofResultId, dofBokeh0);
            postProcessStack.Draw(context, rtColorBuffer, outputBuffer, PostProcessPass.DOFCombine);

            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public static TextureHandle Record(RenderGraph renderGraph, PostProcessStack postProcessStack, in CameraRendererTextures cameraTextures)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out DepthOfFieldPass pass, sampler);
            pass.postProcessStack = postProcessStack;

            pass.rtColorBuffer = builder.ReadTexture(cameraTextures.rtColorBuffer);
            pass.rtDepthBuffer = builder.ReadTexture(cameraTextures.rtDepthBuffer);

            var format = SystemInfo.GetGraphicsFormat(postProcessStack.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);

            pass.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(cameraTextures.bufferSize.x, cameraTextures.bufferSize.y)
            {
                name = "Depth of Field Output",
                colorFormat = format,
            }));

            int width = cameraTextures.bufferSize.x / 2, height = cameraTextures.bufferSize.y / 2;
            pass.dofCOCBuffer = builder.CreateTransientTexture(new TextureDesc(width, height)
            {
                name = "DOF COC Buffer",
                filterMode = FilterMode.Bilinear,
                colorFormat = GraphicsFormat.R16_SFloat,
            });
            pass.dofBokeh0 = builder.CreateTransientTexture(new TextureDesc(width, height)
            {
                name = "DOF Bokeh 0",
                filterMode = FilterMode.Bilinear,
                colorFormat = format,
            });
            pass.dofBokeh1 = builder.CreateTransientTexture(new TextureDesc(width, height)
            {
                name = "DOF Bokeh 1",
                filterMode = FilterMode.Bilinear,
                colorFormat = format,
            });

            builder.SetRenderFunc<DepthOfFieldPass>(static (pass, context) => pass.Render(context));
            return pass.outputBuffer;
        }
    }
}