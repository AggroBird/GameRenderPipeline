using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GRP
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class PostProcessCamera : MonoBehaviour
    {
        [System.Serializable]
        public struct FinalBlendMode
        {
            public BlendMode source, destination;
        }

        public FinalBlendMode finalBlendMode = new FinalBlendMode
        {
            source = BlendMode.One,
            destination = BlendMode.Zero,
        };

        [Space]
        public PostProcessSettingsAsset postProcessSettingsAsset;

        private void Start()
        {

        }
    }
}