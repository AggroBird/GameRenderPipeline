using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    public enum PostProcessEffectOrder
    {
        BeforeBloom,
        BeforeColorGrading,
        BeforeAntiAlias,
        BeforeDisplay,
    }

    [RequireComponent(typeof(PostProcessComponent))]
    public abstract class PostProcessEffect : MonoBehaviour
    {
        internal static readonly int OrderCount = Enum.GetValues(typeof(PostProcessEffectOrder)).Length;

        public virtual PostProcessEffectOrder Order => PostProcessEffectOrder.BeforeColorGrading;
        public virtual int Priority => 0;

        public virtual string EffectName => GetType().Name;

        public abstract void Execute(CommandBuffer buffer, RenderTargetIdentifier src, RenderTargetIdentifier dst);

        private void Start()
        {
            // Empty start to show enabled toggle
        }
    }
}