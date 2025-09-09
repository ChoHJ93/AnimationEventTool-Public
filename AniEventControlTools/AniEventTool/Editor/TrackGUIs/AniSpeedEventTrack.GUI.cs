using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    using System;
    using UnityEditor;
    using AniEventTool.Editor;

    internal class AniSpeedEventTrackGUI : EventTrackGUI<AniSpeedEventTrack>
    {
        private AniSpeedEventTrack m_Track => eventTrack as AniSpeedEventTrack;

        public override void Init(AniEventTrackBase aniEventTrack)
        {
            base.Init(aniEventTrack);
        }
        public override void SetContentBounds(Rect bounds)
        {
            bounds.xMax = bounds.x + bounds.height;
            base.SetContentBounds(bounds);
        }
        public override void DrawContent(float rectX, float contentViewWidth)
        {
            if (contentViewWidth < WindowConstants.trackContentFieldMin)
                return;

            contentViewWidth = controlRect.height;
            base.DrawContent(rectX, contentViewWidth);
        }
        protected override void DrawContent(Rect contentRect)
        {
            Rect iconRect = new Rect(contentRect);
            iconRect.size *= 0.7f;
            iconRect.x = contentRect.x - iconRect.width * 0.5f;
            iconRect.y += iconRect.height * 0.25f;

            if (CustomGUIStyles.eventKeyIcon_Gray.image != null)
            {
                Color iconColor = isSelected ? Color.white : Color.gray;
                GUI.DrawTexture(iconRect, CustomGUIStyles.eventKeyIcon_Gray.image, ScaleMode.ScaleToFit, true, 1, iconColor, 0, 0);
            }
            else
                base.DrawContent(contentRect);

            Rect fSpeedLabelRect = new Rect(contentRect);
            fSpeedLabelRect.x += iconRect.width * 0.5f;
            fSpeedLabelRect.y += WindowConstants.trackBindingPadding * 0.6f;
            fSpeedLabelRect.width = contentRect.width + 12;
            fSpeedLabelRect.height -= 10;

            if (contentRect.width < WindowConstants.trackContentFieldMin)
                return;


            //EditorGUI.DrawRect(fSpeedLabelRect, isSelected ? CustomGUIStyles.colorSelectedContentBackground : CustomGUIStyles.colorContentBackground);
            Color guiColor = GUI.color;
            GUI.color = Color.white;//CustomGUIStyles.colorEventListBackground;
            EditorGUI.LabelField(fSpeedLabelRect, $"x{m_Track.speed:F2}");
            GUI.color = guiColor;
        }
        public override void OnContentClicked()
        {
            base.OnHeaderClicked();
        }
    }

}