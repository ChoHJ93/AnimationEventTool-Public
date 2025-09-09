#if UNITY_EDITOR
namespace AniEventTool
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using UnityEngine;
    using UnityEngine.SceneManagement;
#if UNITY_EDITOR
    using UnityEditor;
    using AniEventTool.Editor;
#endif

    public abstract class AniEventTrackBase : ScriptableObject
    {
        protected WindowState windowState;
        public virtual string eventName { get; set; }
        public virtual float startTime { get; set; }
        public virtual float endTime { get; set; }
        public virtual bool isEnable { get; set; }
        public virtual bool isLocked { get; set; }
        public virtual void Init(WindowState windowState, AniEventBase aniEvent, AniEventGroupTrack parentTrackAsset) { }
        public virtual void ApplyToEventData() { }
        public virtual AniEventGroupTrack ParentGroupTrack { get; }
        public virtual AniEventBase GetAniEvent { get; }
        public virtual CachedObject GetCachedObject { get; }
        public virtual void OnResourceModified() { }
        public virtual void MoveTime(float movedTime) { }
        public virtual void PlayEvent(float currentTime) { }
        public virtual void StopEvent() { }
        public virtual void OnRelease() { }
        public virtual void Refresh() { }
        public float duration => Mathf.Max(0, endTime - startTime);
    }

    public class AniEventTrack<T> : AniEventTrackBase where T : AniEventBase
    {
        protected AniEventGroupTrack parentTrack;
        protected T data;
        protected CachedObject cachedObject;


        public override AniEventGroupTrack ParentGroupTrack => parentTrack;
        public override AniEventBase GetAniEvent => data;
        public override CachedObject GetCachedObject => cachedObject;

        public int groupId;
        public int index;

        public override void Init(WindowState windowState, AniEventBase aniEvent, AniEventGroupTrack parentTrackAsset = null)
        {
            if (aniEvent is T eventInstance)
                Init(windowState, eventInstance, parentTrackAsset);
        }
        protected virtual void Init(WindowState windowState, T aniEvent, AniEventGroupTrack parentTrackAsset = null)
        {
            if (windowState == null)
                throw new ArgumentException("windowState parameter can't be null!");
            if (aniEvent == null)
                throw new ArgumentException("aniEvent parameter can't be null!");

            string trackName = aniEvent?.name ?? aniEvent.GetType().Name;
            if (string.IsNullOrEmpty(trackName) || trackName == "AniEventBase" || trackName == "AniEvent")
            {
                throw new ArgumentException($"AniEvent name can't be null or empty! AniEvent Type: {aniEvent.GetType().Name}");
            }

            this.windowState = windowState;
            data = aniEvent;
            parentTrack = parentTrackAsset;

            isEnable = true;
            isLocked = false;

            groupId = parentTrackAsset?.index ?? -1;
            Type type = aniEvent.GetType();
            index = parentTrackAsset?.Editor_GetSameTrackCount(aniEvent) ?? aniEvent.index;

            eventName = string.IsNullOrEmpty(aniEvent?.name) ? $"{GetType().Name}{index:D2}" : aniEvent.name;
            eventName = eventName.RemoveAllOccurrences("Track");
            eventName = eventName.RemoveAllOccurrences("PR_");

            startTime = aniEvent.startTime;
            endTime = aniEvent.endTime;

            if (parentTrackAsset != null && aniEvent is not AniEventGroup)
            {
                parentTrackAsset.ChildEventTracks.Add(this);
            }
        }

        public override void OnRelease()
        {
            if (cachedObject?.ObjectInstance != null)
                cachedObject.ObjectInstance.SetActive(false);
        }

        public virtual void SetPrefabObjectData(GameObject originalPrefab, out GameObject prefabInstance, bool setEndTime = false)
        {
            prefabInstance = null;
            if (originalPrefab == null)
            {
                if (cachedObject != null)
                    cachedObject.ReNew(originalPrefab, null);
                ClearData();
                isLocked = false;
                startTime = 0;
                endTime = 0;


                return;
            }

            if (PrefabUtility.GetPrefabAssetType(originalPrefab) == PrefabAssetType.NotAPrefab)
            {
                Debug.LogError($"{originalPrefab.name} is not prefab file!");
                return;
            }

            Scene mainScene = SceneManager.GetActiveScene();
            prefabInstance = PrefabUtility.InstantiatePrefab(originalPrefab, mainScene) as GameObject;
            prefabInstance.transform.SetParent(windowState.objectRootTr, false);
            //objectInstance.hideFlags = HideFlags.HideAndDontSave;
            prefabInstance.SetActive(false);

            if (cachedObject == null)
                cachedObject = new CachedObject(originalPrefab, prefabInstance);
            else
                cachedObject.ReNew(originalPrefab, prefabInstance);
        }
        protected virtual void OnDestroy()
        {
            if (parentTrack != null)
                parentTrack.ChildEventTracks.Remove(this);
            if (cachedObject != null)
                cachedObject.Clear();

            data = null;
        }

        public override void PlayEvent(float currentTime)
        {
            if (isEnable == false)
                return;
        }

        public override void MoveTime(float movedTime)
        {
            float duration = endTime - startTime;
            float moveStart = Math.Max(0, startTime + movedTime);

            startTime = moveStart;
            endTime = duration > 0 ? moveStart + duration : endTime;
        }
        protected virtual void ClearData()
        {
            if (data != null)
            {
                data.startTime = default;
                data.endTime = default;
            }
        }
        public override void ApplyToEventData()
        {
            data.groupId = groupId;
            data.index = index;
            data.name = eventName;
            data.startTime = startTime;
            data.endTime = endTime < 0 ? 0 : endTime;
        }
    }
}
#endif