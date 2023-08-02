using System;
using UnityEngine;

namespace AggroBird.GameRenderPipeline
{
    internal sealed class ConditionalPropertyAttribute : PropertyAttribute
    {
        public ConditionalPropertyAttribute(string name, params string[] values)
        {
            this.name = name;
            this.values = values;
        }

        public readonly string name;
        public readonly string[] values;
    }

    [Serializable]
    public sealed class EnvironmentSettings
    {
        public enum FogMode
        {
            Linear = 1,
            Exponential,
            ExponentialSquared,
        }

        [Serializable]
        public struct FogSettings
        {
            public bool enabled;

            [Space]
            [Range(0, 1)] public float blend;
            public Color ambientColor;
            public Color inscatteringColor;
            public FogMode fogMode;
            [ConditionalProperty("fogMode", "Linear"), Min(0)]
            public float linearStart;
            [ConditionalProperty("fogMode", "Linear"), Min(0)]
            public float linearEnd;
            [ConditionalProperty("fogMode", "Exponential", "ExponentialSquared"), Min(0)]
            public float density;

            [Space]
            public bool overrideLightDirection;
            public Vector3 lightDirection;
        }

        public FogSettings fogSettings = new()
        {
            enabled = false,
            blend = 1,
            ambientColor = new Color(0.6f, 0.95f, 1),
            inscatteringColor = new Color(0.990f, 0.961f, 0.630f),
            fogMode = FogMode.Linear,
            linearEnd = 1000,
            density = 0.002f,
            overrideLightDirection = false,
            lightDirection = Vector3.forward,
        };

        public enum SkyboxSource
        {
            Material,
            Cubemap,
            Gradient,
        }

        public enum GradientSource
        {
            Gradient,
            Texture,
        }

        [Serializable]
        public struct SkyboxSettings
        {
            internal static Gradient CreateDefaultSkyboxGradient()
            {
                Gradient gradient = new();
                GradientColorKey[] gradientColorKeys =
                {
                    new GradientColorKey(new Color(0.6f, 0.95f, 1), 0),
                    new GradientColorKey(new Color(0.35f, 0.75f, 1), 0.05f),
                    new GradientColorKey(new Color(0, 0.5f, 1), 0.15f),
                    new GradientColorKey(new Color(0, 0.3f, 0.6f), 1),
                };
                GradientAlphaKey[] gradientAlphaKeys =
                {
                    new GradientAlphaKey(1, 0),
                    new GradientAlphaKey(1, 1),
                };
                gradient.SetKeys(gradientColorKeys, gradientAlphaKeys);
                return gradient;
            }

            public GradientSource gradientSource;
            [ConditionalProperty("gradientSource", "Gradient")]
            public Gradient skyboxGradient;
            [ConditionalProperty("gradientSource", "Texture")]
            public Texture2D skyboxTexture;
            public Color groundColor;
            [Space]
            public SkyboxSource skyboxSource;
            [ConditionalProperty("skyboxSource", "Material")]
            public Material skyboxMaterial;
            [ConditionalProperty("skyboxSource", "Cubemap")]
            public Cubemap skyboxCubemap;
            [Space]
            [ColorUsage(false, true)]
            public Color ambientColor;
        }

        public SkyboxSettings skyboxSettings = new()
        {
            skyboxMaterial = null,
            skyboxGradient = SkyboxSettings.CreateDefaultSkyboxGradient(),
            groundColor = new Color(0.407f, 0.380f, 0.357f),
        };


        internal Texture2D SkyboxGradientTexture { get; private set; }

        private Texture2D skyboxGradientRenderTexture;
        private int lastRenderedFrameCount = -1;

        internal bool UpdateEnvironment()
        {
            if (lastRenderedFrameCount != Time.renderedFrameCount)
            {
                if (skyboxSettings.gradientSource == GradientSource.Texture)
                {
                    SkyboxGradientTexture = skyboxSettings.skyboxTexture;
                }
                else
                {
                    TextureUtility.RenderGradientToTexture(ref skyboxGradientRenderTexture, skyboxSettings.skyboxGradient);
                    SkyboxGradientTexture = skyboxGradientRenderTexture;
                }

                return true;
            }

            return false;
        }
    }
}