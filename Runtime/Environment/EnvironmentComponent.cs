using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [DisallowMultipleComponent]
    public abstract class EnvironmentComponent : MonoBehaviour
    {
        public abstract EnvironmentSettings EnvironmentSettings { get; }

        public static EnvironmentComponent activeSceneEnvironment;

        internal bool modified = false;


        protected virtual void OnEnable()
        {

        }
        protected virtual void OnDisable()
        {

        }

        protected virtual void OnValidate()
        {
            modified = true;
        }
    }
}