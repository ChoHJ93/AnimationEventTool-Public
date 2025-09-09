namespace AniEventTool.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    using System;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using AniEventTool.Editor;
    using UnityEngine.Events;


    //AniEventBase > AniEventTrack > AniEventTrackGUI
    [System.Serializable]
    internal static class AniEventToolEditorCache
    {
        [SerializeField]
        private static List<GameObject> m_CachedPrefabFiles = new List<GameObject>();
        private static Dictionary<string, List<AniEventGroup>> m_CachedStateEventGroupListPair = new Dictionary<string, List<AniEventGroup>>();
        private static Dictionary<AniEventBase, AniEventTrackBase> m_CachedEventTrackAsset = new Dictionary<AniEventBase, AniEventTrackBase>();
        private static Dictionary<AniEventTrackBase, EventTrackGUIBase> m_CachedEventTrackGUI = new Dictionary<AniEventTrackBase, EventTrackGUIBase>();
        public static UnityAction<AniEventTrackBase> OnTrackAdded { get; set; }
        public static UnityAction<AniEventTrackBase> OnTrackRemoved { get; set; }
#if USE_CHJ_SOUND
        [SerializeField]
        private static List<SoundTable> m_SoundTableList;
        public static List<SoundTable> SoundTableList
        {
            get
            {
                if (m_SoundTableList.IsNullOrEmpty())
                {
                    m_SoundTableList = new List<SoundTable>(Resources.LoadAll<SoundTable>(SoundManager.PATH_SOUND));
                }
                return m_SoundTableList;
            }
        }
#endif
        internal static void Clear()
        {
            EventTrackGUIBase[] eventTrackGUIs = m_CachedEventTrackGUI.Values.ToArray();
            foreach (EventTrackGUIBase trackGUI in eventTrackGUIs)
                DeleteEventTrack(trackGUI);

            m_CachedStateEventGroupListPair.Clear();
            m_CachedEventTrackGUI.Clear();
            m_CachedEventTrackAsset.Clear();
        }

        public static GameObject InstantiatePrefab(GameObject assetComponentOrGameObject, Scene destinationScene, Transform parent = null)
        {
            GameObject instantiatedObj = PrefabUtility.InstantiatePrefab(assetComponentOrGameObject, destinationScene) as GameObject;
            if (parent == null)
            {
                instantiatedObj.transform.SetParent(AniEventToolWindow.Instance.State.objectRootTr);
            }

            if (m_CachedPrefabFiles.Contains(assetComponentOrGameObject) == false)
                m_CachedPrefabFiles.Add(assetComponentOrGameObject);
            return instantiatedObj;
        }

        public static void DestroyImmediate(GameObject objInstance)
        {
            GameObject originPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(objInstance) as GameObject;

            if (originPrefab != null && m_CachedPrefabFiles.Contains(originPrefab))
            {
                m_CachedPrefabFiles.Remove(originPrefab);
            }
            GameObject.DestroyImmediate(objInstance);
        }
        public static void DestroyImmediateChildren(Transform parent)
        {
            while (0 != parent.childCount)
            {
                Transform tfChild = parent.GetChild(0);
                tfChild.SetParent(null);
                AniEventToolEditorCache.DestroyImmediate(tfChild.gameObject);
            }
        }

        internal static void CacheAllEventList(AniEventControllerBase selectedController)
        {
            m_CachedStateEventGroupListPair.Clear();

            if (selectedController == null)
                return;

            string[] aniStates = selectedController.Editor_GetAniStateNames();

            if (aniStates == null || aniStates.Length == 0)
                return;
            List<AniEventGroup> allEventGroupList = new List<AniEventGroup>();
            for (int i = 0; i < aniStates.Length; i++)
            {
                List<AniEventGroup> eventGroupList = selectedController.Editor_GetEventList(i);
                m_CachedStateEventGroupListPair.Add($"{aniStates[i]}", eventGroupList);
                allEventGroupList.AddRange(eventGroupList);
            }

            //Create All Track Assets, Track GUIs
            foreach (AniEventGroup eventGroup in allEventGroupList)
            {
                GetEventTrackGUI(eventGroup);
                List<AniEventBase> allChildrenAniEvents = GetAllChildrenAniEvent(eventGroup);
                for (int i = 0; i < allChildrenAniEvents.Count; i++) 
                {
                    GetEventTrackGUI(allChildrenAniEvents[i], eventGroup);
                }
            }
        }

        private static List<AniEventBase> GetAllChildrenAniEvent(AniEventGroup parentGroup)
        {
            List<AniEventBase> eventList = new List<AniEventBase>();
            foreach (PropertyInfo propertyInfo in typeof(AniEventGroup).GetProperties())
            {
                if (IsValidChildEventTypes(propertyInfo))
                {
                    var list = (IList)propertyInfo.GetValue(parentGroup);
                    if (list == null)
                        continue;

                    foreach (AniEventBase eventBase in list)
                    {
                        eventList.Add(eventBase);
                    }
                }
            }
            return eventList;
        }
        private static bool IsValidChildEventTypes(PropertyInfo propertyInfo)
        {
            bool isGenericType = propertyInfo.PropertyType.IsGenericType;
            if(!isGenericType)
                return false;
            bool isListType = propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>);
            bool isBaseType = propertyInfo.PropertyType.GenericTypeArguments[0].Equals(typeof(AniEventBase));
            bool isSubType = propertyInfo.PropertyType.GenericTypeArguments[0].IsSubclassOf(typeof(AniEventBase));
            return isGenericType && isListType && (isBaseType || isSubType);
        }

        #region Get-Methods
        internal static bool TryGetEventGroupList(string stateName, out List<AniEventGroup> allEventGroups_origin)
        {
            return m_CachedStateEventGroupListPair.TryGetValue(stateName, out allEventGroups_origin);
        }
        internal static EventTrackGUIBase GetEventTrackGUI(AniEventBase aniEvent, AniEventGroup parentGroup = null)
        {
            return GetEventTrackGUI_Generic(aniEvent, parentGroup);
        }
        private static EventTrackGUIBase GetEventTrackGUI_Generic<T>(T aniEvent, AniEventGroup parentGroup = null) where T : AniEventBase
        {
            AniEventTrackBase trackAsset;
            EventTrackGUIBase trackGUI;
            if (m_CachedEventTrackAsset.TryGetValue(aniEvent, out trackAsset) == false)
                trackAsset = CreateEventTrackAsset(aniEvent, parentGroup);
            if (m_CachedEventTrackGUI.TryGetValue(trackAsset, out trackGUI) == false)
                trackGUI = CreateEventTrackGUI(trackAsset);

            return trackGUI;
        }

        internal static AniEventTrackBase GetEventTrackAsset(AniEventBase aniEvent, AniEventGroup parentGroup = null)
        {
            return GetEventTrackAsset_Generic(aniEvent, parentGroup);
        }
        private static AniEventTrackBase GetEventTrackAsset_Generic<T>(T aniEvent, AniEventGroup parentGroup = null) where T : AniEventBase
        {
            AniEventTrackBase trackAsset;
            if (m_CachedEventTrackAsset.TryGetValue(aniEvent, out trackAsset) == false)
                trackAsset = CreateEventTrackAsset(aniEvent, parentGroup);
            return trackAsset;
        }
        #endregion

        #region Create-Methods
        private static AniEventTrackBase CreateEventTrackAsset<T>(T aniEvent, AniEventGroup parentGroup = null) where T : AniEventBase
        {
            AniEventTrackBase trackAsset = null;
            Type[] eventTrackTypes = CommonUtil.GetLeafDerivedTypes(typeof(AniEventTrackBase));

            //set trackAsset as instance of type that matches with aniEvent
            foreach (Type type in eventTrackTypes)
            {
                if (type.IsAbstract || type.IsGenericType)
                    continue;

                Type matchEventType = CommonUtil.GetMatchEventType(type);

                bool isAssignable = typeof(AniEventTrackBase).IsAssignableFrom(type);
                bool hasConstructor = type.GetConstructor(Type.EmptyTypes) != null;
                bool isMatchType = matchEventType.Equals(aniEvent.GetType()) || aniEvent.GetType().IsSubclassOf(matchEventType);

                if (isAssignable && hasConstructor && isMatchType)
                {
                    trackAsset = ScriptableObject.CreateInstance(type) as AniEventTrackBase; //Activator.CreateInstance(type) as AniEventTrackBase;
                    break;
                }
            }

            AniEventGroupTrack parentTrack = null;
            if (parentGroup != null && m_CachedEventTrackAsset.TryGetValue(parentGroup, out AniEventTrackBase parentTrackAsset))
                parentTrack = parentTrackAsset as AniEventGroupTrack;

            trackAsset.Init(AniEventToolWindow.Instance.State, aniEvent, parentTrack);
            m_CachedEventTrackAsset.Add(aniEvent, trackAsset);

            if (OnTrackAdded != null)
                OnTrackAdded.Invoke(trackAsset);

            return trackAsset;
        }
        private static List<AniEventTrackBase> GetChildrenEventTrackAssets(AniEventGroupTrack parentTrack)
        {
            List<AniEventTrackBase> childEventTracks = new List<AniEventTrackBase>();
            AniEventGroup aniEventGroup = parentTrack.eventGroup;
            foreach (PropertyInfo propertyInfo in typeof(AniEventGroup).GetProperties())
            {
                if (propertyInfo.PropertyType.IsGenericType &&
                    propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                    propertyInfo.PropertyType.GenericTypeArguments[0].IsSubclassOf(typeof(AniEventBase)))
                {
                    var list = (IList)propertyInfo.GetValue(aniEventGroup);
                    if (list == null)
                        continue;

                    foreach (AniEventBase eventBase in list)
                    {
                        AniEventTrackBase eventTrack = CreateEventTrackAsset(eventBase, aniEventGroup);
                        childEventTracks.Add(eventTrack);
                    }
                }
            }
            return childEventTracks;
        }

        private static EventTrackGUIBase CreateEventTrackGUI(AniEventTrackBase eventTrack)
        {

            EventTrackGUIBase eventTrackGUI = null;
            Type[] eventTrackGUITypes = CommonUtil.GetLeafDerivedTypes(typeof(EventTrackGUIBase));

            foreach (Type type in eventTrackGUITypes)
            {
                if (type.IsAbstract || type.IsGenericType)
                    continue;

                Type matchEventTrackType = CommonUtil.GetMatchEventType(type);

                bool isAssignable = typeof(EventTrackGUIBase).IsAssignableFrom(type);
                bool hasConstructor = type.GetConstructor(Type.EmptyTypes) != null;
                bool isMatchType = matchEventTrackType.Equals(eventTrack.GetType()) || eventTrack.GetType().IsSubclassOf(matchEventTrackType);

                if (isAssignable && hasConstructor && isMatchType)
                {
                    eventTrackGUI = Activator.CreateInstance(type, eventTrack) as EventTrackGUIBase;
                    break;
                }
            }
            if (eventTrackGUI == null)
            {
                Debug.LogError($"Fail to create EventTrackGUIBase for {eventTrack.GetType().Name}");
                return null;
            }

            eventTrackGUI.Init(eventTrack);
            m_CachedEventTrackGUI.Add(eventTrack, eventTrackGUI);
            return eventTrackGUI;
        }

        private static GameObject CreateCachedObject(GameObject originalPrefab)
        {
            if (PrefabUtility.GetPrefabAssetType(originalPrefab) == PrefabAssetType.NotAPrefab)
            {
                Debug.LogError($"{originalPrefab.name} is not prefab file!");
                return null;
            }

            Scene mainScene = SceneManager.GetActiveScene();
            GameObject objectInstance = AniEventToolEditorCache.InstantiatePrefab(originalPrefab, mainScene) as GameObject;
            //objectInstance.hideFlags = HideFlags.HideAndDontSave;
            objectInstance.SetActive(false);
            return objectInstance;
        }
        #endregion

        internal static bool DeleteEventTrack(EventTrackGUIBase eventTrackGUI)
        {
            AniEventBase aniEvent = eventTrackGUI.EventTrack.GetAniEvent;

            if (m_CachedEventTrackAsset == null || aniEvent == null)
                return false;

            AniEventTrackBase cachedTrackAsset = null;
            EventTrackGUIBase cachedTrackGUI = null;
            if (m_CachedEventTrackAsset.ContainsKey(aniEvent) == false)
            {
                Debug.LogError($"Fail to find selected Track Asset from Cache");
                return false;
            }
            cachedTrackAsset = m_CachedEventTrackAsset[aniEvent];

            if (m_CachedEventTrackGUI.ContainsKey(cachedTrackAsset) == false)
            {
                Debug.LogError($"Fail to find selected TrackGUI from Cache!");
                return false;
            }
            cachedTrackGUI = m_CachedEventTrackGUI[cachedTrackAsset];
            if (cachedTrackGUI.Equals(eventTrackGUI) == false)
            {
                Debug.LogError($"Selected eventTrackGUI doesn't match with cached TrackGUI");
                return false;
            }

            if (OnTrackRemoved != null)
                OnTrackRemoved.Invoke(cachedTrackAsset);
            //Remove datas from cached dictionary
            m_CachedEventTrackAsset.Remove(aniEvent);
            m_CachedEventTrackGUI.Remove(cachedTrackAsset);
            //Release references from Datas
            cachedTrackGUI.Release();
            ScriptableObject.DestroyImmediate(cachedTrackAsset as ScriptableObject);

            //foreach (var track in AllEventTrackAssets)
            //    track.Refresh();

            return true;
        }

        #region GetProperties
        internal static List<GameObject> CachedPrefabs => m_CachedPrefabFiles;
        //internal static Dictionary<AniEventTrackBase, EventTrackGUIBase>.ValueCollection AllEventTrackGUIs => m_CachedEventTrackGUI.Values;
        //internal static Dictionary<AniEventTrackBase, EventTrackGUIBase>.KeyCollection AllEventTrackAssets => m_CachedEventTrackGUI.Keys;
        internal static Dictionary<AniEventBase, AniEventTrackBase>.KeyCollection AllCachedAniEvents => m_CachedEventTrackAsset.Keys;
        #endregion
    }
}
