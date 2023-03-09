using UnityEngine;

namespace AggroBird.GRP
{
    public class EnvironmentReference : Environment
    {
        [SerializeField] private Environment environment = default;
        public override EnvironmentSettings EnvironmentSettings
        {
            get
            {
                if (environment)
                {
                    IsDirty |= environment.IsDirty;
                    environment.IsDirty = false;
                    return environment.EnvironmentSettings;
                }
                return null;
            }
        }
    }
}