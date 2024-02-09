using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed partial class CameraRenderer
    {
        [Flags]
        private enum ShowFlags
        {
            None = 0,
            Fog = 1,
            Skybox = 2,
            PostProcess = 4,
            All = Fog | Skybox | PostProcess,
        }
        private ShowFlags showFlags = ShowFlags.All;

        private bool ShowFog => (showFlags & ShowFlags.Fog) != ShowFlags.None;
        private bool ShowSkybox => (showFlags & ShowFlags.Skybox) != ShowFlags.None;
        private bool ShowPostProcess => (showFlags & ShowFlags.PostProcess) != ShowFlags.None;

        partial void PrepareSceneWindow();


#if UNITY_EDITOR
        partial void PrepareSceneWindow()
        {
            if (Camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(Camera);

                SceneView.SceneViewState viewState = SceneView.currentDrawingSceneView.sceneViewState;
                showFlags = ShowFlags.None;
                if (viewState.showFog) showFlags |= ShowFlags.Fog;
                if (viewState.showSkybox) showFlags |= ShowFlags.Skybox;
                if (viewState.imageEffectsEnabled) showFlags |= ShowFlags.PostProcess;
            }
            else
            {
                showFlags = ShowFlags.All;
            }
        }
#endif
    }
}