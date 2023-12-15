using UnityEditor;
using UnityEngine;

namespace AggroBird.GameRenderPipeline.Editor
{
    [CustomPropertyDrawer(typeof(ConditionalPropertyAttribute))]
    internal sealed class ConditionalPropertyAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (CheckCondition(property))
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (CheckCondition(property))
            {
                return EditorGUI.GetPropertyHeight(property);
            }
            return 0;
        }

        private bool CheckCondition(SerializedProperty property)
        {
            if (attribute is ConditionalPropertyAttribute conditional)
            {
                string path = property.propertyPath;
                int idx = path.LastIndexOf('.');
                if (idx != -1)
                {
                    path = path.Substring(0, idx);
                    SerializedProperty check = property.serializedObject.FindProperty(path + '.' + conditional.name);
                    if (check != null)
                    {
                        switch (check.propertyType)
                        {
                            case SerializedPropertyType.Enum:
                                foreach (var val in conditional.values)
                                {
                                    if ((int)val == check.intValue)
                                    {
                                        return true;
                                    }
                                }
                                return false;
                            case SerializedPropertyType.Boolean:
                                foreach (var val in conditional.values)
                                {
                                    if ((bool)val == check.boolValue)
                                    {
                                        return true;
                                    }
                                }
                                return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    internal abstract class InspectorBase : UnityEditor.Editor
    {
        protected void CollapsablePropertyField(SerializedProperty property)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(property);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        protected void CollapsablePropertyField(SerializedProperty target, string propertyName)
        {
            SerializedProperty property = target.FindPropertyRelative(propertyName);
            if (property == null)
            {
                GUILayout.Label($"<Missing property '{propertyName}'>");
                return;
            }

            CollapsablePropertyField(property);
        }
    }

    [CustomEditor(typeof(PostProcessSettingsAsset))]
    internal sealed class PostProcessSettingsAssetEditor : InspectorBase
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty settings = serializedObject.FindProperty("postProcessSettings");
            {
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
                foreach (var member in typeof(PostProcessSettings).GetFields(flags))
                {
                    CollapsablePropertyField(settings, member.Name);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}