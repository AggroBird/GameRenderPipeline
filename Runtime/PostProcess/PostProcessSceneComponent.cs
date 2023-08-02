using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [ExecuteAlways]
    public class PostProcessSceneComponent : PostProcessComponent
    {
        protected override void OnEnable()
        {
            activeScenePostProcess = this;
        }
    }
}