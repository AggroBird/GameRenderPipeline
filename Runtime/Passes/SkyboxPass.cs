using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class SkyboxPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(SkyboxPass));

        private static readonly int skyboxStaticCubemapId = Shader.PropertyToID("_SkyboxStaticCubemap");

        private Material defaultSkyboxMaterial;
        private EnvironmentSettings environmentSettings;

        private void Render(RenderGraphContext context)
        {
            var buffer = context.cmd;
            switch (environmentSettings.skyboxSettings.skyboxSource)
            {
                case EnvironmentSettings.SkyboxSource.Material:
                case EnvironmentSettings.SkyboxSource.Gradient:
                    bool useDefault = environmentSettings.skyboxSettings.skyboxSource == EnvironmentSettings.SkyboxSource.Gradient || !environmentSettings.skyboxSettings.skyboxMaterial;
                    Material mat = useDefault ? defaultSkyboxMaterial : environmentSettings.skyboxSettings.skyboxMaterial;
                    buffer.DrawFullscreenEffect(mat, 0);
                    break;
                case EnvironmentSettings.SkyboxSource.Cubemap:
                    buffer.SetGlobalTexture(skyboxStaticCubemapId, environmentSettings.skyboxSettings.skyboxCubemap);
                    buffer.DrawFullscreenEffect(defaultSkyboxMaterial, 1);
                    break;
            }
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public static void Record(RenderGraph renderGraph, Material defaultSkyboxMaterial, EnvironmentSettings environmentSettings, in CameraRendererTextures cameraTextures)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out SkyboxPass pass, sampler);
            pass.defaultSkyboxMaterial = defaultSkyboxMaterial;
            pass.environmentSettings = environmentSettings;
            builder.ReadWriteTexture(cameraTextures.rtColorBuffer);
            builder.ReadTexture(cameraTextures.rtDepthBuffer);
            builder.SetRenderFunc<SkyboxPass>(static (pass, context) => pass.Render(context));
        }
    }
}