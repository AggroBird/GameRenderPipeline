using UnityEditor;
using UnityEngine;

namespace AggroBird.GRP.Editor
{
    internal abstract class EnvironmentSettingsPropertyDrawer : PropertyDrawer
    {
        protected const float BaseHeight = 20;
        private SerializedProperty property = default;
        protected Rect rowPosition;

        private float totalHeight = 0;

        protected EnvironmentSettingsPropertyDrawer(int propertyCount, int spaceCount)
        {
            totalHeight = BaseHeight * (propertyCount + 1) + BaseHeight * 0.5f * spaceCount;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = BaseHeight;
            if (property.isExpanded)
            {
                height += totalHeight;
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            this.property = property;
            rowPosition = position;
            rowPosition.height = BaseHeight - 2;

            property.isExpanded = EditorGUI.Foldout(rowPosition, property.isExpanded, property.displayName);
            rowPosition.y += BaseHeight;
        }

        protected SerializedProperty DefaultPropertyField(string name, bool space = false)
        {
            if (space)
            {
                rowPosition.y += BaseHeight * 0.5f;
            }

            SerializedProperty prop = property.FindPropertyRelative(name);
            if (prop == null)
            {
                EditorGUI.LabelField(rowPosition, $"<{name}>");
            }
            else
            {
                EditorGUI.PropertyField(rowPosition, prop);
            }
            rowPosition.y += BaseHeight;
            return prop;
        }
    }

    [CustomPropertyDrawer(typeof(EnvironmentSettings.FogSettings))]
    internal sealed class FogSettingsPropertyDrawer : EnvironmentSettingsPropertyDrawer
    {
        public FogSettingsPropertyDrawer() : base(5, 1)
        {

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) + (GetFogMode(property.FindPropertyRelative("fogMode")) == FogMode.Linear && property.isExpanded ? BaseHeight : 0);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

            if (!property.isExpanded) return;

            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUI.DisabledScope(!DefaultPropertyField("enabled").boolValue))
                {
                    DefaultPropertyField("blend", true);
                    DefaultPropertyField("ambientColor");
                    DefaultPropertyField("inscatteringColor");
                    FogMode fogMode = GetFogMode(DefaultPropertyField("fogMode"));

                    SerializedProperty fogParamProperty = property.FindPropertyRelative("fogParam");
                    switch (fogMode)
                    {
                        case FogMode.Linear:
                        {
                            SerializedProperty linearStart = fogParamProperty.FindPropertyRelative("x");
                            SerializedProperty linearEnd = fogParamProperty.FindPropertyRelative("y");
                            EditorGUI.PropertyField(rowPosition, linearStart, new GUIContent("Linear Start"));
                            rowPosition.y += BaseHeight;
                            EditorGUI.PropertyField(rowPosition, linearEnd, new GUIContent("Linear End"));
                            linearStart.floatValue = Mathf.Max(linearStart.floatValue, 0);
                            linearEnd.floatValue = Mathf.Max(linearStart.floatValue, linearEnd.floatValue);
                        }
                        break;
                        default:
                            SerializedProperty density = fogParamProperty.FindPropertyRelative("z");
                            EditorGUI.PropertyField(rowPosition, density, new GUIContent("Density"));
                            density.floatValue = Mathf.Max(density.floatValue, 0);
                            break;
                    }
                    rowPosition.y += BaseHeight;
                }
            }
        }

        private FogMode GetFogMode(SerializedProperty property)
        {
            if (property != null && property.propertyType == SerializedPropertyType.Enum)
            {
                return (FogMode)property.intValue;
            }
            return FogMode.Linear;
        }
    }

    [CustomPropertyDrawer(typeof(EnvironmentSettings.CloudSettings))]
    internal sealed class CloudSettingsPropertyDrawer : EnvironmentSettingsPropertyDrawer
    {
        public CloudSettingsPropertyDrawer() : base(12, 4)
        {

        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

            if (!property.isExpanded) return;

            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUI.DisabledScope(!DefaultPropertyField("enabled").boolValue))
                {
                    DefaultPropertyField("colorTop", true);
                    DefaultPropertyField("colorBottom");

                    DefaultPropertyField("sampleOffset", true);
                    DefaultPropertyField("sampleScale");

                    DefaultPropertyField("thickness", true);
                    DefaultPropertyField("height");
                    DefaultPropertyField("layerHeight");
                    DefaultPropertyField("fadeDistance");

                    DefaultPropertyField("traceLengthMax", true);
                    DefaultPropertyField("traceStep");
                    DefaultPropertyField("traceEdgeAccuracy");
                    DefaultPropertyField("traceEdgeThreshold");
                }
            }
        }
    }

    [CustomEditor(typeof(Environment))]
    internal sealed class EnvironmentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }

    public abstract class InspectorBase : UnityEditor.Editor
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
    public sealed class PostProcessSettingsAssetEditor : InspectorBase
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