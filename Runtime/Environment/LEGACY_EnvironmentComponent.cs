using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [System.Obsolete]
    public abstract class LEGACY_EnvironmentComponent : MonoBehaviour
    {
        public abstract EnvironmentSettings GetEnvironmentSettings();

        public bool IsDirty { get; set; }


        protected virtual void OnValidate()
        {
            IsDirty = true;
        }
    }
}