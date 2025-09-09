using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    using UnityEditor;
    using AniEventTool.Editor;

#if NO_USE
    [CustomPropertyDrawer(typeof(RangeInfo))]
    public class RangeInfoDrawer : PropertyDrawer 
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //base.OnGUI(position, property, label);

            RangeInfo info = (RangeInfo)attribute;

            SerializedProperty rangeTypeProp = property.FindPropertyRelative("rangeType");
            SerializedProperty centerProp = property.FindPropertyRelative("center");
            SerializedProperty rotationProp = property.FindPropertyRelative("rotation");
            SerializedProperty sizeProp = property.FindPropertyRelative("size");
            SerializedProperty radiusProp = property.FindPropertyRelative("radius");

            EditorGUILayout.PropertyField(rangeTypeProp, new GUIContent("Shape"));

            switch ((eRangeType)rangeTypeProp.enumValueFlag) 
            {
                case eRangeType.Sphere: 
                    {
                        EditorGUILayout.PropertyField(centerProp);
                        EditorGUILayout.PropertyField(radiusProp);
                        rotationProp.vector3Value = Vector3.zero;
                        sizeProp.vector3Value = Vector3.zero;
                    }
                    break;
                case eRangeType.Box:
                    {
                        EditorGUILayout.PropertyField(centerProp);
                        float angle = rotationProp.vector3Value.y;
                        angle = EditorGUILayout.Slider("Angle", angle, 0f, 360f);
                        rotationProp.vector3Value = Vector3.up * angle;
                    }
                    break;
                case eRangeType.Capsule:
                    {
                    }
                    break;
                case eRangeType.Ray:
                    {
                    }
                    break;
            }
        }
    }
#endif
}
