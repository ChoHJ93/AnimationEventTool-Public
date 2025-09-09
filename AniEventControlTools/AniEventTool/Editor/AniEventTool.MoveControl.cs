using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    public partial class AniEventToolWindow
    {
        List<MoveEventTrackBase> m_MoveTrackList = new List<MoveEventTrackBase>();
        

        protected void OnMoveEventAdded(AniEventTrackBase eventTrack)
        {
            if(eventTrack.GetType().IsSubclassOf(typeof(MoveEventTrackBase)) == true)
            {
                MoveEventTrackBase moveEventTrack = eventTrack as MoveEventTrackBase;
                if (m_MoveTrackList.Contains(moveEventTrack))
                    return;
                m_MoveTrackList.Add(moveEventTrack);
                m_MoveTrackList.Sort((evt1, evt2) => evt1.startTime.CompareTo(evt2.startTime));

                RefreshControllerTransform();
            }
        }
        protected void OnMoveEventRemoved(AniEventTrackBase eventTrack)
        {
            if (eventTrack.GetType().IsSubclassOf(typeof(MoveEventTrackBase)) == true)
            {
                MoveEventTrackBase moveEventTrack = eventTrack as MoveEventTrackBase;
                if (!m_MoveTrackList.Contains(moveEventTrack))
                    return;
                m_MoveTrackList.Remove(moveEventTrack);
                m_MoveTrackList.Sort((evt1, evt2) => evt1.startTime.CompareTo(evt2.startTime));

                RefreshControllerTransform();
            }
        }
        public void OnMoveEventEdited()
        {
            if (m_MoveTrackList.IsNullOrEmpty())
                return;

            m_MoveTrackList.Sort((evt1, evt2) => evt1.startTime.CompareTo(evt2.startTime));
            RefreshControllerTransform();
        }

        private void UpdateControllerTransform(float currentTime)
        {
            MoveEventTrackBase currentTrack = null;
            Vector3 endPos = Vector3.zero;
            int lastTrackIndex = 0;
            if (m_MoveTrackList.IsNullOrEmpty() == false)
            {
                for (int i = 0; i < m_MoveTrackList.Count; i++)
                {
                    if (m_MoveTrackList[i].GetAniEvent is not MoveEventBase)
                        continue;
                    MoveEventTrackBase curTrack = m_MoveTrackList[i];
                    if (i == 0)
                        curTrack.endPosition = endPos;
                    else
                    {
                        MoveEventTrackBase lastTrack = m_MoveTrackList[i - 1];
                        float lastEventEndTime = lastTrack.endTime < curTrack.startTime ? lastTrack.duration : curTrack.startTime - lastTrack.startTime;
                        float t = Mathf.Min(curTrack.startTime, lastEventEndTime);
                        curTrack.endPosition = endPos + lastTrack.direction.normalized * CustomMathUtils.GetDistanceAtTime(lastTrack.simpleSpeed, lastEventEndTime);

                        if (m_MoveTrackList[i - 1].endTime < currentTime && currentTime < curTrack.startTime)
                            endPos = curTrack.endPosition;

                        if (curTrack.direction.magnitude > 0)
                            lastTrackIndex = i;
                    }

                    if (curTrack.direction.magnitude > 0 && currentTime >= curTrack.startTime && currentTime <= curTrack.endTime)
                    {
                        currentTrack = curTrack;
                    }
                }
                MoveEventTrackBase lastMoveEvent = m_MoveTrackList[lastTrackIndex];//[m_MoveTrackList.Count - 1];
                if (currentTime > lastMoveEvent.endTime)
                    currentTrack = lastMoveEvent;
            }
            else
                SelectedController.Editor_ResetTransform();

            if (currentTrack != null)
                SelectedController.Editor_SetMoveEvent(currentTrack.EditedEvent, currentTrack.endPosition);
            else
                SelectedController.Editor_SetMoveEvent(null, endPos);

            if(SelectedController != null && State.SelectedClip != null)
            SelectedController.Editor_UpdateTransform(currentTime);
        }

        private void RefreshControllerTransform()
        {
            if (SelectedController == null)
                return;

            SelectedController.Editor_ResetTransform();
            UpdateControllerTransform((float)m_State.time);
        }
    }
}