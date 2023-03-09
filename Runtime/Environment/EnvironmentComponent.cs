using UnityEngine;

namespace AggroBird.GRP
{
    public class EnvironmentComponent : Environment
    {
        [SerializeField] private EnvironmentSettings environmentSettings = default;
        public override EnvironmentSettings EnvironmentSettings => environmentSettings;
    }
}