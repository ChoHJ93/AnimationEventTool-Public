#if UNITY_EDITOR
namespace AniEventTool
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    using UnityEditor;
    using AniEventTool.Editor;
    using static UnityEditor.Profiling.RawFrameDataView;

    public class AniSpeedEventTrack : AniEventTrack<AniSpeedEvent>
    {
        public AniSpeedEvent aniSpeedEvent => data;

        [SerializeField] public float speed;
        [SerializeField] bool prevActiveState = false;

        protected override void Init(WindowState windowState, AniSpeedEvent aniEvent, AniEventGroupTrack parentTrackAsset = null)
        {
            if(aniEvent.speed <= 0)
                aniEvent.speed = 1;

            if (aniEvent.duration <= 0)
                aniEvent.endTime = aniEvent.startTime + 1;

            base.Init(windowState, aniEvent, parentTrackAsset);

            speed = aniEvent.speed;
        }
        public override void PlayEvent(float currentTime)
        {
            base.PlayEvent(currentTime);
            bool isInTime = currentTime >= startTime && currentTime <=  endTime;
            bool activeEvent = isInTime && isEnable;
            bool isStartSpeed = startTime == 0 && currentTime < TimeUtilityReflect.kTimeEpsilon;
            if (prevActiveState != activeEvent || isStartSpeed)
            {
                prevActiveState = activeEvent;
                windowState.playSpeed = activeEvent ? speed : windowState.playSpeed;
            }

        }
        public override void ApplyToEventData()
        {
            base.ApplyToEventData();
            data.speed = speed;
        }
#if UNITY_EDITOR
        public void Inspector_OnPropertiesModified() 
        {

        }
#endif
    }

}
#endif