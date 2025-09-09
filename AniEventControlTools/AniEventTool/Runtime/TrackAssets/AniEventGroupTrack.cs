using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
namespace AniEventTool
{
    public class AniEventGroupTrack : AniEventTrack<AniEventGroup>
    {
        List<AniEventTrackBase> childEventTracks = new List<AniEventTrackBase>();
        public List<AniEventTrackBase> ChildEventTracks => childEventTracks;
        public override AniEventGroupTrack ParentGroupTrack => null;
        public override CachedObject GetCachedObject => null;
        /// <summary>
        /// return AniEvent of it self
        /// </summary>
        public AniEventGroup eventGroup => data;

#if UNITY_EDITOR
        public bool foldoutState { get; set; }

        public int Editor_GetSameTrackCount<T>(T aniEvent) where T : AniEventBase
        {
            if (aniEvent == null)
                return 0;
            int count = 0;
            foreach (var childTrack in ChildEventTracks)
            {
                if (childTrack is AniEventTrack<T> aniEventTrack && aniEventTrack.GetAniEvent.GetType() == aniEvent.GetType())
                {
                    count++;
                }
            }
            return count;
        }
#endif

        protected override void Init(WindowState windowState, AniEventGroup aniEvent, AniEventGroupTrack parentTrackAsset = null)
        {
            base.Init(windowState, aniEvent);
            foldoutState = true;

            if (string.IsNullOrWhiteSpace(aniEvent.eventName))
                aniEvent.eventName = $"EventGroup{aniEvent.index:D2}";
            eventName = aniEvent.eventName;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            childEventTracks.Clear();
            childEventTracks = null;
        }
        public override void ApplyToEventData()
        {
            //base.ApplyToEventData();
            eventGroup.eventName = eventName;
            eventGroup.startTime = startTime;
            eventGroup.endTime = endTime;
            eventGroup.index = index;
            SetEventTime();
            foreach (var eventTrack in ChildEventTracks)
            {
                eventTrack.ApplyToEventData();
                SetAniEvents(eventTrack.GetAniEvent);
            }
        }

        private void SetEventTime()
        {
            float startTime = float.MaxValue;
            float endTime = 0;
            foreach (var eventTrack in ChildEventTracks)
            {
                if (eventTrack.startTime < startTime)
                    startTime = eventTrack.startTime;
                if (eventTrack.endTime > endTime)
                    endTime = eventTrack.endTime;
            }
            this.startTime = startTime;
            this.endTime = endTime;
        }

        private void SetAniEvents(AniEventBase eventData)
        {
            if (eventData == null)
                return;
            foreach (PropertyInfo propertyInfo in typeof(AniEventGroup).GetProperties())
            {
                if (propertyInfo.PropertyType.IsGenericType &&
                    propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                    propertyInfo.PropertyType.GenericTypeArguments[0].IsSubclassOf(typeof(AniEventBase)))
                {
                    var list = (IList)propertyInfo.GetValue(eventGroup);
                    if (list == null)
                    {
                        // �� List<T> ����
                        var genericListType = typeof(List<>).MakeGenericType(propertyInfo.PropertyType.GenericTypeArguments[0]);
                        list = (IList)Activator.CreateInstance(genericListType);
                        propertyInfo.SetValue(eventGroup, list); // ���� ������ List<T>�� �ʵ忡 ����
                    }
                    Type eventType = propertyInfo.PropertyType.GenericTypeArguments[0];
                    if (eventData.GetType() == eventType)
                    {
                        list.Add(eventData);
                        propertyInfo.SetValue(eventGroup, list, null);
                    }
                }
            }

        }

        public override void MoveTime(float movedTime)
        {
            //base.MoveTime(movedTime);
        }
    }

}
#endif // UNITY_EDITOR