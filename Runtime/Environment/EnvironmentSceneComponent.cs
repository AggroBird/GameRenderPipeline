using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [ExecuteAlways]
    public class EnvironmentSceneComponent : EnvironmentComponent
    {
        [SerializeField] private EnvironmentSettings environmentSettings;
        public override EnvironmentSettings GetEnvironmentSettings() => environmentSettings;


        protected override void OnEnable()
        {
            activeSceneEnvironment = this;
        }
    }
}