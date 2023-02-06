using UnityEditor;
using UnityEngine;

namespace AggroBird.GRP
{
    internal partial class PostProcessStack
    {
        private bool TryFindPostProcessCamera(out PostProcessCamera postProcessCamera)
        {
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                postProcessCamera = null;

                if (!SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
                {
                    return false;
                }

                foreach (Camera camera in Object.FindObjectsOfType<Camera>())
                {
                    if (camera.tag == "MainCamera" && camera.TryGetComponent(out postProcessCamera) && postProcessCamera.enabled)
                    {
                        return true;
                    }
                }

                return false;
            }
#endif

            return camera.TryGetComponent(out postProcessCamera) && postProcessCamera.enabled;
        }
    }
}