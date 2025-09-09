namespace AniEventTool.Editor
{
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    public class JsonFilePostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            List<string> assets = new List<string>();
            assets.AddRange(importedAssets);
            assets.AddRange(deletedAssets);
            assets.AddRange(movedAssets);


            string relativePath = AniEventToolPreferences.JSONFilePath;
            relativePath = "Assets" + relativePath.Remove(0, Application.dataPath.Length);

            foreach (string asset in assets)
            {
                if (asset.EndsWith(".json") && asset.Contains(relativePath))
                {
                    Debug.Log("JSON file modified: " + asset);

                    if (AniEventToolWindow.IsOpened)
                        AniEventToolWindow.Instance.OnJsonFileModified();
                }
                else if (asset.EndsWith(".prefab"))
                {
                    string prefabName = Path.GetFileNameWithoutExtension(asset);

                    if (AniEventToolWindow.IsOpened && AniEventToolEditorCache.CachedPrefabs.Exists(prefab => prefab.name.Equals(prefabName)))
                    {
                        Debug.Log("Prefab file modified: " + asset);
                        AniEventToolWindow.Instance.OnJsonFileModified();
                    }
                }
                else if (asset.Contains("SoundTable_", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (AniEventToolWindow.IsOpened)
                    {
#if USE_CHJ_SOUND
                        Debug.Log("SoundTable file modified: " + asset);
                        SoundManager.Instance.Initialize();
#endif
                    }
                }
            }
        }
    }

}