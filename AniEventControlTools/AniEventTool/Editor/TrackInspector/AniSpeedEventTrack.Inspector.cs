using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    using UnityEditor;
    using AniEventTool.Editor;

    [CustomEditor(typeof(AniSpeedEventTrack))]
    public class AniSpeedEventTrack_Inspector : Editor_EventTrackInspector
    {
        AniSpeedEventTrack tgt = null;

        SerializedProperty fSpeedProp = null;

        private void OnEnable()
        {
            if (target == null)
                return;
            tgt = (AniSpeedEventTrack)target;

            fSpeedProp = serializedObject.FindProperty("speed");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUI.BeginDisabledGroup(tgt.isLocked);
            EditorGUILayout.PropertyField(fSpeedProp, new GUIContent("Speed"));

            EditorGUI.BeginChangeCheck();
            float duration = Mathf.Max(0, tgt.endTime - tgt.startTime);
            duration = EditorGUILayout.FloatField("Time", duration);
            if (EditorGUI.EndChangeCheck())
            {
                tgt.endTime = duration + tgt.startTime;
                OnModified();
            }
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                OnModified();
            }
            EditorGUI.EndDisabledGroup();
        }

        protected override void OnModified()
        {
            tgt.Inspector_OnPropertiesModified();
            base.OnModified();
        }
    }
}
