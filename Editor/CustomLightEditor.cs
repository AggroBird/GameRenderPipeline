using UnityEditor;
using UnityEngine;

namespace AggroBird.GameRenderPipeline.Editor
{
    [CustomEditorForRenderPipeline(typeof(Light), typeof(GameRenderPipelineAsset))]
    internal sealed class CustomLightEditor : LightEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (!settings.lightType.hasMultipleDifferentValues && (LightType)settings.lightType.enumValueIndex == LightType.Spot)
            {
                settings.DrawInnerAndOuterSpotAngle();
                settings.ApplyModifiedProperties();
            }

            /*Light light = target as Light;
            if (light.cullingMask != -1)
            {
                EditorGUILayout.HelpBox(
                    light.type == LightType.Directional ?
                    "Culling Mask only affects shadows." :
                    "Culling Mask only affects shadow unless Use Lights Per Objects is on.",
                    MessageType.Warning
                );
            }*/
        }
    }
}