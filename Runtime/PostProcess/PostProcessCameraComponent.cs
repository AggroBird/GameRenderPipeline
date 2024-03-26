using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    [RequireComponent(typeof(Camera)), ExecuteAlways]
    public class PostProcessCameraComponent : PostProcessComponent
    {
        public enum RenderScaleMode
        {
            Inherit,
            Multiply,
            Override,
        }

        [Space]
        [SerializeField]
        private FinalBlendMode finalBlendMode = new()
        {
            source = BlendMode.One,
            destination = BlendMode.Zero,
        };

        [Space, SerializeField]
        private RenderScaleMode renderScaleMode = RenderScaleMode.Inherit;
        [SerializeField, Range(GeneralSettings.RenderScaleMin, GeneralSettings.RenderScaleMax)]
        private float renderScale = 1;


        public override float GetRenderScale(float currentScale)
        {
            return renderScaleMode switch
            {
                RenderScaleMode.Override => renderScale,
                RenderScaleMode.Multiply => currentScale * renderScale,
                _ => currentScale,
            };
        }
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