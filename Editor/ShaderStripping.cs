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

        private readonly GameRenderPipelineSettings settings;

        public ShaderStripper()
        {
            if (GraphicsSettings.defaultRenderPipeline is GameRenderPipelineAsset renderPipeline)
            {
                settings = renderPipeline.Settings;
            }
        }

        private static bool CheckSkip(Shader shader, ShaderCompilerData data, GlobalKeyword[] keywords, int activeIndex)
        {
            if (activeIndex == 0)
            {
                // Skip if any keyword is enabled
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (shader.keywordSpace.FindKeyword(keywords[i].name).isValid)
                    {
                        if (data.shaderKeywordSet.IsEnabled(keywords[i]))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                activeIndex--;

                // Skip if selected keyword is disabled
                if (shader.keywordSpace.FindKeyword(keywords[activeIndex].name).isValid)
                {
                    if (!data.shaderKeywordSet.IsEnabled(keywords[activeIndex]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private static bool CheckSkip(Shader shader, ShaderCompilerData data, in GlobalKeyword keyword, bool active)
        {
            // Skip if selected keyword is enabled/disabled depending on requirement
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
            if (settings != null)
            {
                switch (snippet.passType)
                {
                    case PassType.Normal:
                    case PassType.ScriptableRenderPipeline:
                    case PassType.ScriptableRenderPipelineDefaultUnlit:
                        for (int i = data.Count - 1; i >= 0; i--)
                        {
                            if (CheckSkip(shader, data[i], Shadows.directionalFilterKeywords, (int)settings.shadows.directional.filter))
                            {
                                data.RemoveAt(i);
                                goto Continue;
                            }
                            if (CheckSkip(shader, data[i], Shadows.otherFilterKeywords, (int)settings.shadows.other.filter))
                            {
                                data.RemoveAt(i);
                                goto Continue;
                            }
                            if (CheckSkip(shader, data[i], Shadows.cascadeBlendKeywords, (int)settings.shadows.directional.cascadeBlend))
                            {
                                data.RemoveAt(i);
                                goto Continue;
                            }
                            if (CheckSkip(shader, data[i], Lighting.lightsPerObjectKeyword, settings.general.useLightsPerObject))
                            {
                                data.RemoveAt(i);
                                goto Continue;
                            }
                            if (CheckSkip(shader, data[i], Lighting.hatchingKeyword, settings.experimental.hatching.enabled))
                            {
                                data.RemoveAt(i);
                                goto Continue;
                            }
                            if (CheckSkip(shader, data[i], Lighting.cellShadingKeyword, settings.experimental.cellShading.enabled))
                            {
                                data.RemoveAt(i);
                                goto Continue;
                            }

                            // Strip unused shader fog settings
                            if (settings.stripping.fog.StripFog)
                            {
                                GlobalKeyword[] fogModeKeywords = CameraRenderer.fogModeKeywords;
                                for (int fog = 0; fog < fogModeKeywords.Length; fog++)
                                {
                                    if (!settings.stripping.fog[(EnvironmentSettings.FogMode)(fog + 1)])
                                    {
                                        if (shader.keywordSpace.FindKeyword(fogModeKeywords[fog].name).isValid)
                                        {
                                            if (data[i].shaderKeywordSet.IsEnabled(fogModeKeywords[fog]))
                                            {
                                                data.RemoveAt(i);
                                                goto Continue;
                                            }
                                        }
                                    }
                                }
                            }

                        Continue:
                            continue;
                        }
                        break;
                }
            }
        }
    }
}