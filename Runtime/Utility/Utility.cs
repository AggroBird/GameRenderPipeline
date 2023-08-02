using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    public static class CameraUtility
    {
        public static Camera CurrentCamera { get; internal set; }
    }

    internal static class Tags
    {
        public const string MainCameraTag = "MainCamera";
    }

    internal enum BlitRenderTargetPass
    {
        Color,
        Depth,
        ColorAndDepth,
    }

    public static class CommandBufferUtility
    {
        private static Material blitDepthMaterialInstance = default;
        private static Material BlitDepthMaterial
        {
            get
            {
                if (!blitDepthMaterialInstance)
                {
                    blitDepthMaterialInstance = new(GameRenderPipelineAsset.Instance.Resources.blitRenderTargetShader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
                return blitDepthMaterialInstance;
            }
        }

        private static readonly int
            blitColorTexId = Shader.PropertyToID("_MainTex"),
            blitDepthTexId = Shader.PropertyToID("_DepthTex");

        public static void BlitFrameBuffer(this CommandBuffer commandBuffer, RenderTargetIdentifier srcColor, RenderTargetIdentifier dst, bool clear = false)
        {
            commandBuffer.SetGlobalTexture(blitColorTexId, srcColor);
            commandBuffer.SetRenderTarget(dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            if (clear) commandBuffer.ClearRenderTarget(true, true, Color.clear);
            commandBuffer.DrawFullscreenEffect(BlitDepthMaterial, (int)BlitRenderTargetPass.Color);
        }
        public static void BlitFrameBuffer(this CommandBuffer commandBuffer, RenderTargetIdentifier srcColor, RenderTargetIdentifier srcDepth, RenderTargetIdentifier dst, bool clear = false)
        {
            commandBuffer.SetGlobalTexture(blitColorTexId, srcColor);
            commandBuffer.SetGlobalTexture(blitDepthTexId, srcDepth);
            commandBuffer.SetRenderTarget(dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            if (clear) commandBuffer.ClearRenderTarget(true, true, Color.clear);
            commandBuffer.DrawFullscreenEffect(BlitDepthMaterial, (int)BlitRenderTargetPass.ColorAndDepth);
        }
        public static void BlitDepthBuffer(this CommandBuffer commandBuffer, RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            commandBuffer.SetGlobalTexture(blitDepthTexId, src);
            commandBuffer.SetRenderTarget(dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.DrawFullscreenEffect(BlitDepthMaterial, (int)BlitRenderTargetPass.Depth);
        }
        public static void BlitFrameBuffer(this CommandBuffer commandBuffer, RenderTargetIdentifier srcColor, RenderTargetIdentifier srcDepth, RenderTargetIdentifier dstColor, RenderTargetIdentifier dstDepth, bool clear = false)
        {
            commandBuffer.SetGlobalTexture(blitColorTexId, srcColor);
            commandBuffer.SetGlobalTexture(blitDepthTexId, srcDepth);
            commandBuffer.SetRenderTarget(dstColor, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, dstDepth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            if (clear) commandBuffer.ClearRenderTarget(true, true, Color.clear);
            commandBuffer.DrawFullscreenEffect(BlitDepthMaterial, (int)BlitRenderTargetPass.ColorAndDepth);
        }
        public static void BlitFrameBuffer(this CommandBuffer commandBuffer, RenderTargetIdentifier srcColor, RenderTargetIdentifier dst, Material material, int pass, bool clear = false)
        {
            commandBuffer.SetGlobalTexture(blitColorTexId, srcColor);
            commandBuffer.SetRenderTarget(dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            if (clear) commandBuffer.ClearRenderTarget(true, true, Color.clear);
            commandBuffer.DrawFullscreenEffect(material, pass);
        }
        public static void BlitFrameBuffer(this CommandBuffer commandBuffer, RenderTargetIdentifier srcColor, RenderTargetIdentifier srcDepth, RenderTargetIdentifier dst, Material material, int pass, bool clear = false)
        {
            commandBuffer.SetGlobalTexture(blitColorTexId, srcColor);
            commandBuffer.SetGlobalTexture(blitDepthTexId, srcDepth);
            commandBuffer.SetRenderTarget(dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            if (clear) commandBuffer.ClearRenderTarget(true, true, Color.clear);
            commandBuffer.DrawFullscreenEffect(material, pass);
        }
        public static void BlitFrameBuffer(this CommandBuffer commandBuffer, RenderTargetIdentifier srcColor, RenderTargetIdentifier srcDepth, RenderTargetIdentifier dstColor, RenderTargetIdentifier dstDepth, Material material, int pass, bool clear = false)
        {
            commandBuffer.SetGlobalTexture(blitColorTexId, srcColor);
            commandBuffer.SetGlobalTexture(blitDepthTexId, srcDepth);
            commandBuffer.SetRenderTarget(dstColor, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, dstDepth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            if (clear) commandBuffer.ClearRenderTarget(true, true, Color.clear);
            commandBuffer.DrawFullscreenEffect(material, pass);
        }
        public static void DrawFullscreenEffect(this CommandBuffer commandBuffer, Material material, int pass)
        {
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }

        public static void SetGlobalBool(this CommandBuffer commandBuffer, int nameID, bool value)
        {
            commandBuffer.SetGlobalFloat(nameID, value ? 1f : 0f);
        }


        public static void SetKeywords(this CommandBuffer commandBuffer, GlobalKeyword[] keywords, int enabled)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (i == enabled)
                {
                    commandBuffer.EnableKeyword(keywords[i]);
                }
                else
                {
                    commandBuffer.DisableKeyword(keywords[i]);
                }
            }
        }
    }

    internal enum ShaderPathID
    {
        Lit,
        Unlit,
        TerrainLit,
    }

    internal static class ShaderUtility
    {
        private static readonly string[] shaderPaths =
        {
            "GRP/Lit",
            "GRP/Unlit",
            "GRP/TerrainLit",
        };

        public static string GetShaderPathName(ShaderPathID id)
        {
            int index = (int)id;
            if (index < 0 && index >= shaderPaths.Length)
            {
                Debug.LogError("Invalid Shader ID");
                return string.Empty;
            }
            return shaderPaths[index];
        }
        public static ShaderPathID GetShaderPathID(string path)
        {
            var index = Array.FindIndex(shaderPaths, m => m == path);
            return (ShaderPathID)index;
        }
    }

    internal static class TextureUtility
    {
        private static Color32[] colorBuffer = null;

        public static void RenderGradientToTexture(ref Texture2D texture, Gradient gradient)
        {
            if (!texture)
            {
                texture = new(256, 1, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = gradient.mode == GradientMode.Fixed ? FilterMode.Point : FilterMode.Bilinear
                };
            }
            RenderGradientToTexture(texture, gradient);
        }
        public static void RenderGradientToTexture(Texture2D texture, Gradient gradient)
        {
            int size = texture.width;

            if (colorBuffer == null || colorBuffer.Length != size)
            {
                colorBuffer = new Color32[size];
            }

            float step = 1.0f / (size - 1);
            float t = 0;
            for (int i = 0; i < size; i++, t += step)
            {
                colorBuffer[i] = gradient.Evaluate(t);
            }
            texture.SetPixels32(colorBuffer);
            texture.Apply();
        }
    }

    internal static class LinearColorUtility
    {
        public static Color ColorSpaceAdjusted(this Color color)
        {
            return GameRenderPipeline.LinearColorSpace ? color.linear : color;
        }
    }

    public readonly ref struct CommandBufferScope
    {
        public CommandBufferScope(string name)
        {
            commandBuffer = CommandBufferPool.Get(name);
        }

        public readonly CommandBuffer commandBuffer;

        public static implicit operator CommandBuffer(CommandBufferScope scope)
        {
            return scope.commandBuffer;
        }

        public void Dispose()
        {
            CommandBufferPool.Release(commandBuffer);
        }
    }
}
