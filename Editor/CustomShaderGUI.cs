using UnityEditor;
using UnityEngine;

namespace AggroBird.GameRenderPipeline.Editor
{
    internal sealed class CustomShaderGUI : ShaderGUI
    {
        private enum ShadowMode
        {
            On, Clip, Dither, Off
        }

        private Object[] materials;
        private MaterialProperty[] properties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();

            base.OnGUI(materialEditor, properties);

            materials = materialEditor.targets;
            this.properties = properties;

            if (EditorGUI.EndChangeCheck())
            {
                UpdateProperties();
            }
        }

        private void UpdateProperties()
        {
            if (TryFindProperty("_Shadows", out MaterialProperty shadows) && !shadows.hasMixedValue)
            {
                bool enabled = shadows.floatValue < (float)ShadowMode.Off;
                foreach (Material m in materials)
                {
                    m.SetShaderPassEnabled("ShadowCaster", enabled);
                }
            }

            EnableTextureToggle("_EmissionTex", "_HAS_EMISSION_TEXTURE");
            EnableTextureToggle("_NormalTex", "_HAS_NORMAL_TEXTURE");
        }

        private void EnableTextureToggle(string textureName, string featureName)
        {
            if (TryFindProperty(textureName, out MaterialProperty diffuse) && !diffuse.hasMixedValue)
            {
                foreach (Material material in materials)
                {
                    material.SetKeywordEnabled(featureName, diffuse.textureValue != null);
                }
            }
        }

        private bool TryFindProperty(string name, out MaterialProperty property)
        {
            property = FindProperty(name, properties, false);
            return property != null;
        }
    }

    internal static class MaterialExtend
    {
        public static void SetKeywordEnabled(this Material material, string name, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(name);
            }
            else
            {
                material.DisableKeyword(name);
            }
        }
    }
}