using System.Collections.Generic;
using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [RequireComponent(typeof(Camera)), ExecuteAlways]
    public class PostProcessCameraComponent : PostProcessComponent
    {
#if UNITY_EDITOR
        internal static List<PostProcessCameraComponent> activeCameraComponents = new();

        protected override void OnEnable()
        {
            activeCameraComponents.Add(this);
        }
        protected override void OnDisable()
        {
            activeCameraComponents.Remove(this);
        }
#endif
    }
}