using UnityEngine;
using UnityEditor;

namespace GizmoTool
{
    using static GizmoTool.CustomGizmos;
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CustomGizmos))]
    public class CustomGizmos_Inspector : Editor
    {
        static CustomGizmos_Inspector m_Instance;


        SerializedProperty m_ArrowGizmoInfoList;
        SerializedProperty arraySizeProp;
        CustomGizmos myTarget;


        GUIContent m_iconLocalGlobal = null;
        GUIContent m_iconLocal = null;
        GUIContent m_iconGlobal = null;
        private void OnEnable()
        {
            m_Instance = this;
            m_ArrowGizmoInfoList = serializedObject.FindProperty("m_ArrowGizmoInfoList");
            myTarget = (CustomGizmos)target;

            m_iconLocal = EditorGUIUtility.IconContent("d_ToolHandleLocal@2x");
            m_iconLocal.tooltip = "Current is local direction, click for change to use global direction";

            m_iconGlobal = EditorGUIUtility.IconContent("d_ToolHandleGlobal@2x");
            m_iconGlobal.tooltip = "Current is global direction, click for change to use local direction";

        }

        private void OnDestroy()
        {
            m_Instance = null;
        }

        public override void OnInspectorGUI()
        {

            CustomGizmos myTarget = (CustomGizmos)target;
            EditorGUILayout.BeginHorizontal();
            m_ArrowGizmoInfoList.isExpanded = EditorGUILayout.Foldout(m_ArrowGizmoInfoList.isExpanded, "Arrow Gizmo List");

            arraySizeProp = m_ArrowGizmoInfoList.FindPropertyRelative("Array.size");
            EditorGUILayout.PropertyField(arraySizeProp, GUIContent.none, GUILayout.MaxWidth(48));
            if (arraySizeProp.intValue == 0)
                m_ArrowGizmoInfoList.isExpanded = false;

            EditorGUILayout.EndHorizontal();


            if (m_ArrowGizmoInfoList.isExpanded)
            {
                DrawListInfo();
            }

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+", GUILayout.MaxWidth(30f)))
                {
                    m_ArrowGizmoInfoList.InsertArrayElementAtIndex(m_ArrowGizmoInfoList.arraySize);
                    SerializedProperty newElement = m_ArrowGizmoInfoList.GetArrayElementAtIndex(m_ArrowGizmoInfoList.arraySize - 1);
                    newElement.FindPropertyRelative("color").colorValue = Color.white;
                    newElement.FindPropertyRelative("m_fLength").floatValue = 1f;
                    newElement.FindPropertyRelative("m_size").floatValue = 1f;
                    newElement.FindPropertyRelative("m_vSimpleDirection").enumValueIndex = 1;
                    newElement.FindPropertyRelative("m_style").enumValueIndex = 0;
                    newElement.FindPropertyRelative("m_vCustomDir").vector3Value = Vector3.forward;
                }

                if (GUILayout.Button("-", GUILayout.MaxWidth(30f)))
                    m_ArrowGizmoInfoList.DeleteArrayElementAtIndex(m_ArrowGizmoInfoList.arraySize - 1);
            }
            EditorGUILayout.EndHorizontal();



            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(myTarget);
            }
        }

        void DrawListInfo()
        {
            if (myTarget == null || myTarget.m_ArrowGizmoInfoList == null || arraySizeProp.intValue <= 0)
                return;
            CustomEditorUtil.DrawUILine(Color.gray);

            for (int i = 0; i < myTarget.m_ArrowGizmoInfoList.Count; i++)
            {

                CustomGizmos.ArrowGizmoInfo info = myTarget.m_ArrowGizmoInfoList[i];

                #region CompareObjectField
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    info.bIsShowTargetObjecField = EditorGUILayout.Toggle("Use Other Object Direction", info.bIsShowTargetObjecField);

                    if (info.bIsShowTargetObjecField)
                    {
                        info.targetObject = (GameObject)EditorGUILayout.ObjectField(info.targetObject, typeof(GameObject), true);
                    }
                    if (EditorGUI.EndChangeCheck()) 
                    {
                        info.InitRootTrType(myTarget.transform);
                    }
                }
                EditorGUILayout.EndHorizontal();
                #endregion

                #region DirectionField
                info.m_bUseCustomDirection = EditorGUILayout.Toggle("Use Custom Direction", info.m_bUseCustomDirection);

                EditorGUILayout.BeginHorizontal();
                {
                    float buttonWidth = 27;
                    m_iconLocalGlobal = info.m_bLocalDirection ? m_iconLocal : m_iconGlobal;
                    bool clickLocalGlobalToggle = GUILayout.Button(m_iconLocalGlobal, GUILayout.Width(buttonWidth));
                    if (clickLocalGlobalToggle)
                    {
                        info.m_bLocalDirection = !info.m_bLocalDirection;
                    }
                    EditorGUILayout.LabelField("Direction", GUILayout.MaxWidth(60));

                    if (info.m_bUseCustomDirection)
                    {
                        info.m_vCustomDir = EditorGUILayout.Vector3Field("", info.m_vCustomDir, GUILayout.MaxWidth(200f));
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();
                        info.m_vSimpleDirection = (ArrowDirection)EditorGUILayout.EnumPopup("", info.m_vSimpleDirection, GUILayout.MaxWidth(80));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Transform targetObjTr = (serializedObject.targetObject as MonoBehaviour).transform;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                #endregion

                #region OptionFields
                EditorGUILayout.BeginHorizontal();

                GUILayoutOption[] opt_Wide = { GUILayout.MinWidth(40f), GUILayout.MaxWidth(100f) };
                GUILayoutOption[] opt_Middle = { GUILayout.MinWidth(30f), GUILayout.MaxWidth(80f) };
                GUILayoutOption[] opt_Narraw = { GUILayout.MinWidth(20f), GUILayout.MaxWidth(40f) };
                GUILayoutOption[] opt_Btn = { GUILayout.MinWidth(5f), GUILayout.MaxWidth(15f) };


                EditorGUILayout.EndHorizontal();
                SerializedProperty item = m_ArrowGizmoInfoList.GetArrayElementAtIndex(i);
                SerializedProperty colorProperty = item.FindPropertyRelative("color");
                SerializedProperty lengthProperty = item.FindPropertyRelative("m_fLength");
                SerializedProperty sizeProperty = item.FindPropertyRelative("m_size");
                SerializedProperty styleProperty = item.FindPropertyRelative("m_style");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(colorProperty, GUIContent.none, opt_Middle);
                EditorGUILayout.PropertyField(styleProperty, GUIContent.none, opt_Wide);
                CustomEditorUtil.DynamicLabelField("Length");
                EditorGUILayout.PropertyField(lengthProperty, GUIContent.none, opt_Narraw);
                CustomEditorUtil.DynamicLabelField("Size");
                EditorGUILayout.PropertyField(sizeProperty, GUIContent.none, opt_Narraw);

                EditorGUILayout.EndHorizontal();

                #endregion

                CustomEditorUtil.DrawUILine(Color.gray);
            }

        }
    }


}
