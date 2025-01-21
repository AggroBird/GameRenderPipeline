using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline
{
    [System.Serializable]
    public struct CameraSettings
    {
        public bool renderShadowAtlas;
        public bool renderEnvironment;
        public bool renderPostProcess;
        public bool renderFog;

        [Space, Range(GeneralSettings.RenderScaleMin, GeneralSettings.RenderScaleMax)]
        public float renderScale;
        public BicubicRescalingMode bicubicRescalingMode;

        [Space]
        public DepthBits depthBufferBits;

        [Space]
        public OpaqueBufferOutputs opaqueBufferOutputs;
    }

    public class CameraSettingsComponent : MonoBehaviour
    {
        public CameraSettings settings = new()
        {
            renderShadowAtlas = true,
            renderEnvironment = true,
            renderPostProcess = true,
            renderFog = true,
            renderScale = 1,
            bicubicRescalingMode = BicubicRescalingMode.UpOnly,
            depthBufferBits = DepthBits.Depth32,
            opaqueBufferOutputs = OpaqueBufferOutputs.None,
        };
    }
}