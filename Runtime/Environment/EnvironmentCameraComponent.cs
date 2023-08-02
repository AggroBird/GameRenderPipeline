using System.Collections.Generic;
using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [RequireComponent(typeof(Camera)), ExecuteAlways]
    public sealed class EnvironmentCameraComponent : EnvironmentComponent
    {
        [SerializeField] private EnvironmentSettings environmentSettings;
        public override EnvironmentSettings GetEnvironmentSettings() => environmentSettings;

        private void Start()
        {

        }

#if UNITY_EDITOR
        internal static List<EnvironmentCameraComponent> activeCameraComponents = new();

        protected override void OnEnable()
        {
            activeCameraComponents.Add(this);
        }
        protected override void OnDisable()
        {
            activeCameraComponents.Remove(this);
        }
#endif
    }
}