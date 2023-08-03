using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    [DisallowMultipleComponent]
    public abstract class PostProcessComponent : MonoBehaviour
    {
        [Serializable]
        public struct FinalBlendMode
        {
            public BlendMode source, destination;
        }

        public FinalBlendMode finalBlendMode = new()
        {
            source = BlendMode.One,
            destination = BlendMode.Zero,
        };

        [Space]
        [SerializeField] private PostProcessSettingsAsset postProcessSettingsAsset;

        public virtual PostProcessSettings GetPostProcessSettings()
        {
            if (postProcessSettingsAsset != null)
            {
                return postProcessSettingsAsset.Settings;
            }
            return null;
        }


        internal static PostProcessComponent activeScenePostProcess;

        protected virtual void OnEnable()
        {

        }
        protected virtual void OnDisable()
        {

        }
    }
}