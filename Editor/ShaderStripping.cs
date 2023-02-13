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

        private static ShaderKeyword[] MakeKeywords(string[] strings)
        {
            ShaderKeyword[] result = new ShaderKeyword[strings.Length];
            for (int i = 0; i < strings.Length; i++)
            {
                result[i] = new ShaderKeyword(strings[i]);
            }
            return result;
        }

        private readonly ShaderKeyword[] directionalFilterKeywords;
        private readonly ShaderKeyword[] otherFilterKeywords;
        private readonly ShaderKeyword[] cascadeBlendKeywords;
        private readonly ShaderKeyword lightsPerObjectKeyword;

        private readonly int directionalFilterIndex;
        private readonly int otherFilterIndex;
        private readonly int cascadeBlendIndex;
        private readonly bool lightsPerObjectEnabled;

        private readonly GameRenderPipelineAsset renderPipeline;

        public ShaderStripper()
        {
            if (GraphicsSettings.defaultRenderPipeline is GameRenderPipelineAsset renderPipeline)
            {
                this.renderPipeline = renderPipeline;

                directionalFilterKeywords = MakeKeywords(Shadows.directionalFilterKeywords);
                otherFilterKeywords = MakeKeywords(Shadows.otherFilterKeywords);
                cascadeBlendKeywords = MakeKeywords(Shadows.cascadeBlendKeywords);
                lightsPerObjectKeyword = new ShaderKeyword(Lighting.LightsPerObjectKeyword);

                GameRenderPipelineSettings settings = renderPipeline.Settings;
                directionalFilterIndex = (int)settings.shadows.directional.filter;
                otherFilterIndex = (int)settings.shadows.other.filter;
                cascadeBlendIndex = (int)settings.shadows.directional.cascadeBlend;
                lightsPerObjectEnabled = settings.general.useLightsPerObject;
            }
        }

        private static bool CheckSkip(Shader shader, ShaderCompilerData data, ShaderKeyword[] keywords, int activeIndex)
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
        private static bool CheckSkip(Shader shader, ShaderCompilerData data, ShaderKeyword keyword, bool active)
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
                    if (CheckSkip(shader, data[i], directionalFilterKeywords, directionalFilterIndex))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                    if (CheckSkip(shader, data[i], otherFilterKeywords, otherFilterIndex))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                    if (CheckSkip(shader, data[i], cascadeBlendKeywords, cascadeBlendIndex))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                    if (CheckSkip(shader, data[i], lightsPerObjectKeyword, lightsPerObjectEnabled))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                }
            }
        }
    }
}