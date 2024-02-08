using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    internal sealed partial class CameraRenderer
    {
        [System.Flags]
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

        private static readonly ShaderTagId[] legacyShaderTagIds =
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };
        private static Material errorMaterial = default;

        public void DrawUnsupportedShaders()
        {
            if (!errorMaterial)
            {
                errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(Camera)) { overrideMaterial = errorMaterial };
            for (int i = 1; i < legacyShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
            }
            var filteringSettings = FilteringSettings.defaultValue;
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

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

#else

        private const string BufferName = "GRP Render Camera";

#endif
    }
}