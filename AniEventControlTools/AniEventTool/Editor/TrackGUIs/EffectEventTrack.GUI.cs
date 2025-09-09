using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{

    using System;
    using UnityEditor;
    using AniEventTool.Editor;

    internal class EffectEventTrackGUI : EventTrackGUI<EffectEventTrack>
    {
        public EffectEventTrack effectEventTrack => eventTrack as EffectEventTrack;

        public override void Init(AniEventTrackBase aniEventTrack)
        {
            base.Init(aniEventTrack);
        }

        public override void DrawHeader(Rect controlRect, Rect treeViewRect)
        {
            base.DrawHeader(controlRect, treeViewRect);
        }

        protected override void DrawContent(Rect contentRect)
        {

            base.DrawContent(contentRect);

            //GUIStyle icon = CustomGUIStyles.clipIn;
            Rect particleRect = new Rect(contentRect);
            particleRect.x += WindowConstants.trackBindingPadding;
            particleRect.y += WindowConstants.trackBindingPadding;
            particleRect.width = Math.Min(contentRect.width - WindowConstants.trackContentFieldMin * 2, 200);
            particleRect.height -= 10;


            EditorGUI.BeginChangeCheck();

            if (contentRect.width >= WindowConstants.contentFieldDrawThreshold)
                effectEventTrack.particlePrefab = EditorGUI.ObjectField(particleRect, "", effectEventTrack.particlePrefab, typeof(GameObject), false) as GameObject;

            if (effectEventTrack.keep)
            {
                Rect loopIconRect = new Rect(particleRect);
                loopIconRect.x += loopIconRect.width + WindowConstants.defaultPadding;
                loopIconRect.width = loopIconRect.height;
                Color guiColor = GUI.color;
                GUI.color = CustomGUIStyles.colorEventListBackground;
                EditorGUI.LabelField(loopIconRect, CustomGUIStyles.loopIcon);
                GUI.color = guiColor;
            }
            if (EditorGUI.EndChangeCheck())
            {
                GameObject particlePrefab = effectEventTrack.particlePrefab;
                effectEventTrack.SetPrefabObjectData(particlePrefab, out GameObject prefabInstance, true);
                if (AniEventToolWindow.IsOpened)
                {
                    AniEventToolWindow.Instance.ReCalculateTimeAreaRange();
                }
            }
        }

        protected override void DrawHeaderBG(Rect rect, float indent)
        {
            base.DrawHeaderBG(rect, indent);
        }

        public override void ReDraw()
        {
            base.ReDraw();
        }

        public override void SetContentBounds(Rect bounds)
        {
            base.SetContentBounds(bounds);
        }
    }
}