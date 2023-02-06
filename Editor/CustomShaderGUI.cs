using UnityEditor;
using UnityEngine;

namespace AggroBird.GRP.Editor
{
    internal sealed class CustomShaderGUI : ShaderGUI
    {
        private MaterialEditor editor;
        private Object[] materials;
        private MaterialProperty[] properties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();

            base.OnGUI(materialEditor, properties);

            editor = materialEditor;
            materials = materialEditor.targets;
            this.properties = properties;

            if (EditorGUI.EndChangeCheck())
            {
                UpdateProperties();
            }
        }

        enum ShadowMode
        {
            On, Clip, Dither, Off
        }

        private void UpdateProperties()
        {
            MaterialProperty shadows = FindProperty("_Shadows", properties, false);
            if (shadows != null && !shadows.hasMixedValue)
            {
                bool enabled = shadows.floatValue < (float)ShadowMode.Off;
                foreach (Material m in materials)
                {
                    if (m.IsGRPMaterial())
                    {
                        m.SetShaderPassEnabled("ShadowCaster", enabled);
                    }
                }
            }

            EnableTextureToggle("_EmissionTex", "_HAS_EMISSION");
            EnableTextureToggle("_NormalTex", "_HAS_NORMAL");
        }

        private void EnableTextureToggle(string textureName, string featureName)
        {
            MaterialProperty diffuse = FindProperty(textureName, properties, false);
            if (diffuse != null && !diffuse.hasMixedValue)
            {
                foreach (Material m in materials)
                {
                    if (m.IsGRPMaterial())
                    {
                        m.SetKeywordEnabled(featureName, diffuse.textureValue != null);
                    }
                }
            }
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
        public static bool IsGRPMaterial(this Material material)
        {
            return material.shader && material.shader.name.StartsWith("GRP/");
        }
    }
}