using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    [DisallowMultipleComponent]
    public abstract class PostProcessComponent : MonoBehaviour
    {
        [System.Serializable]
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
        public PostProcessSettingsAsset postProcessSettingsAsset;


        internal static PostProcessComponent activeScenePostProcess;

        protected virtual void OnEnable()
        {

        }
        protected virtual void OnDisable()
        {

        }
    }
}