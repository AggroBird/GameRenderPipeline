using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GRP
{
    internal enum BlitRenderTargetPass
    {
        Color,
        Depth,
        ColorAndDepth,
    }

    public static class CommandBufferUtility
    {
        private static Material blitDepthMaterialInstance = default;
        private static Material blitDepthMaterial
        {
            get
            {
                if (!blitDepthMaterialInstance)
                {
                    blitDepthMaterialInstance = new Material(GameRenderPipelineAsset.Instance.Resources.blitRenderTargetShader);
                    blitDepthMaterialInstance.hideFlags = HideFlags.HideAndDontSave;
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
            commandBuffer.DrawFullscreenEffect(blitDepthMaterial, (int)BlitRenderTargetPass.Color);
        }
        public static void BlitFrameBuffer(this CommandBuffer commandBuffer, RenderTargetIdentifier srcColor, RenderTargetIdentifier srcDepth, RenderTargetIdentifier dst, bool clear = false)
        {
            commandBuffer.SetGlobalTexture(blitColorTexId, srcColor);
            commandBuffer.SetGlobalTexture(blitDepthTexId, srcDepth);
            commandBuffer.SetRenderTarget(dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            if (clear) commandBuffer.ClearRenderTarget(true, true, Color.clear);
            commandBuffer.DrawFullscreenEffect(blitDepthMaterial, (int)BlitRenderTargetPass.ColorAndDepth);
        }
        public static void BlitFrameBuffer(this CommandBuffer commandBuffer, RenderTargetIdentifier srcColor, RenderTargetIdentifier srcDepth, RenderTargetIdentifier dstColor, RenderTargetIdentifier dstDepth, bool clear = false)
        {
            commandBuffer.SetGlobalTexture(blitColorTexId, srcColor);
            commandBuffer.SetGlobalTexture(blitDepthTexId, srcDepth);
            commandBuffer.SetRenderTarget(dstColor, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, dstDepth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            if (clear) commandBuffer.ClearRenderTarget(true, true, Color.clear);
            commandBuffer.DrawFullscreenEffect(blitDepthMaterial, (int)BlitRenderTargetPass.ColorAndDepth);
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

        public static void SetKeywords(ShaderKeyword[] keywords, int enabled)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (i == enabled)
                {
                    Shader.EnableKeyword(keywords[i].name);
                }
                else
                {
                    Shader.DisableKeyword(keywords[i].name);
                }
            }
        }
        public static void SetKeywords(string[] keywords, int enabled)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (i == enabled)
                {
                    Shader.EnableKeyword(keywords[i]);
                }
                else
                {
                    Shader.DisableKeyword(keywords[i]);
                }
            }
        }

        public static void SetKeyword(ShaderKeyword keyword, bool enabled)
        {
            if (enabled)
            {
                Shader.EnableKeyword(keyword.name);
            }
            else
            {
                Shader.DisableKeyword(keyword.name);
            }
        }
        public static void SetKeyword(string keyword, bool enabled)
        {
            if (enabled)
            {
                Shader.EnableKeyword(keyword);
            }
            else
            {
                Shader.DisableKeyword(keyword);
            }
        }
    }

    internal static class TextureUtility
    {
        private static Color32[] colorBuffer = null;

        public static void RenderGradientToTexture(ref Texture2D texture, Gradient gradient, FilterMode filterMode = FilterMode.Bilinear)
        {
            if (!texture)
            {
                texture = new Texture2D(256, 1, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = filterMode;
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
        public static Color AdjustedColor(this Color color)
        {
            return GameRenderPipeline.linearColorSpace ? color.linear : color;
        }
    }

    public sealed class CommandBufferScope : IDisposable
    {
        public CommandBufferScope(string name)
        {
            commandBuffer = CommandBufferPool.Get(name);
        }

        public CommandBuffer commandBuffer { get; private set; }

        public static implicit operator CommandBuffer(CommandBufferScope scope)
        {
            return scope.commandBuffer;
        }

        public void Dispose()
        {
            CommandBufferPool.Release(commandBuffer);
            commandBuffer = null;
        }
    }
}