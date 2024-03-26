using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    [RequireComponent(typeof(Camera)), ExecuteAlways]
    public class PostProcessCameraComponent : PostProcessComponent
    {
        public FinalBlendMode finalBlendMode = new()
        {
            source = BlendMode.One,
            destination = BlendMode.Zero,
        };

        public override FinalBlendMode GetFinalBlendMode() => finalBlendMode;

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