using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace AggroBird.GRP
{
    internal sealed partial class CameraRenderer
    {
        partial void PrepareBuffer();

        partial void DrawEditorGizmos(GizmoSubset gizmoSubset);

        partial void DrawUnsupportedShaders();

        private bool showFog = true;
        private bool showSkybox = true;

        partial void PrepareSceneWindow();


#if UNITY_EDITOR
        private string bufferName { get; set; }

        private static ShaderTagId[] legacyShaderTagIds =
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };
        private static Material errorMaterial = default;


        partial void PrepareBuffer()
        {
            Profiler.BeginSample("Allocate Buffer Name");
            buffer.name = bufferName = camera.name;
            Profiler.EndSample();
        }

        partial void DrawEditorGizmos(GizmoSubset gizmoSubset)
        {
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, gizmoSubset);
            }
        }

        partial void DrawUnsupportedShaders()
        {
            if (!errorMaterial)
            {
                errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)) { overrideMaterial = errorMaterial };
            for (int i = 1; i < legacyShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
            }
            var filteringSettings = FilteringSettings.defaultValue;
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        partial void PrepareSceneWindow()
        {
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);

                showFog = SceneView.currentDrawingSceneView.sceneViewState.showFog;
                showSkybox = SceneView.currentDrawingSceneView.sceneViewState.showSkybox;
            }
        }

#else

        private const string bufferName = "GRP Render Camera";
        private const bool hideEnvironment = false;

#endif
    }
}