using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class GameRenderPipeline : RenderPipeline
    {
        public const string PipelineName = "GameRenderPipeline";
        private readonly RenderGraph renderGraph = new("GRP Render Graph");

        internal static bool LinearColorSpace => QualitySettings.activeColorSpace == ColorSpace.Linear;

        private readonly CameraRenderer cameraRenderer = new();
        private readonly GameRenderPipelineAsset pipelineAsset = default;

        public GameRenderPipeline(GameRenderPipelineAsset pipelineAsset)
        {
            Shader.globalRenderPipeline = PipelineName;

            this.pipelineAsset = pipelineAsset;

            LODGroup.crossFadeAnimationDuration = pipelineAsset.Settings.general.crossFadeAnimationDuration;
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            GraphicsSettings.lightsUseLinearIntensity = LinearColorSpace;
        }

        private void RenderCamera(RenderGraph renderGraph, ScriptableRenderContext context, Camera camera, int cameraIndex)
        {
            BeginCameraRendering(context, camera);

            CameraUtility.CurrentCamera = camera;

            try
            {
                cameraRenderer.Render(renderGraph, context, camera, cameraIndex, pipelineAsset);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            CameraUtility.CurrentCamera = null;

            EndCameraRendering(context, camera);
        }
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            BeginContextRendering(context, cameras);

            for (int i = 0; i < cameras.Count; i++)
            {
                RenderCamera(renderGraph, context, cameras[i], i);
            }

            EndContextRendering(context, cameras);
            renderGraph.EndFrame();
        }
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            renderGraph.Cleanup();
        }
    }
}
