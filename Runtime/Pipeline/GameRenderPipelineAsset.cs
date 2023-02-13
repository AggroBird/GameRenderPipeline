using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

[assembly: InternalsVisibleTo("AggroBird.GRP.Editor")]

namespace AggroBird.GRP
{
    [CreateAssetMenu(menuName = "Rendering/GRP/Pipeline Asset", order = 997)]
    public sealed partial class GameRenderPipelineAsset : RenderPipelineAsset
    {
        internal static GameRenderPipelineAsset Instance { get; private set; }

        internal const string SettingsPath = "ProjectSettings/GRP.json";
        internal const string SettingsFileName = "GRP_SETTINGS";

        [SerializeField] private GameRenderPipelineResources resources;
        internal GameRenderPipelineResources Resources => resources;

        [Space]
        [SerializeField] private GameRenderPipelineSettings settings = null;
        internal GameRenderPipelineSettings Settings => settings;


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
