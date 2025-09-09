using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AniEventTool;
using AniEventTool.Editor;

namespace AniEventTool.Editor.ProjectRelated
{
    using AniEventTool.ProjectRelated;

    [CustomEditor(typeof(PR_GameEventTrack))]
    public class PR_GameEventTrackInspector : Editor_EventTrackInspector
    {
        PR_GameEventTrack tgt = null;

        SerializedProperty eEventTypeProp = null;
        SerializedProperty bCancelSkill = null;

        private eGameEventType eventType => (eGameEventType)eEventTypeProp.enumValueFlag;

        private void OnEnable()
        {
            if (target == null)
                return;

            tgt = (PR_GameEventTrack)target;

            eEventTypeProp = serializedObject.FindProperty("gameEventType");
            bCancelSkill = serializedObject.FindProperty("cancelSkill");

            tgt.Inspector_OnPropertiesModified();

        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (serializedObject == null)
                return;

            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(tgt.isLocked);
            DrawGUI_EventTypePopup();

            switch (eventType)
            {
                case eGameEventType.NextSkill:
                    DrawGUI_NextSkill();
                    break;
                case eGameEventType.EnableMove:
                    DrawGUI_EnableMove();
                    break;
            }

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                tgt.Inspector_OnPropertiesModified();
                OnModified();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawGUI_EventTypePopup()
        {
            string[] enumNames = System.Enum.GetNames(typeof(eGameEventType));
            List<string> displayedOptions = new List<string>(enumNames);

            int selectedIndex = displayedOptions.IndexOf(tgt.gameEventType.ToString());

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            GUIStyle labelStyle = EditorStyles.boldLabel;
            labelStyle.normal.textColor = Color.yellow;
            EditorGUILayout.LabelField("�̺�Ʈ ����", labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
            selectedIndex = EditorGUILayout.Popup(selectedIndex, displayedOptions.ToArray());
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex >= 0)
                {
                    tgt.gameEventType = (eGameEventType)System.Enum.Parse(typeof(eGameEventType), displayedOptions[selectedIndex]);
                }
            }
        }

        private void DrawGUI_NextSkill()
        {
            CustomEditorUtil.AutoWrapLabelField("���̺�� ���� ��ų(Next Skill ID) ���� �ִٸ� ���� Ű �Է� �� ���� ��ų�� ����");
        }

        private void DrawGUI_EnableMove()
        {
            GUIContent cancelSkillLabel = new GUIContent("��ų ���");
            EditorGUILayout.PropertyField(bCancelSkill, cancelSkillLabel);
            string message = bCancelSkill.boolValue ? "�ִϸ��̼��� ���ݵ� '������� ��ų ���' �� �̵� ����" : "��ų ��� �߿� �̵� Ű �Է��� ���� �̵� ����";
            CustomEditorUtil.AutoWrapLabelField(message);
        }
    }
}