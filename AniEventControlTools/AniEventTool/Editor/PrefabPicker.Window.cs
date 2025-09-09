using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using AniEventTool.Editor;

namespace AniEventTool.Editor
{
    public class PrefabPickerWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private Dictionary<GameObject, bool> previewLoadAttempts = new Dictionary<GameObject, bool>();
        private System.Action<GameObject> onSelect;
        private System.Action onClose;
        private bool isSelected = false;

        public void Show(System.Action<GameObject> onSelectCallback)
        {
            var window = GetWindow<PrefabPickerWindow>("Select Prefab");
            window.onSelect = onSelectCallback;
            window.Show();
        }

        public void Show(System.Action<GameObject> onSelectCallback, System.Action onCloseCallback)
        {
            var window = GetWindow<PrefabPickerWindow>("Select Prefab");

            window.isSelected = false;
            window.onSelect = onSelectCallback;
            window.onClose = onCloseCallback;
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Select a Prefab", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            string[] prefabGuids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Resources" });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<AniEventControllerBase>() != null)
                {
                    if (!previewLoadAttempts.ContainsKey(prefab))
                    {
                        previewLoadAttempts[prefab] = false;
                    }

                    EditorGUILayout.BeginHorizontal();

                    Texture2D preview = AssetPreview.GetAssetPreview(prefab);

                    if (preview != null)
                    {
                        preview = CropTexture(preview);
                    }
                    previewLoadAttempts[prefab] = preview != null;
                    if (preview == null)
                    {
                        preview = AssetPreview.GetMiniThumbnail(prefab); // Fallback to mini-thumbnail
                    }

                    GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));
                    GUILayout.BeginVertical();
                    if (GUILayout.Button(prefab.name, GUILayout.ExpandWidth(true), GUILayout.Height(40)))
                    {
                        HandlePrefabSelection(prefab);
                        Close();
                    }
                    EditorGUILayout.BeginHorizontal();
                    if (!previewLoadAttempts[prefab])
                    {
                        if (GUILayout.Button(CustomGUIStyles.refreshIcon, EditorStyles.iconButton, GUILayout.Width(20), GUILayout.Height(20)))
                        {
                            previewLoadAttempts[prefab] = false; // Reset attempt flag
                            //Repaint(); // Trigger a repaint to reload the preview
                            ForceRefreshAssetPreview(prefab);
                        }
                    }
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnDestroy()
        {
            if (!isSelected == false)
                onClose?.Invoke();
        }

        void ForceRefreshAssetPreview(GameObject prefab)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private void HandlePrefabSelection(GameObject selectedPrefab)
        {
            // Logic to handle what happens when a prefab is selected
            isSelected = true;
            onSelect?.Invoke(selectedPrefab);
            Close();
        }
        Texture2D CropTexture(Texture2D sourceTexture)
        {
            int xCenter = sourceTexture.width / 2;
            int yCenter = sourceTexture.height / 2;

            // Calculate the start points and size of the new texture
            int startX = xCenter - sourceTexture.width / 4;
            int startY = yCenter - sourceTexture.height / 4;
            int width = sourceTexture.width / 2;
            int height = sourceTexture.height / 2;

            // Get the pixels from the source texture
            Color[] pixels = sourceTexture.GetPixels(startX, startY, width, height);

            // Create a new texture and set the extracted pixels
            Texture2D croppedTexture = new Texture2D(width, height);
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply(); // Apply changes to the texture

            return croppedTexture;
        }
    }
}