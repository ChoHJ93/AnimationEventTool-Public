namespace AniEventTool
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class InGameAniInfo
    {
        public int stateHash;
        public string stateName;
        public string clipName;

        public AnimationClip clip;
        public float inverseClipLength = 1;

        public float endTime;
        [System.Obsolete]
        public float cutFrame;

        public InGameAniInfo() { }
    }

    public class InGameAniEventGroup
    {
        public List<AniEventBase> aniEvents { get; set; }
        public int EventsCount => aniEvents.Count;//effects?.Count + sounds?.Count + moveDatas?.Count + aniSpeedDatas?.Count + hitEvents?.Count + gameEvents?.Count ?? 0;

        private bool _isPlaying = false;
        public bool isPlaying
        {
            get { return _isPlaying; }
            set
            {
                _isPlaying = value;
                if (!value)
                {
                    ResetPlayingState(aniEvents);
                    return;
                }
            }
        }
        private void ResetPlayingState<T>(List<T> aniEvents) where T : AniEventBase
        {
            foreach (T evt in aniEvents)
                evt.isPlaying = false;
        }

        public InGameAniEventGroup(List<AniEventGroup> eventGroups)
        {
            aniEvents = new List<AniEventBase>();
            foreach (var group in eventGroups)
            {
                if (group.aniEvents.IsNullOrEmpty() == false)
                    aniEvents.AddRange(group.aniEvents);
            }
            isPlaying = false;

            return;
        }
    }
}