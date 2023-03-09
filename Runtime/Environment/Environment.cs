using UnityEngine;

namespace AggroBird.GRP
{
    [System.Serializable]
    public sealed class EnvironmentSettings
    {
        [System.Serializable]
        public struct FogSettings
        {
            public bool enabled;

            [Range(0, 1)] public float blend;
            public Color ambientColor;
            public Color inscatteringColor;
            public FogMode fogMode;
            public Vector4 fogParam;

            [Space]
            public bool overrideLightDirection;
            public Vector3 lightDirection;
        }

        public FogSettings fogSettings = new FogSettings
        {
            enabled = false,
            blend = 1,
            ambientColor = new Color(0.6f, 0.95f, 1),
            inscatteringColor = new Color(0.990f, 0.961f, 0.630f),
            fogMode = FogMode.Linear,
            fogParam = new Vector4(0, 1000, 0.002f),
            overrideLightDirection = false,
            lightDirection = Vector3.forward,
        };

        [System.Serializable]
        public struct SkyboxSettings
        {
            internal static Gradient CreateDefaultSkyboxGradient()
            {
                Gradient gradient = new Gradient();
                GradientColorKey[] gradientColorKeys =
                {
                    new GradientColorKey(new Color(0.6f, 0.95f, 1), 0),
                    new GradientColorKey(new Color(0, 0.5f, 1), 1),
                };
                GradientAlphaKey[] gradientAlphaKeys =
                {
                    new GradientAlphaKey(1, 0),
                    new GradientAlphaKey(1, 1),
                };
                gradient.SetKeys(gradientColorKeys, gradientAlphaKeys);
                return gradient;
            }

            public bool generateSkyboxCubemap;
            public Texture sourceCubemap;
            public bool useCubemapAsSkybox;
            public Gradient gradient;
            public Texture2D gradientTexture;
            public Color groundColor;
            [ColorUsage(false, true)]
            public Color ambientColor;
        }

        public SkyboxSettings skyboxSettings = new SkyboxSettings
        {
            generateSkyboxCubemap = true,
            sourceCubemap = null,
            useCubemapAsSkybox = false,
            gradient = SkyboxSettings.CreateDefaultSkyboxGradient(),
            groundColor = new Color(0.407f, 0.380f, 0.357f),
        };

        [System.Serializable]
        public struct CloudSettings
        {
            public bool enabled;

            public Color colorTop;
            public Color colorBottom;

            public Vector3 sampleOffset;
            public Vector3 sampleScale;

            [Range(0, 1)] public float thickness;
            public float height;
            [Min(0)] public float layerHeight;
            public float fadeDistance;

            [Min(0)] public float traceLengthMax;
            [Range(0.1f, 50)] public float traceStep;
            [Range(0.001f, 1)] public float traceEdgeAccuracy;
            [Range(0, 1)] public float traceEdgeThreshold;
        }

        public CloudSettings cloudSettings = new CloudSettings
        {
            enabled = false,
            colorTop = new Color(1, 1, 1, 1),
            colorBottom = new Color(0, 0.795f, 1, 1),
            sampleOffset = new Vector3(5, 0, 0),
            sampleScale = new Vector3(0.2f, 0.2f, 0.2f),
            thickness = 0.3f,
            height = 200,
            layerHeight = 50,
            fadeDistance = 1000,
            traceLengthMax = 1400,
            traceStep = 30,
            traceEdgeAccuracy = 0.1f,
            traceEdgeThreshold = 0.2f,
        };
    }

    public abstract class Environment : MonoBehaviour
    {
        public abstract EnvironmentSettings EnvironmentSettings { get; }

        public bool IsDirty { get; set; }


        protected virtual void OnValidate()
        {
            IsDirty = true;
        }
    }
}