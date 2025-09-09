using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if USE_CHJ_SOUND
namespace AniEventTool.Editor
{
    using System;
    using UnityEditor;
    using AniEventTool.Editor;

    internal class SoundEventTrackGUI : EventTrackGUI 
    {
        public SoundEventTrack soundEventTrack => eventTrack as SoundEventTrack;

        public SoundEventTrackGUI(AniEventTrackBase eventTrack) : base(eventTrack)
        {
            if (eventTrack is not SoundEventTrack)
                throw new InvalidOperationException($"Track GUI Type({typeof(SoundEventTrackGUI).Name}) is not Match with Event Track{eventTrack.GetType().Name}");
        }

        public override void DrawHeader(Rect controlRect, Rect treeViewRect)
        {
            base.DrawHeader(controlRect, treeViewRect);
        }

        protected override void DrawContent(Rect contentRect)
        {
            base.DrawContent(contentRect);
        }

        protected override void DrawHeaderBG(Rect rect, float indent)
        {
            base.DrawHeaderBG(rect, indent);
        }

        //Sound Name ǥ��
        protected override void DrawTrackLabel(Rect rect, float allButtonWitdh)
        {
            //base.DrawTrackLabel(rect, allButtonWitdh);

            Rect labelRect = new Rect(rect);
            labelRect.xMin += 10;
            labelRect.y += 6f;
            labelRect.width -= allButtonWitdh;
            labelRect.width = Math.Max(labelRect.width, 30);
            labelRect.height = WindowConstants.eventLabelHeight;

            string soundTrackName = string.IsNullOrEmpty(soundEventTrack.soundName) ? "No Sound" : soundEventTrack.soundName;
            EditorGUI.LabelField(labelRect, soundTrackName);
        }

        public override void ReDraw()
        {
            base.ReDraw();
        }

        public override void SetContentBounds(Rect bounds)
        {
            base.SetContentBounds(bounds);
        }

        public override void OnContentClicked()
        {
            base.OnHeaderClicked();
        }
    }
}
#endif