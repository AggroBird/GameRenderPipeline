using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class SkyboxPass
    {
        private static readonly ProfilingSampler sampler = new(nameof(SkyboxPass));

        private CameraRenderer renderer;
        private EnvironmentSettings environmentSettings;

        private void Render(RenderGraphContext context)
        {
            if (renderer.Camera.clearFlags == CameraClearFlags.Skybox)
            {
                switch (environmentSettings.skyboxSettings.skyboxSource)
                {
                    case EnvironmentSettings.SkyboxSource.Material:
                    case EnvironmentSettings.SkyboxSource.Gradient:
                        bool useDefault = environmentSettings.skyboxSettings.skyboxSource == EnvironmentSettings.SkyboxSource.Gradient || !environmentSettings.skyboxSettings.skyboxMaterial;
                        Material mat = useDefault ? renderer.DefaultSkyboxMaterial : environmentSettings.skyboxSettings.skyboxMaterial;
                        context.cmd.DrawFullscreenEffect(mat, 0);
                        break;
                    case EnvironmentSettings.SkyboxSource.Cubemap:
                        context.cmd.SetGlobalTexture(CameraRenderer.SkyboxStaticCubemapId, environmentSettings.skyboxSettings.skyboxCubemap);
                        context.cmd.DrawFullscreenEffect(renderer.DefaultSkyboxMaterial, 1);
                        break;
                }
                renderer.ExecuteBuffer();
            }
        }

        public static void Record(RenderGraph renderGraph, CameraRenderer renderer, EnvironmentSettings environmentSettings)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out SkyboxPass pass, sampler);
            pass.renderer = renderer;
            pass.environmentSettings = environmentSettings;
            builder.SetRenderFunc<SkyboxPass>((pass, context) => pass.Render(context));
        }
    }
}