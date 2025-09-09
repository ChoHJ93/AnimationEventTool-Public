using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    using System;
    using UnityEditor;
    using AniEventTool.Editor;

    internal class HitEventTrackGUI : EventTrackGUI<HitEventTrack> 
    {
        private HitEventTrack _track;
        private HitEventTrack m_Track
        {
            get
            {
                if (_track == null)
                    _track = EventTrack as HitEventTrack;
                return _track;
            }
        }

        public override void Init(AniEventTrackBase aniEventTrack)
        {
            base.Init(aniEventTrack);
        }

        public override void SetContentBounds(Rect bounds)
        {
            bounds.xMax = m_Track.hitCount == 1 ? bounds.x + bounds.height : bounds.xMax;
            base.SetContentBounds(bounds);
        }

        public override void DrawContent(float rectX, float contentViewWidth)
        {
            if (contentViewWidth < WindowConstants.trackBindingPadding)
                return;

            contentViewWidth = m_Track.hitCount == 1 ? controlRect.height : contentViewWidth;

            base.DrawContent(rectX, contentViewWidth);
        }

        protected override void DrawContent(Rect contentRect)
        {
            float hitCount = m_Track.hitCount;
            if (hitCount <= 0)
                return;

            float startTime = m_Track.startTime;
            float durationPerHit = m_Track.durationPerHit;

            Rect iconRect = new Rect(contentRect);
            iconRect.width = iconRect.height;
            iconRect.size *= 0.7f;
            iconRect.x = contentRect.x - iconRect.width * 0.5f;
            iconRect.y += iconRect.height * 0.25f;
            GUIContent icon = GetIcon(m_Track.targetType, out bool drawDuration);
            if (icon.image != null)
            {
                Color iconColor = isSelected ? Color.white : Color.gray;
                float widthPerHit = contentRect.width / hitCount; //- iconRect.width * 0.5f;
                float widthPerHitDuration = durationPerHit > 0 ? AniEventToolWindow.Instance.TrackRectWidthToTimeline(startTime, startTime + durationPerHit) : 0;
                for (int i = 0; i < m_Track.hitCount; i++)
                {
                    if (durationPerHit > 0)
                    {
                        Rect hitDurationRect = new Rect(iconRect);
                        hitDurationRect.yMin += hitDurationRect.width * 0.25f;
                        hitDurationRect.yMax -= hitDurationRect.width * 0.25f;
                        hitDurationRect.width = widthPerHitDuration;
                        hitDurationRect.xMin += iconRect.width * 0.5f;
                        EditorGUI.DrawRect(hitDurationRect, iconColor);
                    }
                    GUI.DrawTexture(iconRect, icon.image, ScaleMode.ScaleToFit, true, 1, iconColor, 0, 0);
                    iconRect.x += widthPerHit;
                }
            }

            if (drawDuration)
                base.DrawContent(contentRect);

            Rect labelRect = new Rect(contentRect);
            labelRect.x += iconRect.width * 0.5f;
            labelRect.y += WindowConstants.trackBindingPadding * 0.6f;
            labelRect.width = contentRect.width + 12;
            labelRect.height -= 10;
        }
        private GUIContent GetIcon(eTargetType targetType, out bool drawDuration)
        {
            drawDuration = false; 
            switch (targetType)
            {
                case eTargetType.Player: return CustomGUIStyles.eventKeyIcon_Yellow;
                case eTargetType.Other: return CustomGUIStyles.eventKeyIcon_Red;
                case eTargetType.Custom: return CustomGUIStyles.eventKeyIcon_Blue;
                default:
                    return CustomGUIStyles.eventKeyIcon_Gray;
            }
        }
        public override void OnContentClicked()
        {
            base.OnHeaderClicked();
        }
    }
}
