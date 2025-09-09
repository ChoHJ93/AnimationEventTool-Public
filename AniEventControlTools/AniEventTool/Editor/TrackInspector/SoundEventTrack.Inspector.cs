using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if USE_CHJ_SOUND
using eSoundType = SoundManager.eSoundType;

namespace AniEventTool.Editor
{
    using UnityEditor;
    using AniEventTool.Editor;
    using System;

    [CustomEditor(typeof(SoundEventTrack))]
    public class SoundEventTrack_Inspector : Editor_EventTrackInspector
    {
        SoundEventTrack tgt;
        List<string> soundNameList = new List<string>();
        SerializedProperty soundInfoProp = null;
        SerializedProperty sTableNameProp = null;
        SerializedProperty sSoundNameProp = null;
        SoundManager soundManager => AniEventToolWindow.Instance.GetSoundManager;


        private void OnEnable()
        {
            if (target == null)
                return;

            tgt = (SoundEventTrack)target;

            sTableNameProp = serializedObject.FindProperty("tableName");
            sSoundNameProp = serializedObject.FindProperty("soundName");

            soundInfoProp = serializedObject.FindProperty("soundInfo");
            foreach (SoundInfo soundInfo in soundManager.Editor_GetAllSoundInfo)
            {
                soundNameList.Add(soundInfo.name);
            }

            if (string.IsNullOrWhiteSpace(sTableNameProp.stringValue) == false)
                tgt.selectedTableID = Array.IndexOf(tgt.tableNames, sTableNameProp.stringValue);
            if (tgt.selectedTableID > 0)
            {
                tgt.selectedSoundID = Array.IndexOf(tgt.soundInfoNames, sSoundNameProp.stringValue);
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (AniEventToolWindow.Instance.IsSoundTableLoaded == false)
            {
                EditorGUILayout.HelpBox("���� ���̺� �����͸� ã�� �� �����ϴ�.", MessageType.Warning);
                return;
            }
            EditorGUI.BeginDisabledGroup(tgt.isLocked);
            serializedObject.Update();


            EditorGUI.BeginChangeCheck();
            tgt.selectedTableID = EditorGUILayout.Popup("Sound Table", tgt.selectedTableID, tgt.tableNames);
            if (EditorGUI.EndChangeCheck())
            {
                sTableNameProp.stringValue = tgt.tableNames[tgt.selectedTableID];
                tgt.selectedSoundID = 0;
                OnModified();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginDisabledGroup(tgt.selectedTableID <= 0);
            tgt.selectedSoundID = EditorGUILayout.Popup("Sound Name", tgt.selectedSoundID, tgt.soundInfoNames);
            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck())
            {
                sSoundNameProp.stringValue = tgt.soundInfoNames[tgt.selectedSoundID];
                OnModified();
            }

            if (tgt.DrawSoundInfo)
            {
                //EditorGUILayout.PropertyField(soundInfoProp);
                var nameProp = soundInfoProp.FindPropertyRelative("name");
                var memoProp = soundInfoProp.FindPropertyRelative("memo");
                var clipsProp = soundInfoProp.FindPropertyRelative("clips");
                var volumeProp = soundInfoProp.FindPropertyRelative("volume");

                float inspectorWidth = EditorGUIUtility.currentViewWidth;
                inspectorWidth -= 63;
                float nameWidth = inspectorWidth * 0.7f;
                float memoWidth = inspectorWidth - nameWidth;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Name", GUILayout.Width(35));
                nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue, GUILayout.Width(nameWidth));
                memoProp.stringValue = EditorGUILayout.TextField(memoProp.stringValue, GUILayout.Width(memoWidth));
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < clipsProp.arraySize; i++)
                {
                    SerializedProperty clipElemProp = clipsProp.GetArrayElementAtIndex(i);

                    EditorGUILayout.BeginHorizontal();
                    if (i == 0)
                    {
                        if (GUILayout.Button("+", GUILayout.Width(30)))
                        {
                            clipsProp.arraySize++;
                            volumeProp.arraySize++;
                            clipsProp.GetArrayElementAtIndex(clipsProp.arraySize - 1).objectReferenceValue = null;
                            //volumeProp.GetArrayElementAtIndex(volumeProp.arraySize - 1).floatValue = 1;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("-", GUILayout.Width(30)))
                        {
                            clipsProp.DeleteArrayElementAtIndex(i);
                            volumeProp.DeleteArrayElementAtIndex(i);
                            continue;
                        }
                    }
                    if (GUILayout.Button("��", GUILayout.Width(30)))
                    {
                        tgt.Editor_PlayClip(clipElemProp.objectReferenceValue as AudioClip);
                    }
                    clipElemProp.objectReferenceValue = EditorGUILayout.ObjectField(clipElemProp.objectReferenceValue, typeof(AudioClip), false, GUILayout.Width(nameWidth));

                    SerializedProperty volumeElemProp = volumeProp.GetArrayElementAtIndex(i);
                    float volume = volumeElemProp.floatValue;
                    EditorGUILayout.LabelField(CustomGUIStyles.audioIcon, GUILayout.Width(20));
                    volumeElemProp.floatValue = CustomEditorUtil.SetRectDraggable(new Rect(GUILayoutUtility.GetLastRect()), ref volume);
                    volumeElemProp.floatValue = EditorGUILayout.FloatField(volumeElemProp.floatValue, GUILayout.Width(55));
                    if (volumeElemProp.floatValue < 0)
                        volumeElemProp.floatValue = 0;
                    EditorGUILayout.EndHorizontal();
                }
            }


            if (serializedObject.hasModifiedProperties)
            {
                OnModified();
            }

            EditorGUI.EndDisabledGroup();
        }

        protected override void OnModified()
        {
            serializedObject.Update();
            tgt.OnInspectorModified();
            serializedObject.ApplyModifiedProperties();
            base.OnModified();
        }

    }
}
#endif