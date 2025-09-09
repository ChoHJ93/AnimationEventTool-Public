using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    using UnityEditor;
    using AniEventTool.Editor;

    [CustomEditor(typeof(EffectEventTrack))]
    public class EffectEventTrack_Inspector : Editor_EventTrackInspector
    {
        enum DeavtiveType
        {
            SetActive,
            LoopValue
        }

        const string undoName = "AnimEventTool-EffectTrack-Inspector";
        const string lifeTimeTooltip = "�ִϸ��̼��� ��ȯ �� ����Ʈ ��Ȱ��ȭ �� �� ������ ��";
        readonly string deactiveTypeToolTip = $"Loop ��ƼŬ�� ��Ȱ�� ȭ �� �� Ÿ��.\n {DeavtiveType.SetActive}: ������Ʈ�� ��Ȱ��ȭ \n{DeavtiveType.LoopValue} : loop ���� off ó��";

        EffectEventTrack tgt = null;

        SerializedProperty bAttachProp = null;
        SerializedProperty bAttachToBoneProp = null;
        SerializedProperty bStartOnBoneProp = null;
        SerializedProperty bFollowParentRotProp = null;
        SerializedProperty nSelectedIndexProp = null;
        SerializedProperty vAddedPositionProp = null;
        SerializedProperty vAddedRotationProp = null;
        SerializedProperty bKeepProp = null;
        SerializedProperty fAliveTimeProp = null;
        SerializedProperty bDeactiveLoopProp = null;
        SerializedProperty bIgnoreYProp = null;
        SerializedProperty bDetachOnEnd = null;
        SerializedProperty bManualDetach = null;
        SerializedProperty fbDetachTime = null;

        bool attach => bAttachProp.boolValue;
        bool attachToBone => bAttachToBoneProp.boolValue;
        bool startOnBone => bStartOnBoneProp.boolValue;
        bool useBone => attachToBone || startOnBone;
        bool hasLoopPS => tgt.hasLoopPS;

        private void OnEnable()
        {
            if (target == null)
                return;

            tgt = (EffectEventTrack)target;
            //gui = AniEventToolEditorCache.GetEventTrackGUI(tgt.GetAniEvent, tgt.ParentGroupTrack.GetAniEvent as AniEventGroup) as EffectEventTrackGUI;
            //tgt.Init_Inspector();

            bAttachProp = serializedObject.FindProperty("attach");
            bAttachToBoneProp = serializedObject.FindProperty("attachToBone");
            bStartOnBoneProp = serializedObject.FindProperty("startOnBone");
            bFollowParentRotProp = serializedObject.FindProperty("followParentRot");
            nSelectedIndexProp = serializedObject.FindProperty("selectIndex");
            vAddedPositionProp = serializedObject.FindProperty("addedPosition");
            vAddedRotationProp = serializedObject.FindProperty("addedRotation");
            bKeepProp = serializedObject.FindProperty("keep");
            fAliveTimeProp = serializedObject.FindProperty("lifeTime");
            bDeactiveLoopProp = serializedObject.FindProperty("deactiveLoop");
            bIgnoreYProp = serializedObject.FindProperty("ignoreY");
            bDetachOnEnd = serializedObject.FindProperty("detachOnEnd");
            bManualDetach = serializedObject.FindProperty("manualDetach");
            fbDetachTime = serializedObject.FindProperty("detachTime");

            //tgt.Inspector_TransformOptionsEditted();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginDisabledGroup(tgt.isLocked);
            {
                EditorGUI.BeginChangeCheck();
                tgt.particlePrefab = EditorGUILayout.ObjectField("Prefab File", tgt.particlePrefab, typeof(GameObject), false) as GameObject;
                if (EditorGUI.EndChangeCheck())
                {
                    GameObject particlePrefab = tgt.particlePrefab;
                    tgt.SetPrefabObjectData(particlePrefab, out GameObject prefabInstance, true);
                    if (AniEventToolWindow.IsOpened)
                    {
                        AniEventToolWindow.Instance.ReCalculateTimeAreaRange();
                    }
                }

                if (tgt.particleInstance == null)
                    return;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Object in Hierarchy", tgt.particleInstance, typeof(GameObject), true);
                EditorGUI.EndDisabledGroup();

                serializedObject.Update();

                EditorGUI.BeginChangeCheck();
                {
                    bAttachProp.boolValue = EditorGUILayout.Toggle("Attach", bAttachProp.boolValue);

                    EditorGUI.BeginDisabledGroup(attach && startOnBone);
                    bAttachToBoneProp.boolValue = EditorGUILayout.Toggle("Attach to Bone", bAttachToBoneProp.boolValue);
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(attach && attachToBone);
                    bStartOnBoneProp.boolValue = EditorGUILayout.Toggle("Start on Bone", bStartOnBoneProp.boolValue);
                    EditorGUI.EndDisabledGroup();

                    bFollowParentRotProp.boolValue = EditorGUILayout.Toggle("Follow Parent Rotation", bFollowParentRotProp.boolValue);

                    EditorGUI.BeginDisabledGroup(useBone == false);
                    nSelectedIndexProp.intValue = EditorGUILayout.Popup("Target Bone", nSelectedIndexProp.intValue, tgt.boneNames);
                    EditorGUI.EndDisabledGroup();

                }
                if (EditorGUI.EndChangeCheck() || serializedObject.hasModifiedProperties)
                {
                    if (attachToBone)
                        bAttachProp.boolValue = true;
                    bStartOnBoneProp.boolValue = attach && attachToBone ? false : startOnBone;
                    OnOptionsEditted();
                }

                EditorGUI.BeginChangeCheck();
                {
                    vAddedPositionProp.vector3Value = EditorGUILayout.Vector3Field(useBone ? "offset Position" : "Start Position", vAddedPositionProp.vector3Value);
                    vAddedRotationProp.vector3Value = EditorGUILayout.Vector3Field(useBone ? "offset Rotation" : "Start Rotation", vAddedRotationProp.vector3Value);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    OnAddedTrasnformValueEditted();
                    return;
                }

                bKeepProp.boolValue = EditorGUILayout.Toggle("Keep", bKeepProp.boolValue);

                EditorGUI.BeginDisabledGroup(bKeepProp.boolValue == false);
                fAliveTimeProp.floatValue = EditorGUILayout.FloatField(new GUIContent("AliveTime", lifeTimeTooltip), fAliveTimeProp.floatValue);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(bAttachProp.boolValue == false || bKeepProp.boolValue == false);
                {//Detach
                    bool detachOnEnd = EditorGUILayout.Toggle("Detach On End", bDetachOnEnd.boolValue);
                    bool manualDetach = EditorGUILayout.Toggle("Manual Detach", bManualDetach.boolValue);
                    EditorGUI.BeginDisabledGroup(bManualDetach.boolValue == false);
                    EditorGUILayout.BeginHorizontal();
                    fbDetachTime.floatValue = EditorGUILayout.FloatField("Detach Time", fbDetachTime.floatValue);
                    if (GUILayout.Button("Current Time", GUILayout.Width(120)))
                    {
                        fbDetachTime.floatValue = tgt.currentTime;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();

                    if (detachOnEnd != bDetachOnEnd.boolValue)
                    {
                        bManualDetach.boolValue = !detachOnEnd;
                        bDetachOnEnd.boolValue = detachOnEnd;
                    }
                    else if (manualDetach != bManualDetach.boolValue)
                    {
                        bDetachOnEnd.boolValue = !manualDetach;
                        bManualDetach.boolValue = manualDetach;
                    }
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(bKeepProp.boolValue == false || hasLoopPS == false);
                DeavtiveType deavtiveType = bDeactiveLoopProp.boolValue ? DeavtiveType.LoopValue : DeavtiveType.SetActive;
                deavtiveType = (DeavtiveType)EditorGUILayout.EnumPopup(new GUIContent("Deactive Type", deactiveTypeToolTip), deavtiveType);
                bDeactiveLoopProp.boolValue = deavtiveType == DeavtiveType.LoopValue;
                EditorGUI.EndDisabledGroup();

                bIgnoreYProp.boolValue = EditorGUILayout.Toggle("Ignore Y", bIgnoreYProp.boolValue);

                if (serializedObject.hasModifiedProperties)
                {
                    if (bAttachProp.boolValue == false || bKeepProp.boolValue == false)
                    {
                        bDetachOnEnd.boolValue = false;
                    }


                    serializedObject.ApplyModifiedProperties();
                    OnModified();
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void OnOptionsEditted()
        {
            if (!attach && attachToBone)
                bAttachToBoneProp.boolValue = false;

            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(tgt, undoName + "attachValue");
            tgt.Inspector_TransformOptionsEditted();
            OnModified();
        }

        private void OnAddedTrasnformValueEditted()
        {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(tgt, undoName + "addedTr");
            tgt.Inspector_TransformValuesEditted();
            OnModified();
            EditorUtility.SetDirty(tgt);
        }
    }
}