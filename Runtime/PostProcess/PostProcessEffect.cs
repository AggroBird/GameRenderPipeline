using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GRP
{
    public enum PostProcessEffectOrder
    {
        BeforeBloom,
        BeforeColorGrading,
        BeforeAntiAlias,
        BeforeDisplay,
    }

    [RequireComponent(typeof(PostProcessCamera))]
    public abstract class PostProcessEffect : MonoBehaviour
    {
        internal static readonly int OrderCount = System.Enum.GetValues(typeof(PostProcessEffectOrder)).Length;

        public virtual PostProcessEffectOrder order => PostProcessEffectOrder.BeforeColorGrading;
        public virtual int priority => 0;
        internal const string DefaultEffectName = "CustomPostProcessEffect";
        public virtual string effectName => DefaultEffectName;

        public abstract void Execute(CommandBuffer buffer, RenderTargetIdentifier src, RenderTargetIdentifier dst);

        private void Start()
        {

        }
    }
}