using UnityEngine;
using UnityEditor;

namespace GizmoTool
{
    public static class CustomEditorUtil
    {
        public static readonly float EPSILON = 0.0001f;
        private const float widthPerChar = 7f;
        private const float widthPerChar_Kor = 13;
        private const float additionalWidth_Bold = 5;

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
    }
}
