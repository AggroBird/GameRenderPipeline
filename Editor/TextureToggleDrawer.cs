using UnityEditor;
using UnityEngine;

namespace AggroBird.GameRenderPipeline.Editor
{
    internal sealed class TextureToggleDrawer : MaterialPropertyDrawer
    {
        private readonly string keyword;

        public TextureToggleDrawer(string keyword)
        {
            this.keyword = keyword;
        }

        private static bool IsPropertyTypeSuitable(MaterialProperty prop)
        {
            return prop.type == MaterialProperty.PropType.Texture;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (!IsPropertyTypeSuitable(prop))
            {
                EditorGUI.LabelField(position, "Toggle used on a non-texture property: " + prop.name, EditorStyles.helpBox);
                return;
            }

            MaterialEditor.BeginProperty(position, prop);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            Texture value = editor.TextureProperty(position, prop, label);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                SetKeyword(prop, value);
            }
            EditorGUI.BeginChangeCheck();
            MaterialEditor.EndProperty();
        }
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (!IsPropertyTypeSuitable(prop))
            {
                return 45f;
            }
            return MaterialEditor.GetDefaultPropertyHeight(prop);
        }
        public override void Apply(MaterialProperty prop)
        {
            if (IsPropertyTypeSuitable(prop) && !prop.hasMixedValue)
            {
                SetKeyword(prop, prop.textureValue);
            }
        }

        private void SetKeyword(MaterialProperty prop, bool on)
        {
            if (!string.IsNullOrEmpty(keyword))
            {
                Object[] targets = prop.targets;
                for (int i = 0; i < targets.Length; i++)
                {
                    Material material = (Material)targets[i];
                    if (on)
                    {
                        material.EnableKeyword(keyword);
                    }
                    else
                    {
                        material.DisableKeyword(keyword);
                    }
                }
            }
        }
    }
}