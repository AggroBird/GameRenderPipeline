using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [System.Obsolete]
    public class LEGACY_EnvironmentCameraComponent : LEGACY_EnvironmentComponent
    {
        [SerializeField] private EnvironmentSettings environmentSettings = default;
        public override EnvironmentSettings GetEnvironmentSettings()
        {
            return environmentSettings;
        }
    }
}