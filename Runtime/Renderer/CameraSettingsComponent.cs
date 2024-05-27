using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    public class CameraSettingsComponent : MonoBehaviour
    {
        [field: SerializeField]
        public bool RenderShadowAtlas { get; private set; } = true;
    }
}