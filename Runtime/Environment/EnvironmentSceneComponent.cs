using System.Collections.Generic;
using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [ExecuteAlways]
    public class EnvironmentSceneComponent : EnvironmentComponent
    {
        public int priority = 0;

        [SerializeField] private EnvironmentSettings environmentSettings;
        public override EnvironmentSettings GetEnvironmentSettings() => environmentSettings;

        internal static List<EnvironmentSceneComponent> activeSceneComponents = new();


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