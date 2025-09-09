using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.SimpleActiveTool.Editor
{
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEditor.Animations;
    using AniEventTool.Editor;
    using EventData = AniEventTool.SimpleActiveTool.EffectActiveController.EventData;

    public class EffectActiveToolWindow : EditorWindow
    {
        static readonly string undoKey = "EffectActiveToolWindow-UndoKey";

        private static EffectActiveToolWindow m_Window;
        public static EffectActiveToolWindow instance => m_Window;
        public static bool IsOpen => m_Window != null;
        public bool IsFocused => m_Window == EditorWindow.focusedWindow;

        [MenuItem("Tools/���� ������Ʈ On-Off Tool", false)]
        public static void ShowWindow()
        {
            if (m_Window == null)
            {
                m_Window = GetWindow<EffectActiveToolWindow>(false, "Child Object On/Off", true);
                m_Window.minSize = new Vector2(400, 300);
                m_Window.Show();
                m_Window.Init();
            }
            else
            {
                m_Window.Focus();
            }
        }
        
        [SerializeField] private bool m_isCompiling = false;
        [SerializeField] private WindowState m_WindowState;
        [SerializeField] private GameObject m_OriginPrefab = null;
        private List<EventData> eventDatas
        {
            get
            {
                if (SelectedClip != null && SelectedController != null)
                    return SelectedController.EventList.Where(data => data.name.Equals(SelectedClip.name)).ToList();
                else
                    return null;
            }
        }
        private float m_beforeTime = 0.0f;
        private float m_deltaTime = 0;
        private List<ParticleSystem> psList = new List<ParticleSystem>();

        private WindowState WindowState => m_WindowState;
        private EffectActiveController SelectedController => WindowState?.SelectedController;
        private AnimationClip SelectedClip => WindowState?.SelectedClip;
        private bool IsReadyToPlayAni => m_isCompiling == false && SelectedClip != null && SelectedController != null;

        private void Init()
        {
            if (m_Window == null)
            {
                m_Window = this;
            }

            if (m_WindowState == null)
                m_WindowState = new WindowState();

            WindowState.Init();
            if (m_OriginPrefab != null && SelectedController == null)
                LoadSelectedObject(m_OriginPrefab);
        }
        private void Clear()
        {
            if (m_WindowState != null)
                m_WindowState.Clear();
            else
                m_WindowState = new WindowState();
        }

        #region EventCallbacks
        private void OnEnable()
        {
            CompilationPipeline.compilationStarted += OnCompileStart;
            CompilationPipeline.compilationFinished += OnCompileFinished;

            Init();
        }
        private void OnDisable()
        {
            //unbind events
            CompilationPipeline.compilationStarted -= OnCompileStart;
            CompilationPipeline.compilationFinished -= OnCompileFinished;
        }
        private void OnDestroy()
        {
            m_Window = null;
            EditorCache.Clear();
            Clear();
            WindowState.OnDestroy();
        }

        private void OnCompileStart(object obj)
        {
            m_isCompiling = true;

            //
        }
        private void OnCompileFinished(object obj)
        {
            m_isCompiling = false;
            Repaint();
        }
        #endregion

        #region DrawGUI
        private void OnGUI()
        {
            if (m_Window != null)
            {
                DrawWindowState();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                {
                    DrawButtons();
                    DrawEventList();
                }
                EditorGUILayout.EndVertical();
            }
        }
        private void DrawWindowState()
        {
            #region Draw_GameObjectField
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("Selected Object", GUILayout.MaxWidth(100));
                GameObject prefabObj = (GameObject)EditorGUILayout.ObjectField("", m_OriginPrefab ?? null, typeof(GameObject), false, GUILayout.MinWidth(120));
                if (EditorGUI.EndChangeCheck())
                {
                    OnSelectedObjectChange(prefabObj);
                }
            }
            EditorGUILayout.EndHorizontal();
            #endregion

            #region Draw_ClipField
            //if m_SelectedClip is not null, Draw ClipField in disabled mode
            if (WindowState.AnimationClipNames.IsNullOrEmpty() == false)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Selected Clip", GUILayout.MaxWidth(100));
                EditorGUI.BeginChangeCheck();
                WindowState.SelectedClipIndex = EditorGUILayout.Popup("", WindowState.SelectedClipIndex, WindowState.AnimationClipNames.ToArray(), GUILayout.MinWidth(150));
                if (EditorGUI.EndChangeCheck())
                {
                    OnClipChange();
                }
                //draw m_SelectedClip GUI in disabled mode
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("", SelectedClip, typeof(AnimationClip), false, GUILayout.MinWidth(80));
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            #endregion

            #region Draw_TimeSlider
            if (SelectedClip != null)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    GUIContent buttonIcon = WindowState.playing ? CustomGUIStyles.pauseIcon : CustomGUIStyles.playIcon;
                    bool isPlaying = GUILayout.Toggle(WindowState.playing, buttonIcon, GUI.skin.button, GUILayout.MaxWidth(23));
                    if (EditorGUI.EndChangeCheck())
                    {
                        WindowState.playing = isPlaying;
                    }
                    if (GUILayout.Button(CustomGUIStyles.stopIcon, GUILayout.MaxWidth(23), GUILayout.MaxHeight(19)))
                    {
                        WindowState.playing = false;
                        WindowState.time = 0;
                    }

                    EditorGUI.BeginChangeCheck();
                    bool loop = GUILayout.Toggle(WindowState.loop, CustomGUIStyles.loopIcon, GUI.skin.button, GUILayout.MaxWidth(23), GUILayout.MaxHeight(19));
                    if (EditorGUI.EndChangeCheck())
                    {
                        WindowState.loop = !WindowState.loop;
                    }

                    EditorGUI.BeginChangeCheck();
                    float time = EditorGUILayout.Slider("", (float)m_WindowState.time, 0, SelectedClip.length, GUILayout.MinWidth(120));
                    if (EditorGUI.EndChangeCheck())
                    {
                        WindowState.playing = false;
                        WindowState.time = time;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion
        }

        private void DrawButtons()
        {
            if (SelectedController == null || SelectedClip == null)
                return;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Event List", GUILayout.MaxWidth(100));
                if (GUILayout.Button("Add", GUILayout.MaxWidth(50)))
                {
                    AddEventData((float)WindowState.time);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Save", "�ִϸ��̼� Ŭ�� ����(*.anim)�� ����˴ϴ�"), GUILayout.MaxWidth(100)))
                {
                    Save();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Time", GUILayout.MaxWidth(100));
                EditorGUILayout.LabelField("Object", GUILayout.MaxWidth(100));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Active", GUILayout.MaxWidth(100));
            }
            EditorGUILayout.EndHorizontal();

        }
        private void DrawEventList()
        {
            if (eventDatas.IsNullOrEmpty())
                return;

            foreach (var item in eventDatas)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    item.time = EditorGUILayout.FloatField(item.time, GUILayout.MaxWidth(100));
                    GameObject obj = (GameObject)EditorGUILayout.ObjectField(item.obj, typeof(GameObject), true, GUILayout.MaxWidth(200));
                    EditorGUILayout.LabelField("", GUILayout.MaxWidth(15));
                    item.active = EditorGUILayout.Toggle(item.active, GUILayout.MaxWidth(25));
                    if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
                    {
                        RemoveEventData(item);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        item.obj = obj != null && SelectedController.IsChildObject(obj) ? obj : item.obj;
                        item.ps = item.obj != null ? item.obj.GetComponent<ParticleSystem>() : null;
                        item.SetID();
                        SelectedController.SortEventList();
                        CustomEditorUtil.RegisterUndo(undoKey, SelectedController);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        #endregion

        #region OnGUIEvents
        private void OnSelectedObjectChange(GameObject prefabObj) 
        {

            bool loadPrefab = prefabObj != null;
            if (m_OriginPrefab != null)
            {
                EditorCache.DestroyPrefab(m_OriginPrefab);
                if (m_OriginPrefab == prefabObj)
                    loadPrefab = false;
            }

            Clear();
            if (loadPrefab)
            {
                LoadSelectedObject(prefabObj);
            }
            m_OriginPrefab = prefabObj;
            Repaint();
        }
        private void OnClipChange()
        {
            m_WindowState.Init();
            WindowState.loop = WindowState.SelectedClip.isLooping;

            SelectedController.InitActiveValuesOfChildren();

            Repaint();
        }
        #endregion

        #region PlayAnimation
        private void Update()
        {
            if (!IsReadyToPlayAni)
                return;

            UpdatePlayAnimation();
            if (WindowState.IsTimeChanged)
            {
                SimulateEventData((float)WindowState.time);
                SimulateParticles((float)WindowState.time);
            }
        }

        private void UpdatePlayAnimation()
        {
            m_deltaTime = Time.realtimeSinceStartup - m_beforeTime;

            if (WindowState.playing)
            {
                WindowState.time += m_deltaTime * WindowState.playSpeed;

                if (WindowState.time >= WindowState.duration)
                {
                    if (WindowState.loop)
                    {
                        WindowState.time = 0;
                    }
                    else
                    {
                        WindowState.time = WindowState.duration - 0.0001f;
                    }
                }
            }

            SelectedClip.SampleAnimation(SelectedController.gameObject, (float)WindowState.time);
            Repaint();
            m_beforeTime = Time.realtimeSinceStartup;
        }
        private void SimulateEventData(float time)
        {
            if (eventDatas.IsNullOrEmpty())
                return;

            foreach (var item in eventDatas)
            {
                if (item.time <= time)
                {
                    item.obj?.SetActive(item.active);
                }
            }
        }
        private void SimulateParticles(float time) 
        {
            if (psList.IsNullOrEmpty())
                return;

            foreach (var ps in psList)
            {
                if (ps == null || ps.gameObject.activeInHierarchy == false)
                    continue;

                if (ps.main.loop)
                {
                    ps.Simulate(time, true);
                }
                else
                {
                    if (time <= ps.main.duration)
                    {
                        ps.Simulate(time, true);
                    }
                    else
                    {
                        ps.Simulate(ps.main.duration, true);
                    }
                }
            }
        }
        #endregion

        private void LoadSelectedObject(GameObject prefab)
        {
            if (prefab == null)
                return;

            GameObject instantiatedObj = EditorCache.InstantiatePrefab(prefab, WindowState.objectRootTr);

            if (SelectedController != null)
                WindowState.Clear();

            WindowState.SelectedController = instantiatedObj.GetComponent<EffectActiveController>();
            SelectedController.Editor_Init();
            List<AnimationClip> clips = new List<AnimationClip>();
            if (instantiatedObj.TryGetComponent(out Animator animator))
            {
                AnimatorController animatorController = animator.runtimeAnimatorController as AnimatorController;
                if (animatorController != null)
                {
                    foreach (var layer in animatorController.layers)
                    {
                        foreach (var state in layer.stateMachine.states)
                        {
                            if (state.state.motion != null)
                                clips.Add(state.state.motion as AnimationClip);
                        }
                    }
                }
            }
            else if (instantiatedObj.TryGetComponent(out Animation animation))
            {
                int clipCount = animation.GetClipCount();
                foreach (AnimationState state in animation)
                {
                    if (state.clip != null)
                        clips.Add(state.clip);
                }
            }

            GetParticles(instantiatedObj);

            WindowState.AddClips(clips.ToArray());
            WindowState.Init();
        }
        private void GetParticles(GameObject instantiatedObj)
        {
            List<Transform> childrenObjects = new List<Transform>();
            childrenObjects.Add(instantiatedObj.transform);
            childrenObjects.AddRange(instantiatedObj.transform.GetAllChildrenByDepth());
            psList = childrenObjects.SelectMany(x => x.GetComponentsInParent<ParticleSystem>(true)).Where(x => x.transform.parent.TryGetComponent(out ParticleSystem _) == false).ToList();
        }

        private void CheckAniClipEvent()
        {
            foreach (var clip in WindowState.GetClips)
            {
                if (clip == null)
                    continue;

                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
                foreach (var evt in events)
                {
                    if (evt.functionName.Equals("SetActive") || evt.functionName.Equals("SetDeactive"))
                    {
                        //TODO: check evt's stringParameter is SelectedController's child object
                        if (!SelectedController.IsChildObject(evt.stringParameter))
                        {

                        }
                    }
                }
            }
        }
        private void AddEventData(float time)
        {
            EventData newData = new EventData();
            newData.name = SelectedClip.name;
            newData.time = time;
            SelectedController.AddEventData(SelectedClip, time, newData.obj, newData.active);
        }
        private void RemoveEventData(EventData data)
        {
            SerializedObject serializedObject = new SerializedObject(SelectedController);
            SerializedProperty serializedEventList = serializedObject.FindProperty("eventList");
            serializedObject.Update();

            int idx = SelectedController.EventList.FindIndex(x => x.ID == data.ID);
            serializedEventList.DeleteArrayElementAtIndex(idx);

            serializedObject.ApplyModifiedProperties();
        }
        private void Save()
        {
            SelectedController.SaveToOriginalPrefab();
            foreach (var clip in WindowState.GetClips)
            {
                if (clip == null)
                    continue;
                List<AnimationEvent> animationEvents = new List<AnimationEvent>();
                //remain events that is not related to this tool
                animationEvents.AddRange(AnimationUtility.GetAnimationEvents(clip).Where(evt => evt.functionName.Equals("SetActive") == false && evt.functionName.Equals("SetDeactive") == false));
                //add events that is related to this tool
                animationEvents.AddRange(SelectedController.EventList.Where(data => data.name.Equals(clip.name)).Select(data => data.ToAnimationEvent()));

                AnimationUtility.SetAnimationEvents(clip, animationEvents.ToArray());
            }
        }
    }
}