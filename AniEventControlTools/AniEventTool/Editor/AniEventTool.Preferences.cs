namespace AniEventTool.Editor
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;

    public class AniEventToolPreferences
    {
        private const string SETTINGS_PATH = "ProjectSettings/AniEventToolSettings.json";
        private static string fullSettingsPath => Path.Combine(Application.dataPath, "..", SETTINGS_PATH);

        private static CustomSettings _settings;
        public static CustomSettings settings
        {
            get
            {
                if (_settings == null)
                {
                    LoadSettings();
                }
                return _settings;
            }
            set
            {
                _settings = value;
            }
        }


        public static string JSONFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(settings.jsonDataPath))
                {
                    Debug.LogError($"json���� ���� ��θ� ã�� �� ��� Assets ������ �����");
                    return Application.dataPath;
                }
                string combinedPath = Path.Combine(Application.dataPath, settings.jsonDataPath);
                return combinedPath.Replace("\\", "/") + "/";
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            return new SettingsProvider("Preferences/AniEventTool", SettingsScope.User)
            {
                guiHandler = (searchContext) =>
                {
                    LoadSettings();

                    // Draw the GUI
                    EditorGUILayout.LabelField("AniEventTool Preferences", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    {
                        string displayedJsonDataPath = Path.Combine("Assets", settings.jsonDataPath);
                        displayedJsonDataPath = EditorGUILayout.TextField("Json File Path", displayedJsonDataPath);
                        if (GUILayout.Button("Browse", GUILayout.Width(70)))
                        {
                            string startPath = string.IsNullOrEmpty(displayedJsonDataPath) || !Directory.Exists(displayedJsonDataPath) ? Application.dataPath : displayedJsonDataPath;
                            string path = EditorUtility.OpenFolderPanel("Select Folder", startPath, "");
                            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                            {
                                settings.jsonDataPath = path.Substring(Application.dataPath.Length + 1);
                                SaveSettings();
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.BeginChangeCheck();
                    // Add GUI for the new bool values
                    settings.showSaveOnInspector = EditorGUILayout.Toggle("Show Save On Inspector", settings.showSaveOnInspector);
                    settings.saveOnPlay = EditorGUILayout.Toggle("Save On Play", settings.saveOnPlay);
                    if (EditorGUI.EndChangeCheck()) 
                    {
                        SaveSettings();
                    }
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Prefab", "Path", "Show Save On Inspector", "Save On Play" })
            };
        }


        private static void LoadSettings()
        {
            if (File.Exists(fullSettingsPath))
            {
                string json = File.ReadAllText(fullSettingsPath);
                settings = JsonUtility.FromJson<CustomSettings>(json);
            }
            else
            {
                settings = new CustomSettings();
            }
        }

        private static void SaveSettings()
        {
            string json = JsonUtility.ToJson(settings, true);
            File.WriteAllText(fullSettingsPath, json);
            AssetDatabase.Refresh();
        }
    }

    [System.Serializable]
    public class CustomSettings
    {
        [SerializeField] public string prefabPath;
        [SerializeField] public string jsonDataPath;
        [SerializeField] public bool showSaveOnInspector;
        [SerializeField] public bool saveOnPlay;

        public CustomSettings()
        {
            prefabPath = string.Empty;
            jsonDataPath = string.Empty;
            showSaveOnInspector = false;
            saveOnPlay = false;
        }
    }

}