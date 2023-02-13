using UnityEngine;

namespace AggroBird.GRP
{
    [System.Serializable]
    internal sealed class GameRenderPipelineSettings
    {
        public GeneralSettings general = new GeneralSettings();

        public ShadowSettings shadows = new ShadowSettings();

        public ExperimentalSettings experimental = new ExperimentalSettings();
    }

    [System.Serializable]
    internal sealed class GeneralSettings
    {
        public bool useDynamicBatching = true;
        public bool useGPUInstancing = true;
        public bool useSRPBatcher = true;
        public bool useLightsPerObject = true;

        [Space, Min(0.01f)]
        public float crossFadeAnimationDuration = 1;

        [Space]
        public bool allowHDR = true;
    }

    [System.Serializable]
    internal sealed class ShadowSettings
    {
        public enum TextureSize
        {
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
            _8192 = 8192,
        }

        public enum FilterMode
        {
            PCF2x2,
            PCF3x3,
            PCF5x5,
            PCF7x7,
        }

        [Min(0.001f)]
        public float maxDistance = 100f;

        [Range(0.001f, 1f)]
        public float distanceFade = 0.1f;

        [System.Serializable]
        public struct Directional
        {
            public TextureSize atlasSize;
            public FilterMode filter;

            [Range(1, 4)] public int cascadeCount;
            [Range(0f, 1f)] public float cascadeRatio1, cascadeRatio2, cascadeRatio3;

            internal Vector3 cascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);

            [Range(0.001f, 1f)]
            public float cascadeFade;

            public enum CascadeBlendMode
            {
                Hard, Soft, Dither,
            }

            public CascadeBlendMode cascadeBlend;
        }

        [Space]
        public Directional directional = new Directional
        {
            atlasSize = TextureSize._1024,
            filter = FilterMode.PCF2x2,
            cascadeCount = 4,
            cascadeRatio1 = 0.1f,
            cascadeRatio2 = 0.25f,
            cascadeRatio3 = 0.5f,
            cascadeFade = 0.1f,
            cascadeBlend = Directional.CascadeBlendMode.Hard,
        };

        [System.Serializable]
        public struct Other
        {
            public TextureSize atlasSize;
            public FilterMode filter;
        }

        [Space]
        public Other other = new Other
        {
            atlasSize = TextureSize._1024,
            filter = FilterMode.PCF2x2,
        };
    }

    [System.Serializable]
    internal sealed class ExperimentalSettings
    {
        [System.Serializable]
        public struct Hatching
        {
            public bool enabled;
            public Texture2D dark;
            public Texture2D bright;
            public float scale;
            public float intensity;
        }

        public Hatching hatching = new Hatching
        {
            enabled = false,
            scale = 5,
            intensity = 1,
        };
    }
}