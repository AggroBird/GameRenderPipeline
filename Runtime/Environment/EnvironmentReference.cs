using UnityEngine;

namespace AggroBird.GRP
{
    public class EnvironmentReference : EnvironmentComponent
    {
        [SerializeField] private EnvironmentComponent environment = default;
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