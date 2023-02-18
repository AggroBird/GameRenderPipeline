using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GRP.Editor
{
    internal sealed class ShaderStripper : IPreprocessShaders
    {
        public int callbackOrder => 0;

        private readonly GameRenderPipelineAsset renderPipeline;
        private readonly GameRenderPipelineSettings settings;

        public ShaderStripper()
        {
            if (GraphicsSettings.defaultRenderPipeline is GameRenderPipelineAsset renderPipeline)
            {
                this.renderPipeline = renderPipeline;
                settings = renderPipeline.Settings;
            }
        }

        private static bool CheckSkip(Shader shader, ShaderCompilerData data, GlobalKeyword[] keywords, int activeIndex)
        {
            if (shader.keywordSpace.FindKeyword(keywords[activeIndex].name).isValid)
            {
                if (!data.shaderKeywordSet.IsEnabled(keywords[activeIndex]))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool CheckSkip(Shader shader, ShaderCompilerData data, in GlobalKeyword keyword, bool active)
        {
            if (shader.keywordSpace.FindKeyword(keyword.name).isValid)
            {
                if (data.shaderKeywordSet.IsEnabled(keyword) != active)
                {
                    return true;
                }
            }
            return false;
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (renderPipeline)
            {
                for (int i = data.Count - 1; i >= 0; i--)
                {
                    if (CheckSkip(shader, data[i], Shadows.directionalFilterKeywords, (int)settings.shadows.directional.filter))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                    if (CheckSkip(shader, data[i], Shadows.otherFilterKeywords, (int)settings.shadows.other.filter))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                    if (CheckSkip(shader, data[i], Shadows.cascadeBlendKeywords, (int)settings.shadows.directional.cascadeBlend))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                    if (CheckSkip(shader, data[i], Lighting.lightsPerObjectKeyword, settings.general.useLightsPerObject))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                    if (CheckSkip(shader, data[i], Lighting.hatchingKeyword, settings.experimental.hatching.enabled))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                    if (CheckSkip(shader, data[i], Lighting.cellShadingKeyword, settings.experimental.cellShading.enabled))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                }
            }
        }
    }
}