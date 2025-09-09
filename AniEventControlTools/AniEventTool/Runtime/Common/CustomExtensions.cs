using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AniEventTool
{
    public static class CustomExtensions
    {
        #region Primitive Types
        public static string SplitCamelCase(this string str)
        {
            return Regex.Replace(str, "(?<!^)([A-Z])", " $1");
        }
    
        #endregion

        #region List & Dictionary
        public static bool IsNullOrEmpty<T>(this T[] array)
        {
            return array == null || array.Length == 0;
        }
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }
        #endregion

        #region Transform
        public static Transform FindChildAtDepthWithName(this Transform parent, int depth, string name)
        {
            if (depth < 0)
            {
                return null;
            }

            if (depth == 0)
            {
                foreach (Transform child in parent)
                {
                    if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return child;
                    }
                }
                return null;
            }

            foreach (Transform child in parent)
            {
                Transform result = child.FindChildAtDepthWithName(depth - 1, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
        public static Transform[] GetChildrenAtDepth(this Transform parent, int depth)
        {
            List<Transform> result = new List<Transform>();
            AddChildrenAtDepth(parent, depth, 0, result);
            return result.ToArray();
        }
        public static List<Transform> GetAllChildrenByDepth(this Transform parent)
        {
            List<Transform> result = new List<Transform>();
            Queue<Transform> queue = new Queue<Transform>();

            foreach (Transform child in parent)
            {
                queue.Enqueue(child);
            }

            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();
                result.Add(current);

                foreach (Transform child in current)
                {
                    queue.Enqueue(child);
                }
            }

            return result;
        }
        private static void AddChildrenAtDepth(Transform current, int targetDepth, int currentDepth, List<Transform> result)
        {
            if (currentDepth == targetDepth)
            {
                result.Add(current);
                return; // Ÿ�� ���̿� ���������Ƿ� ������ �� Ž������ ����
            }

            foreach (Transform child in current)
            {
                AddChildrenAtDepth(child, targetDepth, currentDepth + 1, result);
            }
        }


        public static bool TryGetComponentInChildren<T>(this Transform tr, bool includeInactive, string childName, out T component) where T : UnityEngine.Object
        {
            component = default;
            if (tr.childCount == 0) return false;

            for (int i = 0; i < tr.childCount; i++)
            {
                if (includeInactive == false && tr.GetChild(i).gameObject.activeInHierarchy == false)
                    continue;

                if (tr.GetChild(i).name.Equals(childName) == false)
                    continue;

                if (tr.GetChild(i).TryGetComponent(out component))
                {
                    return true;
                }
            }

            return false;
        }
        public static T GetComponentAtDepth<T>(this GameObject go, int targetDepth) where T : Component
        {
            return GetComponentAtDepth<T>(go.transform, targetDepth, 0);
        }
        private static T GetComponentAtDepth<T>(Transform trans, int targetDepth, int currentDepth) where T : Component
        {
            if (targetDepth == currentDepth)
            {
                return trans.GetComponent<T>();
            }

            if (targetDepth < currentDepth)
            {
                return null;
            }

            foreach (Transform child in trans)
            {
                T component = GetComponentAtDepth<T>(child, targetDepth, currentDepth + 1);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
        public static List<T> GetComponentsAtDepth<T>(this GameObject go, int targetDepth) where T : Component
        {
            List<T> result = new List<T>();
            GetComponentsAtDepth(go.transform, targetDepth, 0, result);
            return result;
        }
        private static void GetComponentsAtDepth<T>(Transform trans, int targetDepth, int currentDepth, List<T> result) where T : Component
        {
            if (targetDepth == currentDepth)
            {
                T component = trans.GetComponent<T>();
                if (component != null)
                {
                    result.Add(component);
                }
                return;
            }

            if (targetDepth < currentDepth)
            {
                return;
            }

            foreach (Transform child in trans)
            {
                GetComponentsAtDepth(child, targetDepth, currentDepth + 1, result);
            }
        }

        /// <summary>
        /// Dosen't care about transform call this function
        /// </summary>
        /// <param name="tf"></param>
        /// <param name="transforPos"></param>
        /// <param name="transformRotation"></param>
        /// <param name="transformScale"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Vector3 InverseTransformPoint(this Transform tf, Vector3 transforPos, Quaternion transformRotation, Vector3 transformScale, Vector3 pos)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(transforPos, transformRotation, transformScale);
            Matrix4x4 inverse = matrix.inverse;
            return inverse.MultiplyPoint3x4(pos);
        }
        #endregion

        public static bool AreKeysNullOrEmpty<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            return dictionary == null || dictionary.Keys.Count == 0;
        }
        public static bool IsNullOrEmpty<TKey, TValue>(this Dictionary<TKey, TValue>.KeyCollection keyCollection)
        {
            return keyCollection == null || keyCollection.Count == 0;
        }
        public static bool AreValuesNullOrEmpty<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            return dictionary == null || dictionary.Values.Count == 0;
        }

        public static bool IsNullOrEmpty<TKey, TValue>(this Dictionary<TKey, TValue>.ValueCollection valueCollection)
        {
            return valueCollection == null || valueCollection.Count == 0;
        }

        public static bool IsDirectManipulationDevice(this Event e)
        {
            return e.pointerType == PointerType.Pen || e.pointerType == PointerType.Touch;
        }

        public static string GetNameFromPath(string path)
        {
            string[] strs = path.Split('/');
            if (strs.Length > 0)
                return strs[strs.Length - 1];

            return "";
        }

        /// <summary>
        /// ���ڿ����� toRemove�� ��� �ν��Ͻ��� �����մϴ�.
        /// �⺻: ��ҹ��� ����(Ordinal).
        /// </summary>
        public static string RemoveAllOccurrences(this string source, string toRemove, StringComparison comparison = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(toRemove))
                return source;

            int start = 0;
            int found;
            var sb = new StringBuilder(source.Length);

            while ((found = source.IndexOf(toRemove, start, comparison)) >= 0)
            {
                sb.Append(source, start, found - start);
                start = found + toRemove.Length;
            }
            sb.Append(source, start, source.Length - start);

            return sb.Length == source.Length ? source : sb.ToString();
        }

        public static void DeleteExt(ref string str)
        {
            int lastIndex = str.LastIndexOf('.');
            if (lastIndex >= 0)
                str = str.Substring(0, lastIndex);
        }
        #region Reflection
        public static bool TryGetField(this Type type, string location, string name, BindingFlags bindingAttr, out FieldInfo fieldInfo)
        {
            FieldInfo result = type.GetField(name, bindingAttr);
            fieldInfo = result;
            return AssertReflectionResult(result, "FieldInfo", name, location);
        }
        public static bool TryGetMethod(this Type type, string location, string name, BindingFlags bindingAttr, out MethodInfo methodInfo)
        {
            MethodInfo result = type.GetMethod(name, bindingAttr);
            methodInfo = result;
            return AssertReflectionResult(result, "MethodInfo", name, location);
        }

        public static bool TryGetNestedType(this Type type, string location, string name, BindingFlags bindingAttr, out Type nestedTypeInfo)
        {
            Type result = type.GetNestedType(name, bindingAttr);
            nestedTypeInfo = result;
            return AssertReflectionResult(result, "Nested Type", name, location);
        }

        //type.GetConstructor
        //type.GetEvent()
        //type.GetInterface
        //type.GetProperty()

        /// <summary>
        /// AppDomain.CurrentDomain�� ��� ������ ������ �˻�
        /// </summary>
        /// <param name="type"></param>
        /// <param name="location"></param>
        /// <param name="typeName"></param>
        /// <param name="ignoreCase"> ��ҹ��� ����?</param>
        /// <param name="foundType"></param>
        /// <returns></returns>
        public static bool TryGetType(this Type type, string location, string typeName, bool ignoreCase, out Type foundType)
        {
            Type found = null;
            string fullPath = string.IsNullOrEmpty(location) ? typeName : $"{location}.{typeName}";
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {

                type = asm.GetType(fullPath, false, ignoreCase);
                if (found != null)
                    break;
            }

            foundType = found;
            return AssertReflectionResult(found, "Type", typeName, location);
        }

        private static bool AssertReflectionResult(object result, string infoType, string targetName, string targetLocation)
        {
            if (result == null)
            {
                Debug.LogError($"Fail to get {infoType} info \"{targetName}\" from {targetLocation}");
            }

            return result != null;
        }
        #endregion

#if UNITY_EDITOR
        #region Animation
        public static Vector3 GetBonePositionAtTime(this AnimationClip clip, string bonePath, float time)
        {
            EditorCurveBinding xCurveBinding = EditorCurveBinding.FloatCurve(bonePath, typeof(Transform), "m_LocalPosition.x");
            EditorCurveBinding yCurveBinding = EditorCurveBinding.FloatCurve(bonePath, typeof(Transform), "m_LocalPosition.y");
            EditorCurveBinding zCurveBinding = EditorCurveBinding.FloatCurve(bonePath, typeof(Transform), "m_LocalPosition.z");

            AnimationCurve xCurve = AnimationUtility.GetEditorCurve(clip, xCurveBinding);
            AnimationCurve yCurve = AnimationUtility.GetEditorCurve(clip, yCurveBinding);
            AnimationCurve zCurve = AnimationUtility.GetEditorCurve(clip, zCurveBinding);

            return new Vector3(xCurve.Evaluate(time), yCurve.Evaluate(time), zCurve.Evaluate(time));
        }

        public static Quaternion GetBoneRotationEulerAtTime(this AnimationClip clip, string bonePath, float time)
        {
            EditorCurveBinding xCurveBinding = EditorCurveBinding.FloatCurve(bonePath, typeof(Transform), "m_LocalRotation.x");
            EditorCurveBinding yCurveBinding = EditorCurveBinding.FloatCurve(bonePath, typeof(Transform), "m_LocalRotation.y");
            EditorCurveBinding zCurveBinding = EditorCurveBinding.FloatCurve(bonePath, typeof(Transform), "m_LocalRotation.z");
            EditorCurveBinding wCurveBinding = EditorCurveBinding.FloatCurve(bonePath, typeof(Transform), "m_LocalRotation.w");


            AnimationCurve xCurve = AnimationUtility.GetEditorCurve(clip, xCurveBinding);
            AnimationCurve yCurve = AnimationUtility.GetEditorCurve(clip, yCurveBinding);
            AnimationCurve zCurve = AnimationUtility.GetEditorCurve(clip, zCurveBinding);
            AnimationCurve wCurve = AnimationUtility.GetEditorCurve(clip, zCurveBinding);

            return new Quaternion(xCurve.Evaluate(time), yCurve.Evaluate(time), zCurve.Evaluate(time), wCurve.Evaluate(time));

        }
        #endregion

#endif
        #region Component
        public static T GetComponent<T>(this GameObject gameObject, bool addIfNotFound) where T : Component
        {
            T component = gameObject.GetComponent<T>();

            if (addIfNotFound && component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
        #endregion

    }

}