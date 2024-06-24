using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    public static class CameraUtility
    {
        public static Camera CurrentCamera { get; internal set; }
    }

    public static class GameRenderPipelineUtility
    {
        // Only valid during rendering
        public static GraphicsFormat ColorFormat { get; internal set; }
        public static Vector2Int BufferSize { get; internal set; }

        public static void DisableFog(CommandBuffer commandBuffer)
        {
            foreach (var kw in CameraRenderer.fogModeKeywords)
            {
                commandBuffer.DisableKeyword(kw);
            }
        }

        private static readonly GlobalKeyword sceneLightOverrideKeyword = GlobalKeyword.Create("_SCENE_LIGHT_OVERRIDE");
        private static readonly int
            overrideLightColorId = Shader.PropertyToID("_OverrideLightColor"),
            overrideLightDirectionId = Shader.PropertyToID("_OverrideLightDirection"),
            overrideLightAmbientId = Shader.PropertyToID("_OverrideLightAmbient");

        public static void EnableSceneLightOverride(Color color, Vector3 direction, Color ambient)
        {
            Shader.EnableKeyword(sceneLightOverrideKeyword);
            Shader.SetGlobalVector(overrideLightColorId, color);
            Shader.SetGlobalVector(overrideLightDirectionId, -direction.normalized);
            Shader.SetGlobalVector(overrideLightAmbientId, ambient);
        }
        public static void DisableSceneLightOverride()
        {
            Shader.DisableKeyword(sceneLightOverrideKeyword);
        }

        public static void EnableSceneLightOverride(CommandBuffer commandBuffer, Color color, Vector3 direction, Color ambient)
        {
            commandBuffer.EnableKeyword(sceneLightOverrideKeyword);
            commandBuffer.SetGlobalVector(overrideLightColorId, color);
            commandBuffer.SetGlobalVector(overrideLightDirectionId, -direction.normalized);
            commandBuffer.SetGlobalVector(overrideLightAmbientId, ambient);
        }
        public static void DisableSceneLightOverride(CommandBuffer commandBuffer)
        {
            commandBuffer.DisableKeyword(sceneLightOverrideKeyword);
        }
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
        internal static Material BlitDepthMaterial
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

        private static readonly bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;

        private static readonly int
            blitColorTexId = Shader.PropertyToID("_Blit_ColorInput"),
            blitDepthTexId = Shader.PropertyToID("_Blit_DepthInput");

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

        public static void CopyOrBlitTexture(this CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            if (copyTextureSupported)
            {
                cmd.CopyTexture(src, dst);
            }
            else
            {
                cmd.BlitFrameBuffer(src, dst);
            }
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
}
