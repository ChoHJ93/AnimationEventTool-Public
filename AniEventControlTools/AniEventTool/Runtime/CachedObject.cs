using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AniEventTool
{
    [System.Serializable]
    public class CachedObject
    {
        public string PrefabGUID { get; private set; }
        public GameObject PrefabObject { get; private set; }
        public GameObject ObjectInstance { get; set; }

        public CachedObject(string id, GameObject instance) 
        {
            this.PrefabGUID = id;
            this.ObjectInstance = instance;
        }

        public CachedObject(GameObject prefab, GameObject instance)
        {
#if UNITY_EDITOR
            PrefabGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
#endif
            PrefabObject = prefab;
            ObjectInstance = instance;
        }

        public void ReNew(string id, GameObject instance) 
        {
            Clear();
            PrefabGUID = id;
            ObjectInstance = instance;
        }
        public void ReNew(GameObject prefab, GameObject instance)
        {
            Clear();
#if UNITY_EDITOR
            PrefabGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
#endif
            PrefabObject = prefab;
            ObjectInstance = instance;
        }

        public void Clear()
        {
            if (ObjectInstance)
                Object.DestroyImmediate(ObjectInstance);
            PrefabGUID = null;
            PrefabObject = null;
            ObjectInstance = null;
        }
    }
}