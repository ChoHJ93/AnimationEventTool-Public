using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    using UnityEditor;
    using AniEventTool.Editor;
    using UnityEditorInternal;
    
    [CustomEditor(typeof(HitEventTrack))]
    public class HitEventTrack_Inspector : Editor_EventTrackInspector
    {
        const string warningMessage_HitCountLimit = "Hit Count�� �ʹ� ª�� �������� �߻��˴ϴ�!\nHit Count ������ ���̰ų� LifeTime�� �÷��ּ���.";

        HitEventTrack tgt = null;

        SerializedProperty nLayerMaskProp = null;
        SerializedProperty eTargetTypeProp = null;
        SerializedProperty nTargetCountProp = null;
        SerializedProperty nHitCountProp = null;
        SerializedProperty fDurationPerHitProp = null;
        SerializedProperty fLifeTimeProp = null;
        SerializedProperty bStiffEffect = null;
        SerializedProperty bAttachProp = null;
        SerializedProperty bFollowParentRotProp = null;

        SerializedProperty bLimitTargetProp = null;
        SerializedProperty bInvokeMultipleProp = null;

        SerializedProperty lstRangesProp = null;
        SerializedProperty eGizmoStyleProp = null;
        SerializedProperty fGizmoAlphaProp = null;

        ReorderableList rangelist;

        private void OnEnable()
        {
            if (target == null)
                return;

            tgt = target as HitEventTrack;

            nLayerMaskProp = serializedObject.FindProperty("layerMask");
            eTargetTypeProp = serializedObject.FindProperty("targetType");
            nTargetCountProp = serializedObject.FindProperty("targetCount");
            bAttachProp = serializedObject.FindProperty("attach");
            bFollowParentRotProp = serializedObject.FindProperty("followParentRot");
            fLifeTimeProp = serializedObject.FindProperty("lifeTime");
            bStiffEffect = serializedObject.FindProperty("stiffEffect");
            nHitCountProp = serializedObject.FindProperty("hitCount");
            fDurationPerHitProp = serializedObject.FindProperty("durationPerHit");

            bLimitTargetProp = serializedObject.FindProperty("limitTargetCount");
            bInvokeMultipleProp = serializedObject.FindProperty("invokeMultiple");

            lstRangesProp = serializedObject.FindProperty("ranges");
            eGizmoStyleProp = serializedObject.FindProperty("gizmoStyle");
            fGizmoAlphaProp = serializedObject.FindProperty("gizmoAlpha");

            rangelist = new ReorderableList(serializedObject, lstRangesProp);

            rangelist.drawHeaderCallback += (rect) =>
            {
                EditorGUI.LabelField(rect, "Range List");
            };
            rangelist.onAddCallback += (list) =>
            {
                lstRangesProp.arraySize++;
                list.index = lstRangesProp.arraySize - 1;
                lstRangesProp.GetArrayElementAtIndex(list.index).FindPropertyRelative("rangeType").enumValueFlag = (int)eRangeType.Ray;
                lstRangesProp.GetArrayElementAtIndex(list.index).FindPropertyRelative("size").vector3Value = Vector3.one;
                lstRangesProp.GetArrayElementAtIndex(list.index).FindPropertyRelative("radius").floatValue = 1;
            };

            tgt.Inspector_OnPropertiesModified();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (serializedObject == null)
                return;

            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(tgt.isLocked);
            DrawGUI_HitType();
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                tgt.Inspector_OnPropertiesModified();
                OnModified();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawGUI_HitType()
        {
            EditorGUILayout.LabelField("�̺�Ʈ ���� ����", style_Bold);
            EditorGUILayout.PropertyField(eTargetTypeProp);
            EditorGUILayout.PropertyField(nLayerMaskProp);
            eTargetType targetType = (eTargetType)eTargetTypeProp.intValue;
            if (targetType != eTargetType.Custom)
                nLayerMaskProp.intValue = tgt.Inspector_GetLayerMask(targetType);

            EditorGUILayout.BeginHorizontal();
            bool limitTarget = bLimitTargetProp.boolValue;
            EditorGUI.BeginDisabledGroup(limitTarget == false);
            nTargetCountProp.intValue = limitTarget ? Mathf.Max(1, nTargetCountProp.intValue) : 0;
            if (limitTarget)
                nTargetCountProp.intValue = EditorGUILayout.IntField(nTargetCountProp.displayName, nTargetCountProp.intValue);
            else
                EditorGUILayout.TextField(nTargetCountProp.displayName, "No Limit");
            EditorGUI.EndDisabledGroup();

            bLimitTargetProp.boolValue = EditorGUILayout.Toggle("", bLimitTargetProp.boolValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool invokeMultiple = bInvokeMultipleProp.boolValue;
            EditorGUI.BeginDisabledGroup(invokeMultiple == false);
            nHitCountProp.intValue = invokeMultiple ? Mathf.Max(2, nHitCountProp.intValue) : 1;
            EditorGUILayout.PropertyField(nHitCountProp);
            EditorGUI.EndDisabledGroup();
            bInvokeMultipleProp.boolValue = EditorGUILayout.Toggle("", bInvokeMultipleProp.boolValue);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(fDurationPerHitProp);
            fDurationPerHitProp.floatValue = Mathf.Max(0, fDurationPerHitProp.floatValue);
            if (fDurationPerHitProp.floatValue == 0)
                EditorGUILayout.LabelField("0 = one frame");
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginDisabledGroup(invokeMultiple == false);
            fLifeTimeProp.floatValue = invokeMultiple ? Mathf.Max(0.04f, fLifeTimeProp.floatValue) : 0;
            EditorGUILayout.PropertyField(fLifeTimeProp);
            EditorGUI.EndDisabledGroup();

            bool overHitCount = nHitCountProp.intValue > 1 && fLifeTimeProp.floatValue * 100 / nHitCountProp.intValue < 2f - CustomMathUtils.EPSILON;
            if (overHitCount)
                EditorGUILayout.HelpBox(warningMessage_HitCountLimit, MessageType.Warning);

            EditorGUILayout.PropertyField(bStiffEffect);

            EditorGUILayout.LabelField("���� ǥ�� ����", style_Bold);
            EditorGUILayout.PropertyField(eGizmoStyleProp);
            fGizmoAlphaProp.floatValue = EditorGUILayout.Slider("Color Alpha", fGizmoAlphaProp.floatValue, 0, 1);
            fGizmoAlphaProp.floatValue = Mathf.Clamp01(fGizmoAlphaProp.floatValue);

            EditorGUILayout.LabelField("���� ���� ����", style_Bold);
            EditorGUILayout.PropertyField(bAttachProp);
            EditorGUILayout.PropertyField(bFollowParentRotProp);

            float prevArraySize = lstRangesProp.arraySize;
            EditorGUILayout.PropertyField(lstRangesProp);
            bool showWarning = false;
            if (lstRangesProp.arraySize > 0)
            {
                for (int i = 0; i < lstRangesProp.arraySize; i++)
                {
                    if (lstRangesProp.GetArrayElementAtIndex(i).FindPropertyRelative("rangeType").enumValueFlag == (int)eRangeType.Capsule)
                    {
                        showWarning = true;
                        break;
                    }
                }
            }
            if (showWarning)
            {
                EditorGUILayout.HelpBox("���� Capsule Ÿ���� ���� ǥ�ð� ����� ���� �ʽ��ϴ�!\nBoxŸ���� ������ּ���.", MessageType.Warning);
            }

            if (serializedObject.hasModifiedProperties)
            {
                if (lstRangesProp.arraySize > 0)
                {
                    for (int i = 0; i < lstRangesProp.arraySize; i++)
                    {
                        float radius = lstRangesProp.GetArrayElementAtIndex(i).FindPropertyRelative("radius").floatValue;
                        lstRangesProp.GetArrayElementAtIndex(i).FindPropertyRelative("radius").floatValue = Mathf.Max(0, radius);
                    }
                }
                if (lstRangesProp.arraySize > prevArraySize)
                {
                    int lastIndex = lstRangesProp.arraySize - 1;
                    lstRangesProp.GetArrayElementAtIndex(lastIndex).FindPropertyRelative("rangeType").enumValueFlag = (int)eRangeType.Ray;
                    lstRangesProp.GetArrayElementAtIndex(lastIndex).FindPropertyRelative("size").vector3Value = Vector3.one;
                    lstRangesProp.GetArrayElementAtIndex(lastIndex).FindPropertyRelative("radius").floatValue = 1;
                }
            }
        }
    }
}
