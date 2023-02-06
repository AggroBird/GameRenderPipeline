using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GRP
{
    internal sealed class GameRenderPipeline : RenderPipeline
    {
        internal static bool linearColorSpace => QualitySettings.activeColorSpace == ColorSpace.Linear;

        private CameraRenderer cameraRenderer = new CameraRenderer();
        private GameRenderPipelineAsset pipelineAsset = default;

        public GameRenderPipeline(GameRenderPipelineAsset pipelineAsset)
        {
            this.pipelineAsset = pipelineAsset;

            LODGroup.crossFadeAnimationDuration = pipelineAsset.settings.general.crossFadeAnimationDuration;
            GraphicsSettings.useScriptableRenderPipelineBatching = pipelineAsset.settings.general.useSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = linearColorSpace;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                cameraRenderer.Render(context, cameras[i], i, pipelineAsset);
            }
        }
    }
}
