using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [System.Obsolete]
    public class LEGACY_EnvironmentReference : LEGACY_EnvironmentComponent
    {
        [SerializeField] private LEGACY_EnvironmentComponent environment = default;
        public override EnvironmentSettings GetEnvironmentSettings()
        {
            if (environment)
            {
                IsDirty |= environment.IsDirty;
                environment.IsDirty = false;
                return environment.GetEnvironmentSettings();
            }
            return null;
        }
    }
}