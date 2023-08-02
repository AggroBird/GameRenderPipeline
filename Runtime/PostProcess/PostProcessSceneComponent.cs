using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [ExecuteAlways]
    public sealed class PostProcessSceneComponent : PostProcessComponent
    {
        protected override void OnEnable()
        {
            activeScenePostProcess = this;
        }
    }
}