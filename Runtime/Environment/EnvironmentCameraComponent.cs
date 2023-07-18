using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    public class EnvironmentCameraComponent : EnvironmentComponent
    {
        [SerializeField] private EnvironmentSettings environmentSettings = default;
        public override EnvironmentSettings GetEnvironmentSettings()
        {
            return environmentSettings;
        }
    }
}