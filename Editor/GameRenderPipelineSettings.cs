using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AggroBird.GRP.Editor
{
    public sealed class GameRenderPipelineSettings : EditorWindow, IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        [MenuItem("GRP/Quality Settings")]
        private static void Open()
        {
            GameRenderPipelineSettings window = GetWindow<GameRenderPipelineSettings>();
            window.titleContent = new GUIContent("GRP Quality Settings");
            window.minSize = new Vector2(400, 500);
        }


        private static string SettingsPath => GameRenderPipelineAsset.SettingsPath;

        private Settings settings = null;

        private void OnEnable()
        {
            try
            {
                settings = JsonUtility.FromJson<Settings>(File.ReadAllText(SettingsPath));
            }
            catch (System.Exception)
            {

            }

            if (settings == null) settings = new Settings();
        }

        private Vector2 scrollView;

        private void OnGUI()
        {
            if (GameRenderPipelineAsset.Instance)
            {
                try
                {

                    SettingsAsset settingsAsset = CreateInstance<SettingsAsset>();
                    settingsAsset.settings = settings;

                    SerializedObject settingsObject = new SerializedObject(settingsAsset);
                    settingsObject.Update();

                    SerializedProperty rootSettings = settingsObject.FindProperty("settings");

                    float currentLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = currentLabelWidth * 1.5f;
                    scrollView = EditorGUILayout.BeginScrollView(scrollView);
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.PropertyField(rootSettings.FindPropertyRelative("general"));
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.PropertyField(rootSettings.FindPropertyRelative("shadows"));
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUIUtility.labelWidth = currentLabelWidth;


                    if (settingsObject.ApplyModifiedProperties())
                    {
                        GameRenderPipelineAsset.Instance.settings = settings;
                        File.WriteAllText(SettingsPath, JsonUtility.ToJson(settings));
                    }
                }
                catch (System.Exception)
                {

                }
            }
            else
            {
                GUILayout.Label("GRP is not set as the active Scriptable Render Pipeline");
            }
        }

        private const string TMPSettingsRootDir = "Assets/_GRP_TMP";
        private const string TMPSettingsPath = TMPSettingsRootDir + "/Resources/" + GameRenderPipelineAsset.SettingsFileName + ".txt";

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(TMPSettingsPath));
                    File.WriteAllText(TMPSettingsPath, File.ReadAllText(SettingsPath));
                    AssetDatabase.ImportAsset(TMPSettingsPath);
                }
            }
            catch (System.Exception)
            {

            }
        }
        public void OnPostprocessBuild(BuildReport report)
        {
            try
            {
                if (Directory.Exists(TMPSettingsRootDir))
                {
                    Directory.Delete(TMPSettingsRootDir, true);
                }
            }
            catch (System.Exception)
            {

            }
        }
    }
}