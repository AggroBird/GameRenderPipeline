using System.Collections.Generic;
using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [ExecuteAlways]
    public class PostProcessSceneComponent : PostProcessComponent
    {
        public int priority = 0;

        internal static readonly List<PostProcessSceneComponent> activeSceneComponents = new();

        protected override void OnEnable()
        {
            activeSceneComponents.Add(this);
        }
        protected override void OnDisable()
        {
            activeSceneComponents.Remove(this);
        }
    }
}