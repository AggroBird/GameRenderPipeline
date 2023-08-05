﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class GameRenderPipeline : RenderPipeline
    {
        public const string PipelineName = "GameRenderPipeline";

        internal static bool LinearColorSpace => QualitySettings.activeColorSpace == ColorSpace.Linear;

        private readonly CameraRenderer cameraRenderer = new();
        private readonly GameRenderPipelineAsset pipelineAsset = default;

        public GameRenderPipeline(GameRenderPipelineAsset pipelineAsset)
        {
            Shader.globalRenderPipeline = PipelineName;

            this.pipelineAsset = pipelineAsset;

            LODGroup.crossFadeAnimationDuration = pipelineAsset.Settings.general.crossFadeAnimationDuration;
            GraphicsSettings.useScriptableRenderPipelineBatching = pipelineAsset.Settings.general.useSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = LinearColorSpace;
        }

        private void RenderCamera(ScriptableRenderContext context, Camera camera, int cameraIndex)
        {
            BeginCameraRendering(context, camera);

            CameraUtility.CurrentCamera = camera;

            try
            {
                cameraRenderer.Render(context, camera, cameraIndex, pipelineAsset);
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
                RenderCamera(context, cameras[i], i);
            }

            EndContextRendering(context, cameras);
        }
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            BeginFrameRendering(context, cameras);

            for (int i = 0; i < cameras.Length; i++)
            {
                RenderCamera(context, cameras[i], i);
            }

            EndFrameRendering(context, cameras);
        }
    }
}
