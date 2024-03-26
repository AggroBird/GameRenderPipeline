using System;
using System.Collections.Generic;
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
            public static FinalBlendMode Default => new()
            {
                source = BlendMode.One,
                destination = BlendMode.Zero,
            };

            public BlendMode source, destination;
        }

        public virtual float GetRenderScale(float renderScale) => renderScale;
        public virtual FinalBlendMode GetFinalBlendMode() => FinalBlendMode.Default;

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


        internal static readonly List<PostProcessComponent> activePostProcessComponents = new();

        protected virtual void OnEnable()
        {

        }
        protected virtual void OnDisable()
        {

        }
    }
}