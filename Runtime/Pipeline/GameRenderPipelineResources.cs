using UnityEngine;

namespace AggroBird.GRP
{
    [CreateAssetMenu(menuName = "Rendering/GRP/Pipeline Resources", order = 998)]
    public sealed class GameRenderPipelineResources : ScriptableObject
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
}