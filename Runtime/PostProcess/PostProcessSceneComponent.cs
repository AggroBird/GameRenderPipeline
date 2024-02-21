using System.Collections.Generic;
using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [ExecuteAlways]
    public class PostProcessSceneComponent : PostProcessComponent
    {
        protected override void OnEnable()
        {
            activePostProcessComponents.Add(this);
        }
        protected override void OnDisable()
        {
            activePostProcessComponents.Remove(this);
        }
    }
}