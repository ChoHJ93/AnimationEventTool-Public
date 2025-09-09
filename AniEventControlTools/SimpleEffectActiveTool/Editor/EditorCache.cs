namespace AniEventTool.SimpleActiveTool.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEditor;

    //using SceneManager = UnityEngine.SceneManagement.SceneManager;
    [System.Serializable]
    internal static class EditorCache
    {
        [SerializeField]
        private static Dictionary<GameObject, GameObject> m_CachedPrefabFiles = new Dictionary<GameObject, GameObject>();

        internal static void Clear()
        {
            foreach (var item in m_CachedPrefabFiles.Keys)
            {
                CheckAndApplyEditedValueToOriginal(item);
            }
            m_CachedPrefabFiles.Clear();
        }

        internal static GameObject InstantiatePrefab(GameObject prefabObj, Transform parent = null)
        {
            if (prefabObj.TryGetComponent(out EffectActiveController controller) == false)
                prefabObj.AddComponent<EffectActiveController>();

            GameObject instantiatedObj = PrefabUtility.InstantiatePrefab(prefabObj, parent) as GameObject;
            instantiatedObj.name = prefabObj.name + "(Editor)";

            if (m_CachedPrefabFiles.ContainsKey(prefabObj) == false)
            {
                m_CachedPrefabFiles.Add(prefabObj, instantiatedObj);
            }

            instantiatedObj.hideFlags = HideFlags.DontSave;//HideFlags.HideAndDontSave;
            instantiatedObj.transform.position = prefabObj.transform.localPosition;
            instantiatedObj.transform.rotation = prefabObj.transform.localRotation;
            return instantiatedObj;
        }

        internal static void DestroyPrefab(GameObject prefabObj)
        {
            if (m_CachedPrefabFiles.ContainsKey(prefabObj))
            {
                if (CheckAndApplyEditedValueToOriginal(prefabObj) == false)
                    Debug.LogError($"Error saving prefab -> {prefabObj.name} ({AssetDatabase.GetAssetPath(prefabObj)})");


                GameObject.DestroyImmediate(m_CachedPrefabFiles[prefabObj]);
                m_CachedPrefabFiles.Remove(prefabObj);
            }
        }
        private static bool CheckAndApplyEditedValueToOriginal(GameObject prefabObj)
        {
            if (m_CachedPrefabFiles.ContainsKey(prefabObj) && m_CachedPrefabFiles[prefabObj] != null)
            {
                if (m_CachedPrefabFiles[prefabObj].TryGetComponent(out EffectActiveController controllerEdited)
                    && prefabObj.TryGetComponent(out EffectActiveController controllerOrigin))
                {
                    if (controllerEdited.IsValidate == false)
                    {
                        GameObject.DestroyImmediate(controllerOrigin, true);
                    }
                    else
                    {
                        controllerEdited.SaveToOriginalPrefab();
                    }
                    PrefabUtility.SaveAsPrefabAsset(prefabObj, AssetDatabase.GetAssetPath(prefabObj), out bool success);
                    return success;
                }
            }
            return false;
        }
    }
}