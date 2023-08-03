using System;
using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    [Serializable]
    public sealed class PostProcessSettings
    {
        [Serializable]
        public struct General
        {
            public enum ColorLUTResolution
            {
                _16 = 16,
                _32 = 32,
                _64 = 64,
            }

            public ColorLUTResolution colorLUTResolution;
        }

        public General general = new()
        {
            colorLUTResolution = General.ColorLUTResolution._32,
        };

        [Serializable]
        public struct SMAA
        {
            public enum Quality
            {
                Low = 0,
                Medium = 1,
                High = 2,
            }

            public bool enabled;

            public Quality quality;
        }

        public SMAA smaa = new()
        {
            enabled = false,
            quality = SMAA.Quality.High,
        };

        [Serializable]
        public struct Bloom
        {
            public bool enabled;

            [Range(1, 16)]
            public int maxIterations;

            [Min(1f)]
            public int downscaleLimit;

            public bool bicubicUpsampling;

            [Min(0f)]
            public float threshold;

            [Range(0f, 1f)]
            public float thresholdKnee;

            [Min(0f)]
            public float intensity;

            public enum Mode { Additive, Scattering }

            public Mode mode;

            [Range(0.05f, 0.95f)]
            public float scatter;
        }

        public Bloom bloom = new()
        {
            enabled = false,
            maxIterations = 3,
            downscaleLimit = 1,
            bicubicUpsampling = true,
            threshold = 0.5f,
            thresholdKnee = 0.5f,
            intensity = 1,
            mode = Bloom.Mode.Scattering,
            scatter = 0.7f,
        };

        [Serializable]
        public struct DepthOfField
        {
            public bool enabled;

            [Min(0)]
            public float focusDistance;

            [Min(3)]
            public float focusRange;

            [Min(1)]
            public float bokehRadius;
        }

        public DepthOfField depthOfField = new()
        {
            enabled = false,
            focusDistance = 3,
            focusRange = 6,
            bokehRadius = 4,
        };

        [Serializable]
        public struct Outline
        {
            public bool enabled;

            public Color color;

            [Min(0)]
            public float normalIntensity;

            [Min(0)]
            public float normalBias;

            [Min(0)]
            public float depthIntensity;

            [Min(0)]
            public float depthBias;

            [Space]
            public bool useDepthFade;

            [Min(0)]
            public float depthFadeBegin;

            [Min(0)]
            public float depthFadeEnd;
        }

        public Outline outline = new()
        {
            color = new Color(0, 0, 0, 1),
            normalIntensity = 2.78f,
            normalBias = 3.4f,
            depthIntensity = 2.86f,
            depthBias = 2.03f,
            useDepthFade = false,
            depthFadeBegin = 0,
            depthFadeEnd = 1000,
        };

        [Serializable]
        public struct AmbientOcclusion
        {
            public bool enabled;

            [Range(1, 32)]
            public int sampleCount;

            [Range(0f, 3f)]
            public float radius;

            [Range(0f, 1f)]
            public float intensity;
        }

        public AmbientOcclusion ambientOcclusion = new()
        {
            enabled = false,
            sampleCount = 4,
            radius = 0.5f,
            intensity = 0.25f,
        };

        [Serializable]
        public struct ColorAdjustments
        {
            public float postExposure;

            [Range(-100f, 100f)]
            public float contrast;

            [ColorUsage(false, true)]
            public Color colorFilter;

            [Range(-180f, 180f)]
            public float hueShift;

            [Range(-100f, 100f)]
            public float saturation;
        }

        public ColorAdjustments colorAdjustments = new()
        {
            colorFilter = Color.white
        };

        [Serializable]
        public struct WhiteBalance
        {
            [Range(-100f, 100f)]
            public float temperature;

            [Range(-100f, 100f)]
            public float tint;
        }

        public WhiteBalance whiteBalance = new()
        {

        };

        [Serializable]
        public struct SplitToning
        {
            [ColorUsage(false)]
            public Color shadows;

            [ColorUsage(false)]
            public Color highlights;

            [Range(-100f, 100f)]
            public float balance;
        }

        public SplitToning splitToning = new()
        {
            shadows = Color.gray,
            highlights = Color.gray,
        };

        [Serializable]
        public struct ChannelMixer
        {
            public Vector3 red, green, blue;
        }

        public ChannelMixer channelMixer = new()
        {
            red = Vector3.right,
            green = Vector3.up,
            blue = Vector3.forward,
        };

        [Serializable]
        public struct ShadowsMidtonesHighlights
        {
            [ColorUsage(false, true)]
            public Color shadows, midtones, highlights;

            [Range(0f, 2f)]
            public float shadowsStart, shadowsEnd, highlightsStart, highLightsEnd;
        }

        public ShadowsMidtonesHighlights shadowsMidtonesHighlights = new()
        {
            shadows = Color.white,
            midtones = Color.white,
            highlights = Color.white,
            shadowsEnd = 0.3f,
            highlightsStart = 0.55f,
            highLightsEnd = 1f,
        };

        [Serializable]
        public struct Vignette
        {
            public bool enabled;
            [Range(0f, 5f)]
            public float falloff;
        }

        public Vignette vignette = new()
        {
            enabled = false,
            falloff = 0.5f,
        };

        [Serializable]
        public struct ToneMapping
        {
            public enum Mode
            {
                None,
                ACES,
                Neutral,
                Reinhard,
            }

            public Mode mode;
        }

        public ToneMapping toneMapping = new()
        {
            mode = ToneMapping.Mode.None,
        };
    }

    [CreateAssetMenu(menuName = "Rendering/GRP/Post Process Settings Asset", order = 999)]
    public sealed class PostProcessSettingsAsset : ScriptableObject
    {
        [SerializeField]
        private PostProcessSettings postProcessSettings = new();
        public PostProcessSettings Settings => postProcessSettings;
    }
}