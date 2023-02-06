using UnityEngine;

namespace AggroBird.GRP
{
    [System.Serializable]
    internal class SettingsAsset : ScriptableObject
    {
        [SerializeField] public Settings settings;
    }
}