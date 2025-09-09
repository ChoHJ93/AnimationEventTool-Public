using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.SimpleActiveTool
{
    public class EffectActiveController : MonoBehaviour
    {
        [System.Serializable]
        public class ActiveData
        {
            public GameObject obj;
            public bool initialActive;
        }

        Dictionary<string, ActiveData> childrenObjects = new Dictionary<string, ActiveData>();

        private void Awake()
        {
            //register children objects to dictionary, key is object's name, ingnore same key
            Transform[] children = transform.GetAllChildrenByDepth().ToArray();
            foreach (var item in children)
            {
                if (item == transform)
                    continue;

                if (childrenObjects.ContainsKey(item.name) == false)
                {
                    childrenObjects.Add(item.name, new ActiveData() { obj = item.gameObject, initialActive = item.gameObject.activeSelf });
                }
            }
        }
        private void OnDisable()
        {
            foreach (var item in childrenObjects.Values)
            {
                item.obj.SetActive(item.initialActive);
            }
        }

        public void SetActive(string objName)
        {
            if (childrenObjects.ContainsKey(objName))
            {
                childrenObjects[objName].obj.SetActive(true);
            }
        }
        public void SetDeactive(string objName)
        {
            if (childrenObjects.ContainsKey(objName))
            {
                childrenObjects[objName].obj.SetActive(false);
            }
        }

#if UNITY_EDITOR
        [System.Serializable]
        public class EventData
        {
            public string name;
            public int ID;
            public float time;
            public GameObject obj;
            public ParticleSystem ps;
            public bool active;

            public bool IsValidate => ID != -1 && obj != null;
            public void SetID()
            {
                ID = obj == null ? -1 : (name + "_" + obj.name + "_" + active.ToString() + (Mathf.Round(time * 100) / 100).ToString()).GetHashCode();
            }
            public AnimationEvent ToAnimationEvent()
            {
                AnimationEvent evt = new AnimationEvent();
                evt.time = time;
                evt.functionName = active ? "SetActive" : "SetDeactive";
                evt.stringParameter = obj.name;
                //evt.objectReferenceParameter = obj;
                return evt;
            }
        }

        [SerializeField] List<EventData> eventList = new List<EventData>();
        [SerializeField] List<ActiveData> m_Editor_CachedObjetList = new List<ActiveData>();
        public List<EventData> EventList => eventList;

        public bool IsValidate { get { return eventList.IsNullOrEmpty() == false && eventList.Exists(data => data.IsValidate); } }

        public void Editor_Init()
        {
            m_Editor_CachedObjetList.Clear();
            Transform[] childrens = transform.GetAllChildrenByDepth().ToArray();
            foreach (var item in childrens)
            {
                if (item == transform)
                    continue;

                if (m_Editor_CachedObjetList.Exists(data => data.obj == item.gameObject) == false)
                {
                    m_Editor_CachedObjetList.Add(new ActiveData() { obj = item.gameObject, initialActive = item.gameObject.activeSelf });
                }
            }
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.randomSeed = 0;
            }
        }
        public void InitActiveValuesOfChildren() 
        {
            foreach (var item in m_Editor_CachedObjetList)
            {
                item.obj.SetActive(item.initialActive);
            }
        }
        public void SortEventList()
        {
            eventList.Sort((x, y) =>
            {
                int result = x.name.CompareTo(y.name);
                if (result == 0)
                    result = x.time.CompareTo(y.time);
                return result;
            });
        }
        public bool IsChildObject(GameObject obj)
        {
            return m_Editor_CachedObjetList.Exists(data => data.obj == obj);
        }
        public bool IsChildObject(string objName)
        {
            return m_Editor_CachedObjetList.Exists(x => x.obj.name == objName);
        }
        public void AddEventData(AnimationClip clip, float time, GameObject obj, bool value)
        {
            EventData eventData = new EventData()
            {
                name = clip.name,
                ID = -1,
                time = time,
                obj = obj
            };
            AddEventDataToList(eventData);

            AnimationEvent evt = new AnimationEvent();
            evt.time = time;
            evt.functionName = value ? "SetActive" : "SetDeactive";
            evt.stringParameter = "test";
            evt.objectReferenceParameter = obj;
        }
        public void RemoveEventData(EventData data)
        {
            if (data == null)
                return;

            int idx = eventList.FindIndex(x => x.ID == data.ID);
            if (idx >= 0)
            {
                eventList.RemoveAt(idx);
            }
        }
        private void AddEventDataToList(EventData eventData)
        {
            eventList.Add(eventData);
            SortEventList();
        }
        public void SaveToOriginalPrefab()
        {
            gameObject.hideFlags = HideFlags.None;
            eventList.RemoveAll(x => x.IsValidate == false);

            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                GameObject prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                if (prefab != null)
                {
                    UnityEditor.PrefabUtility.ApplyPrefabInstance(gameObject, UnityEditor.InteractionMode.UserAction);
                }
            }
            gameObject.hideFlags = HideFlags.DontSave;
        }
#endif
    }
}

#if UNITY_EDITOR
//custom inspector of EffectActiveController
namespace AniEventTool.SimpleActiveTool.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(EffectActiveController.EventData))]
    public class EventDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUILayout.BeginHorizontal();

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var gameObjectValueRect = new Rect(position.x, position.y, position.width - 16, position.height);
            var activeValueRect = new Rect(position.x + gameObjectValueRect.width + 3, position.y, 13, position.height);

            EditorGUI.PropertyField(gameObjectValueRect, property.FindPropertyRelative("obj"), GUIContent.none);
            EditorGUI.PropertyField(activeValueRect, property.FindPropertyRelative("active"), GUIContent.none);

            EditorGUI.indentLevel = indent;

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndProperty();
        }
    }

    //add property drawer for EffectActiveController.ActiveData
    [CustomPropertyDrawer(typeof(EffectActiveController.ActiveData))]
    public class ActiveDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUILayout.BeginHorizontal();

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var gameObjectValueRect = new Rect(position.x, position.y, position.width - 16, position.height);
            var activeValueRect = new Rect(position.x + gameObjectValueRect.width + 3, position.y, 13, position.height);

            EditorGUI.PropertyField(gameObjectValueRect, property.FindPropertyRelative("obj"), GUIContent.none);
            EditorGUI.PropertyField(activeValueRect, property.FindPropertyRelative("initialActive"), GUIContent.none);

            EditorGUI.indentLevel = indent;

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndProperty();
        }
    }


    [CustomEditor(typeof(EffectActiveController))]
    public class EffectActiveControllerInspector : Editor
    {
        private EffectActiveController m_Target;

        private void OnEnable()
        {
            m_Target = target as EffectActiveController;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            base.OnInspectorGUI();
            EditorGUI.EndDisabledGroup();
        }


    }
}
#endif