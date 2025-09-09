using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    using UnityEditor;
    using AniEventTool.Editor;

    [CustomEditor(typeof(AniEventGroupTrack))]
    public class AniEventGroupTrack_Inspector : Editor_EventTrackInspector
    {
        AniEventGroupTrack tgt;

        private void OnEnable()
        {
            tgt = target as AniEventGroupTrack;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}