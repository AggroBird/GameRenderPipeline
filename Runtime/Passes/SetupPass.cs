using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class SetupPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(SetupPass));

        private static readonly RenderTargetIdentifier[] rtArray = new RenderTargetIdentifier[2];

        private OpaqueBufferOutputs opaqueBufferOutputs;
        private TextureHandle rtColorBuffer;
        private TextureHandle rtDepthBuffer;
        private TextureHandle rtNormalBuffer;
        private TextureHandle opaqueColorBuffer;
        private TextureHandle opaqueDepthBuffer;
        private Vector2Int rtBufferSize;
        private Camera camera;
        private CameraClearFlags clearFlags;

        private void Render(RenderGraphContext context)
        {
            CommandBuffer buffer = context.cmd;
            bool clearDepth = clearFlags <= CameraClearFlags.Depth;
            bool clearColor = clearFlags == CameraClearFlags.Color;
            Color backgroundColor = clearFlags == CameraClearFlags.Color ? camera.backgroundColor.ColorSpaceAdjusted() : Color.clear;

            if (opaqueBufferOutputs.And(OpaqueBufferOutputs.ColorAndDepth))
            {
                buffer.SetRenderTarget(
                    opaqueColorBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    opaqueDepthBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                buffer.ClearRenderTarget(true, true, Color.clear);
            }
            else
            {
                if (opaqueBufferOutputs.And(OpaqueBufferOutputs.Color))
                {
                    buffer.SetRenderTarget(opaqueColorBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    buffer.ClearRenderTarget(true, true, Color.clear);
                }
                if (opaqueBufferOutputs.And(OpaqueBufferOutputs.Depth))
                {
                    buffer.SetRenderTarget(opaqueDepthBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    buffer.ClearRenderTarget(true, true, Color.clear);
                }
            }

            // Create regular buffers (these will be render targets if no normals are output)
            context.renderContext.SetupCameraProperties(camera);
            buffer.SetRenderTarget(
                rtColorBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                rtDepthBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(clearDepth, clearColor, backgroundColor);

            bool outputNormals = opaqueBufferOutputs.And(OpaqueBufferOutputs.Normal);
            context.cmd.SetKeyword(CameraRenderer.OutputNormalsKeyword, outputNormals);
            if (outputNormals)
            {
                // Also create normal buffer and add as render target
                buffer.SetRenderTarget(rtNormalBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                buffer.ClearRenderTarget(true, true, Color.clear);
                rtArray[0] = rtColorBuffer;
                rtArray[1] = rtNormalBuffer;
                buffer.SetRenderTarget(rtArray, rtDepthBuffer);
            }
        }

        public static CameraRendererTextures Record(RenderGraph renderGraph, Camera camera, OpaqueBufferOutputs opaqueBufferOutputs, GraphicsFormat colorFormat, Vector2Int bufferSize, DepthBits depthBufferBits)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out SetupPass pass, sampler);

            pass.rtBufferSize = bufferSize;
            pass.camera = camera;
            pass.opaqueBufferOutputs = opaqueBufferOutputs;
            pass.clearFlags = (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.SceneView) ? CameraClearFlags.SolidColor : camera.clearFlags;

            var colorTextureDesc = new TextureDesc(bufferSize.x, bufferSize.y)
            {
                name = "Render Target (Color)",
                colorFormat = colorFormat,
            };
            pass.rtColorBuffer = builder.WriteTexture(renderGraph.CreateTexture(colorTextureDesc));

            var depthTextureDesc = new TextureDesc(bufferSize.x, bufferSize.y)
            {
                name = "Render Target (Depth)",
                colorFormat = GraphicsFormat.None,
                depthBufferBits = depthBufferBits,
            };
            pass.rtDepthBuffer = builder.WriteTexture(renderGraph.CreateTexture(depthTextureDesc));

            TextureHandle opaqueColorBuffer = default;
            if (opaqueBufferOutputs.And(OpaqueBufferOutputs.Color))
            {
                colorTextureDesc.name = "Opaque Output Buffer (Color)";
                pass.opaqueColorBuffer = opaqueColorBuffer = builder.WriteTexture(renderGraph.CreateTexture(colorTextureDesc));
            }

            TextureHandle opaqueDepthBuffer = default;
            if (opaqueBufferOutputs.And(OpaqueBufferOutputs.Depth))
            {
                depthTextureDesc.name = "Opaque Output Buffer (Depth)";
                pass.opaqueDepthBuffer = opaqueDepthBuffer = builder.WriteTexture(renderGraph.CreateTexture(depthTextureDesc));
            }

            TextureHandle rtNormalBuffer = default;
            if (opaqueBufferOutputs.And(OpaqueBufferOutputs.Normal))
            {
                var normalTextureDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    name = "Render Target (Normals)",
                    colorFormat = GraphicsFormat.R16G16_SFloat,
                };
                pass.rtNormalBuffer = rtNormalBuffer = builder.WriteTexture(renderGraph.CreateTexture(normalTextureDesc));
            }

            builder.AllowPassCulling(false);
            builder.SetRenderFunc<SetupPass>(static (pass, context) => pass.Render(context));
            return new(colorFormat, pass.rtColorBuffer, pass.rtDepthBuffer, rtNormalBuffer, opaqueColorBuffer, opaqueDepthBuffer, bufferSize);
        }
    }
}