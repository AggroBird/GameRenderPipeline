using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

[assembly: InternalsVisibleTo("AggroBird.GRP.Editor")]

namespace AggroBird.GRP
{
    [System.Serializable]
    internal sealed class Settings
    {
        public GeneralSettings general = new GeneralSettings();

        public ShadowSettings shadows = new ShadowSettings();
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
    internal sealed class PipelineResources
    {
        public Material defaultLit = default;
        public Material defaultTerrainLit = default;
        public Shader terrainDetailLit = default;
        public Shader terrainDetailGrass = default;
        public Shader detailGrassBillboardShader = default;
        public Shader skyboxRenderShader = default;
        public Shader blitRenderTargetShader = default;
        public Shader postProcessShader = default;
        public Shader smaaShader = default;
        public Texture2D smaaAreaTexture = default;
        public Texture2D smaaSearchTexture = default;
    }

    [CreateAssetMenu(menuName = "Rendering/GRP/Pipeline Asset", order = 998)]
    public sealed partial class GameRenderPipelineAsset : RenderPipelineAsset
    {
        internal static GameRenderPipelineAsset Instance { get; private set; }

        internal const string SettingsPath = "ProjectSettings/GRP.json";
        internal const string SettingsFileName = "GRP_SETTINGS";

        private Settings settingsInstance = null;
        internal Settings settings
        {
            get
            {
                if (settingsInstance == null)
                {
                    try
                    {
                        if (Application.isEditor)
                        {
                            if (File.Exists(SettingsPath))
                            {
                                settingsInstance = JsonUtility.FromJson<Settings>(File.ReadAllText(SettingsPath));
                            }
                        }
                        else
                        {
                            settingsInstance = JsonUtility.FromJson<Settings>(UnityEngine.Resources.Load<TextAsset>(SettingsFileName).text);
                        }
                    }
                    catch (System.Exception)
                    {

                    }

                    if (settingsInstance == null)
                    {
                        settingsInstance = new Settings();
                    }
                }

                return settingsInstance;
            }
            set
            {
                settingsInstance = value;
            }
        }

        [SerializeField, Space]
        private PipelineResources resources = default;
        internal PipelineResources Resources => resources;

        protected override RenderPipeline CreatePipeline()
        {
            Instance = this;

            return new GameRenderPipeline(this);
        }


        public override Shader defaultShader => defaultMaterial.shader;
        public override Material defaultMaterial => resources.defaultLit;
        public override Material defaultTerrainMaterial => resources.defaultTerrainLit;

#if UNITY_EDITOR
        public override Shader terrainDetailLitShader => resources.terrainDetailLit;
        public override Shader terrainDetailGrassShader => resources.terrainDetailGrass;
        public override Shader terrainDetailGrassBillboardShader => resources.detailGrassBillboardShader;
#endif
    }
}
