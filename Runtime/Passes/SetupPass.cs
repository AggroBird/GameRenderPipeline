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

        private bool outputOpaque;
        private bool outputNormals;
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

            if (outputOpaque)
            {
                buffer.SetRenderTarget(
                    opaqueColorBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    opaqueDepthBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                buffer.ClearRenderTarget(true, true, Color.clear);
            }

            // Create regular buffers (these will be render targets if no normals are output)
            context.renderContext.SetupCameraProperties(camera);
            buffer.SetRenderTarget(
                rtColorBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                rtDepthBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(clearDepth, clearColor, backgroundColor);

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

        public static CameraRendererTextures Record(RenderGraph renderGraph, Camera camera, bool outputOpaque, bool outputNormals, bool useHDR)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out SetupPass pass, sampler);

            Vector2Int rtBufferSize = new(camera.pixelWidth, camera.pixelHeight);

            pass.rtBufferSize = rtBufferSize;
            pass.camera = camera;
            pass.outputNormals = outputNormals;
            pass.clearFlags = (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.SceneView) ? CameraClearFlags.SolidColor : camera.clearFlags;

            var colorTextureDesc = new TextureDesc(rtBufferSize.x, rtBufferSize.y)
            {
                name = "Render Target (Color)",
                colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
            };
            pass.rtColorBuffer = builder.WriteTexture(renderGraph.CreateTexture(colorTextureDesc));

            var depthTextureDesc = new TextureDesc(rtBufferSize.x, rtBufferSize.y)
            {
                name = "Render Target (Depth)",
                colorFormat = GraphicsFormat.None,
                depthBufferBits = DepthBits.Depth32,
            };
            pass.rtDepthBuffer = builder.WriteTexture(renderGraph.CreateTexture(depthTextureDesc));

            TextureHandle opaqueColorBuffer = default;
            TextureHandle opaqueDepthBuffer = default;
            if (outputOpaque)
            {
                colorTextureDesc.name = "Opaque Output Buffer (Color)";
                pass.opaqueColorBuffer = opaqueColorBuffer = builder.WriteTexture(renderGraph.CreateTexture(colorTextureDesc));
                depthTextureDesc.name = "Opaque Output Buffer (Depth)";
                pass.opaqueDepthBuffer = opaqueDepthBuffer = builder.WriteTexture(renderGraph.CreateTexture(depthTextureDesc));
            }

            TextureHandle rtNormalBuffer = default;
            if (outputNormals)
            {
                var normalTextureDesc = new TextureDesc(rtBufferSize.x, rtBufferSize.y)
                {
                    name = "Render Target (Normals)",
                    colorFormat = GraphicsFormat.R16G16_SFloat,
                };
                pass.rtNormalBuffer = rtNormalBuffer = builder.WriteTexture(renderGraph.CreateTexture(normalTextureDesc));
            }

            builder.AllowPassCulling(false);
            builder.SetRenderFunc<SetupPass>(static (pass, context) => pass.Render(context));
            return new(pass.rtColorBuffer, pass.rtDepthBuffer, rtNormalBuffer, opaqueColorBuffer, opaqueDepthBuffer);
        }
    }
}
