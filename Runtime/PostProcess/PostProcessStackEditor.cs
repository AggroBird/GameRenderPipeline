using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AggroBird.GRP
{
    internal partial class PostProcessStack
    {
        private static readonly List<GameObject> sceneObjects = new List<GameObject>();
        private static readonly List<Camera> sceneCameras = new List<Camera>();

        private bool TryFindPostProcessCamera(out PostProcessCamera postProcessCamera)
        {
            if (camera.TryGetComponent(out postProcessCamera) && postProcessCamera.enabled)
            {
                return true;
            }

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                Scene currentScene;
                if (camera.scene.IsValid())
                {
                    currentScene = camera.scene;
                }
                else
                {
                    currentScene = SceneManager.GetActiveScene();
                    if (!currentScene.IsValid())
                    {
                        return false;
                    }
                }

                currentScene.GetRootGameObjects(sceneObjects);
                foreach (GameObject sceneObject in sceneObjects)
                {
                    sceneObject.GetComponentsInChildren(false, sceneCameras);
                    foreach (Camera sceneCamera in sceneCameras)
                    {
                        if (sceneCamera == camera) continue;
                        if (!sceneCamera.gameObject.activeInHierarchy) continue;
                        if (!sceneCamera.enabled) continue;
                        if (sceneCamera.tag != "MainCamera") continue;
                        if (sceneCamera.TryGetComponent(out postProcessCamera) && postProcessCamera.enabled)
                        {
                            return true;
                        }
                    }
                }
            }
#endif

            return false;
        }
    }
}