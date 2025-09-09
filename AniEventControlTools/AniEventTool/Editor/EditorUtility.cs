using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
using UnityEngine.Events;
using UnityEditor;
using Object = UnityEngine.Object;
using System.Reflection;

    internal struct GUIViewportScope : IDisposable
    {
        bool m_open;
        public GUIViewportScope(Rect position)
        {
            m_open = false;
            if (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout)
            {
                GUI.BeginClip(position, -position.min, Vector2.zero, false);
                m_open = true;
            }
        }

        public void Dispose()
        {
            CloseScope();
        }

        void CloseScope()
        {
            if (m_open)
            {
                GUI.EndClip();
                m_open = false;
            }
        }
    }

    internal struct GUIColorOverride : IDisposable
    {
        readonly Color m_OldColor;

        public GUIColorOverride(Color newColor)
        {
            m_OldColor = GUI.color;
            GUI.color = newColor;
        }

        public void Dispose()
        {
            GUI.color = m_OldColor;
        }
    }

    internal static class DisplayDialog
    {
        internal static void ShowDisplayDialog(string message, UnityAction onClickOK, string title = "Ani Event Tool", string ok = "OK")
        {
            bool userClickedOk = EditorUtility.DisplayDialog(title, message, ok);

            if (userClickedOk)
            {
                if (onClickOK != null)
                    onClickOK.Invoke();
            }
        }

        internal static void ShowDisplayDialog(string message, UnityAction onClickOK, UnityAction onClickCancel, string title = "Ani Event Tool", string ok = "OK", string cancel = "Cancel")
        {
            bool userClickedOk = EditorUtility.DisplayDialog(title, message, ok, cancel);

            if (userClickedOk)
            {
                if (onClickOK != null)
                    onClickOK.Invoke();
            }
            else
            {
                if (onClickCancel != null)
                    onClickCancel.Invoke();
            }
        }

        static IEnumerator DisplayProgressBarRoutine()
        {
            // �ε� �˾� ǥ��
            EditorUtility.DisplayProgressBar("Saving", "Saving data to JSON...", 0.5f);

            yield return new WaitForSeconds(1f);

            // �ε� �˾� �ݱ�
            EditorUtility.ClearProgressBar();

            // ���� �����ͺ��̽� ����
            AssetDatabase.Refresh();
        }
    }

    #region EditorGUI
    public static class CustomEditorUtil
    {
        public static readonly float EPSILON = 0.0001f;
        private const float widthPerChar = 7f;
        private const float widthPerChar_Kor = 13;
        private const float additionalWidth_Bold = 5;

        public static float GetWidthPerChar(bool isKorean = false)
        {
            return isKorean ? widthPerChar_Kor : widthPerChar;
        }

        public static void RegisterUndo(string name, params Object[] objects)
        {
            if (objects != null && objects.Length > 0)
            {
                UnityEditor.Undo.RecordObjects(objects, name);

                foreach (Object obj in objects)
                {
                    if (obj == null) continue;
                    EditorUtility.SetDirty(obj);
                }
            }
        }

        public static void DrawUILine(Color color, int thickness = 1, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
        public static void DynamicLabelField(string fieldName, bool isKorean = false)
        {
            float charWidth = isKorean ? widthPerChar_Kor : widthPerChar;
            EditorGUILayout.LabelField(fieldName, GUILayout.MinWidth(4 * charWidth), GUILayout.MaxWidth(fieldName.Length * charWidth));
        }
        public static void DynamicLabelField(string fieldName, GUIStyle guiStyle, bool isKorean = false)
        {
            float charWidth = isKorean ? widthPerChar_Kor : widthPerChar;
            charWidth += guiStyle.fontStyle == FontStyle.Bold ? additionalWidth_Bold : 0;
            EditorGUILayout.LabelField(fieldName, guiStyle, GUILayout.MinWidth(4 * charWidth), GUILayout.MaxWidth(fieldName.Length * charWidth));
        }
        public static void DynamicLabelField(string fieldName, int customWidth)
        {
            EditorGUILayout.LabelField(fieldName, GUILayout.MinWidth(customWidth), GUILayout.MaxWidth(customWidth));
        }

        public static void AutoWrapLabelField(string label, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (style == null)
                style = new GUIStyle(GUI.skin.label);

            style.wordWrap = true;

            EditorGUILayout.LabelField(label, style, options);
        }

        public static float SetRectDraggable(Rect labelRect, ref float value)
        {
            EditorGUIOverride.NumberFieldValue value2 = new EditorGUIOverride.NumberFieldValue(value);
            int controlID = GUIUtility.GetControlID(EditorGUIOverride.FloatFieldHash, FocusType.Keyboard, labelRect);
            float sensitivity = ((Event.current.GetTypeForControl(controlID) == EventType.MouseDown) ? ((float)CustomMathUtils.CalculateFloatDragSensitivity(EditorGUIOverride.DragStartValue)) : 0f);
            EditorGUIOverride.DragNumberField(labelRect, controlID, ref value2, sensitivity);
            return CustomMathUtils.ClampToFloat(value2.doubleVal);
        }

        static bool HitTest(Rect rect, Vector2 point, int offset)
        {
            return point.x >= rect.xMin - (float)offset && point.x < rect.xMax + (float)offset && point.y >= rect.yMin - (float)offset && point.y < rect.yMax + (float)offset;
        }
        static bool HitTest(Rect rect, Vector2 point, bool isDirectManipulationDevice)
        {
            int offset = 0;
            return HitTest(rect, point, offset);
        }
        public static bool HitTest(Rect rect, Event evt)
        {
            return HitTest(rect, evt.mousePosition, evt.IsDirectManipulationDevice());
        }


        //Context Menu
        public struct ContextMenuItem
        {
            public string menuName;
            public int priority;
            public bool isChecked;
            public bool isEnabled;
            public GenericMenu.MenuFunction callback;
        }

        public static ContextMenuItem CreateContextMenuItem(string name, int priority, GenericMenu.MenuFunction callback, bool enabled = true)
        {
            ContextMenuItem menuItem = new ContextMenuItem();
            menuItem.menuName = name;
            menuItem.priority = priority;
            menuItem.isChecked = false;
            menuItem.callback = callback;
            menuItem.isEnabled = enabled;

            return menuItem;
        }

        public static void ShowContextMenu(params ContextMenuItem[] items)
        {
            GenericMenu menu = new GenericMenu();

            foreach (var item in items)
            {
                if (item.isEnabled)
                    menu.AddItem(new GUIContent(item.menuName), item.isChecked, item.callback);
                else
                    menu.AddDisabledItem(new GUIContent(item.menuName), item.isChecked);
            }

            menu.ShowAsContext();
        }
    }
    public static class EditorGUIOverride
    {
        private enum DragCandidateState
        {
            NotDragging,
            InitiatedDragging,
            CurrentlyDragging
        }

        public struct NumberFieldValue
        {
            public bool isDouble;
            public double doubleVal;
            public long longVal;
            //public ExpressionEvaluator.Expression expression;
            public bool success;

            //public bool hasResult => success || expression != null;

            public NumberFieldValue(double v)
            {
                isDouble = true;
                doubleVal = v;
                longVal = 0L;
                //expression = null;
                success = false;
            }

            public NumberFieldValue(long v)
            {
                isDouble = false;
                doubleVal = 0.0;
                longVal = v;
                //expression = null;
                success = false;
            }
        }


        static string s_AllowedCharactersForFloat = "inftynaeINFTYNAE0123456789.,-*/+%^()cosqrludxvRL=pP#";
        static string s_AllowedCharactersForInt = "0123456789-*/+%^()cosintaqrtelfundxvRL,=pPI#";

        static DragCandidateState s_DragCandidateState = DragCandidateState.NotDragging;

        static int s_FloatFieldHash = "EditorTextField".GetHashCode();
        static double s_DragStartValue;
        static long s_DragStartIntValue;
        static Vector2 s_DragStartPos;
        static double s_DragSensitivity;

        public static int FloatFieldHash => s_FloatFieldHash;
        public static double DragStartValue => s_DragStartValue;


        public static void DragNumberField(Rect dragHotZone, int id, ref NumberFieldValue value, double dragSensitivity)
        {
            string allowedletters = (value.isDouble ? s_AllowedCharactersForFloat : s_AllowedCharactersForInt);
            if (GUI.enabled)
            {
                DragNumberValue(dragHotZone, id, ref value, dragSensitivity);
            }
        }

        private static void DragNumberValue(Rect dragHotZone, int id, bool isDouble, ref double doubleVal, ref long longVal, double dragSensitivity)
        {
            NumberFieldValue value = default(NumberFieldValue);
            value.isDouble = isDouble;
            value.doubleVal = doubleVal;
            value.longVal = longVal;
            DragNumberValue(dragHotZone, id, ref value, dragSensitivity);
            doubleVal = value.doubleVal;
            longVal = value.longVal;
        }

        private static void DragNumberValue(Rect dragHotZone, int id, ref NumberFieldValue value, double dragSensitivity)
        {
            Event current = Event.current;
            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (CustomEditorUtil.HitTest(dragHotZone, current) && current.button == 0)
                    {
                        EditorGUIUtility.editingTextField = false;
                        GUIUtility.hotControl = id;
                        EndEditing_Text();
                        current.Use();
                        GUIUtility.keyboardControl = id;
                        s_DragCandidateState = DragCandidateState.InitiatedDragging;
                        s_DragStartValue = value.doubleVal;
                        s_DragStartIntValue = value.longVal;
                        s_DragStartPos = current.mousePosition;
                        s_DragSensitivity = dragSensitivity;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && s_DragCandidateState != 0)
                    {
                        GUIUtility.hotControl = 0;
                        s_DragCandidateState = DragCandidateState.NotDragging;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }

                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                    {
                        break;
                    }

                    switch (s_DragCandidateState)
                    {
                        case DragCandidateState.InitiatedDragging:
                            if ((Event.current.mousePosition - s_DragStartPos).sqrMagnitude > 16f)
                            {
                                s_DragCandidateState = DragCandidateState.CurrentlyDragging;
                                GUIUtility.keyboardControl = id;
                            }

                            current.Use();
                            break;
                        case DragCandidateState.CurrentlyDragging:
                            if (value.isDouble)
                            {
                                value.doubleVal += (double)HandleUtility.niceMouseDelta * s_DragSensitivity;
                                value.doubleVal = CustomMathUtils.RoundBasedOnMinimumDifference(value.doubleVal, s_DragSensitivity);
                            }
                            else
                            {
                                value.longVal += (long)System.Math.Round((double)HandleUtility.niceMouseDelta * s_DragSensitivity);
                            }

                            value.success = true;
                            GUI.changed = true;
                            current.Use();
                            break;
                    }

                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id && current.keyCode == KeyCode.Escape && s_DragCandidateState != 0)
                    {
                        value.doubleVal = s_DragStartValue;
                        value.longVal = s_DragStartIntValue;
                        value.success = true;
                        GUI.changed = true;
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }

                    break;
                case EventType.Repaint:
                    EditorGUIUtility.AddCursorRect(dragHotZone, MouseCursor.SlideArrow);
                    break;
                case EventType.MouseMove:
                case EventType.KeyUp:
                case EventType.ScrollWheel:
                    break;
            }
        }

        private static void EndEditing_Text()
        {
            System.Type type = typeof(EditorGUI);
            FieldInfo activeEditor_FieldInfo = type.GetField("activeEditor", BindingFlags.Static | BindingFlags.NonPublic);
            object activeEditor = activeEditor_FieldInfo.GetValue(null);
            if (activeEditor != null)
            {
                MethodInfo EndEditing_MethodInfo = activeEditor.GetType().GetMethod("EndEditing", BindingFlags.Public | BindingFlags.Instance);
                EndEditing_MethodInfo.Invoke(activeEditor_FieldInfo.GetValue(null), new object[] { });
            }
        }
    }
    public static class CustomGUIStyles
    {
        public static Color colorTimelineBackground = new Color(0.2f, 0.2f, 0.2f, 1.0f);

        //Unity Default Resources
        public static readonly GUIContent gotoBeginingContent = L10n.IconContent("Animation.FirstKey", "Go to the beginning of the Animation");
        public static readonly GUIContent gotoEndContent = L10n.IconContent("Animation.LastKey", "Go to the end of the Animation");
        public static readonly GUIContent nextFrameContent = L10n.IconContent("Animation.NextKey", "Go to the next frame");
        public static readonly GUIContent previousFrameContent = L10n.IconContent("Animation.PrevKey", "Go to the previous frame");
        public static readonly GUIContent newContent = L10n.IconContent("CreateAddNew", "Add new event.");
        public static readonly GUIContent searchIcon = EditorGUIUtility.IconContent("Search On Icon");
        public static readonly GUIContent refreshIcon = EditorGUIUtility.IconContent("Refresh@2x");
        public static readonly GUIContent optionsCogIcon = L10n.IconContent("_Popup", "Options");
        public static readonly GUIContent playIcon = L10n.IconContent("Animation.Play", "Play");
        public static readonly GUIContent pauseIcon = EditorGUIUtility.IconContent("d_PauseButton On@2x");
        public static readonly GUIContent stopIcon = EditorGUIUtility.IconContent("d_PreMatQuad@2x");
        public static readonly GUIContent loopIcon = EditorGUIUtility.IconContent("d_preAudioLoopOff@2x");
        public static readonly GUIContent audioIcon = EditorGUIUtility.IconContent("d_Profiler.Audio@2x");
        public static readonly GUIContent eventKeyIcon_Gray = EditorGUIUtility.IconContent("sv_icon_dot10_pix16_gizmo");
        public static readonly GUIContent eventKeyIcon_Yellow = EditorGUIUtility.IconContent("sv_icon_dot12_pix16_gizmo");
        public static readonly GUIContent eventKeyIcon_Red = EditorGUIUtility.IconContent("sv_icon_dot14_pix16_gizmo");
        public static readonly GUIContent eventKeyIcon_Blue = EditorGUIUtility.IconContent("sv_icon_dot9_pix16_gizmo");
        public static readonly GUIContent eventKeyIcon_Purple = EditorGUIUtility.IconContent("sv_icon_dot15_pix16_gizmo");
        public static readonly GUIContent eventKeyIcon_Yellow_Circle = EditorGUIUtility.IconContent("sv_icon_dot4_pix16_gizmo");
        public static readonly GUIContent eventKeyIcon_Red_Circle = EditorGUIUtility.IconContent("sv_icon_dot6_pix16_gizmo");
        public static readonly GUIContent eventMarkIcon = EditorGUIUtility.IconContent("AnimationWindowEvent Icon");//SignalAsset Icon	
        public static readonly GUIContent eventKeyIcon_PlusCircle = EditorGUIUtility.IconContent("PrefabOverlayAdded Icon");
        public static readonly GUIContent eventKeyIcon_MinusCircle = EditorGUIUtility.IconContent("PrefabOverlayRemoved Icon");
        public static readonly GUIContent eventKeyIcon_NoneCircle = EditorGUIUtility.IconContent("PrefabOverlayModified Icon");
        public static readonly GUIContent eventKeyIcon_EnableInput = EditorGUIUtility.IconContent("d_scenepicking_pickable_hover@2x");
        public static readonly GUIContent eventKeyIcon_DisableInput = EditorGUIUtility.IconContent("d_scenepicking_notpickable_hover@2x");
        public static readonly GUIContent eventKeyIcon_Trace = EditorGUIUtility.IconContent("d_AimConstraint Icon");

        //TrackGUI
        public static Color colorContentBackground = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        public static Color colorSelectedContentBackground = new Color(0.7f, 0.7f, 0.7f, 1.0f);
        public static Color defaultEventBackground = new Color(0.254902f, 0.254902f, 0.254902f, 1f);
        public static Color hoverEventBackground = new Color(0.6f, 0.6f, 0.6f, 1f);
        public static Color colorEventListBackground = new Color(0.16f, 0.16f, 0.16f, 1.0f);

        public static GUIStyle eventSwatchStyle = GetGUIStyle("Icon-TrackHeaderSwatch");
        public static GUIStyle eventAddButton = GetGUIStyle("sequenceTrackGroupAddButton");
        public static GUIStyle eventLockButton = GetGUIStyle("trackLockButton");
        public static GUIContent eventEnableState = EditorGUIUtility.IconContent("d_animationvisibilitytoggleon@2x");
        public static GUIContent eventDisableState = EditorGUIUtility.IconContent("d_animationvisibilitytoggleoff@2x");
        public static GUIContent eventAttackNotice = EditorGUIUtility.IconContent("d_AvatarSelector@2x");

        public static GUIStyle clipIn = GetGUIStyle("Icon-ClipIn");
        public static GUIStyle timeCursor = GetGUIStyle("Icon-TimeCursor");
        public static GUIStyle displayBackground = GetGUIStyle("sequenceClip");
        private static GUIStyle GetGUIStyle(string s)
        {
            return EditorStyles.FromUSS(s);
        }

    }
    #endregion // EditorGUI
}