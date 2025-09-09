namespace AniEventTool.Editor
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    using UnityEngine;
    using UnityEngine.SceneManagement;

    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEditor.SceneManagement;

    using AniEventTool.Editor;
    using Object = UnityEngine.Object;
    using UnityEngine.Events;

    public partial class AniEventToolWindow : EditorWindow
    {
        public static UnityAction OnAniEventAdd { get; }
        public static UnityAction OnAniEventRemove { get; }
        protected static UnityAction OnDisableWindow { get; set; }


        private const string WindowTitle = "Ani Event Editor";

        private static AniEventToolWindow m_Window;
        public static AniEventToolWindow Instance => m_Window;
        public static bool IsOpened => m_Window != null;
        public bool IsFocused => EditorWindow.focusedWindow == this;

        #region PrefabPickerWindow
        PrefabPickerWindow m_PrefabPickerWindow = null;
        #endregion

        [SerializeField] private bool m_IsExitEditor = false;
        [SerializeField] private bool m_isCompiling = false;
        [SerializeField] private bool m_IsDirty = false;
        public bool IsDirty
        {
            get
            {
                return m_IsDirty;
            }
            private set
            {
                if (value)
                {
                    titleContent.text = WindowTitle + "*";
                }
                else
                    titleContent.text = WindowTitle;
                m_IsDirty = value;
            }
        }

        [SerializeField] Camera m_Camera = null;
        private Camera sceneCamera
        {
            get
            {
                if (m_Camera == null && SceneView.sceneViews.Count > 0)
                    m_Camera = (SceneView.sceneViews[0] as SceneView).camera;

                return m_Camera;

            }
        }
        [SerializeField] private GameObject m_OriginPrefab = null;
        [SerializeField] private CachedObject m_AttachObj = null;
        [SerializeField] private int m_AttachSocketIdx = 0; // 0 -> R, 1 -> L, 2~ -> custom??

        [SerializeField] private List<AniEventGroupTrack> m_EventGroupTrackList = new List<AniEventGroupTrack>();


        private AniEventControllerBase SelectedController => m_State.SelectedController;
        internal List<AniEventGroupTrack> EventGroupTrackList => m_EventGroupTrackList;
        internal bool IsEventExist => m_EventGroupTrackList.IsNullOrEmpty() == false;
        private bool IsReadyToPlayAni => !m_isCompiling && !Application.isPlaying && SelectedController && State.SelectedClip;

#if USE_CHJ_SOUND
        [SerializeField] private SoundManager m_SoundManager = null;
        internal SoundManager GetSoundManager => m_SoundManager;
        internal bool IsSoundTableLoaded => AniEventToolEditorCache.SoundTableList.IsNullOrEmpty() == false;
#endif

        #region TimeArea
        private float m_beforeTime = 0.0f;
        private float m_deltaTime = 0;
        private float m_sampleFrame = 0;
        private bool m_IsCursorDragging = false;
        #endregion

        internal Vector2 timeAreaShownRange
        {
            get
            {
                if (m_State.isClipSelected)
                    return m_State.timeAreaShowRange;

                return WindowConstants.TimeAreaDefaultRange;
            }
            set
            {
                SetTimeAreaShownRange(value.x, value.y);
            }
        }
        [SerializeField] TextAsset EventFile = null;

        [SerializeField] WindowState m_State = null;
        internal WindowState State => m_State;

        [MenuItem("Tools/�ִ� �̺�Ʈ ������", false)]
        public static void OpenEditorWindow()
        {
            if (m_Window == null)
            {
                m_Window = GetWindow<AniEventToolWindow>(false, WindowTitle, true);
                m_Window.minSize = new Vector2(512, 128); // �ּ� ũ�� 
                m_Window.Clear();
                m_Window.Init();
            }
            else
            {
                m_Window.Focus();
            }
        }

        private void OnEnable()
        {
            CompilationPipeline.compilationStarted += OnCompileStart;
            CompilationPipeline.compilationFinished += OnCompileFinished;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            EditorSceneManager.sceneOpened += OnSceneChanged;
            EditorSceneManager.sceneUnloaded += OnSceneUnloaded;

            Init();
            AniEventToolEditorCache.OnTrackAdded += OnEventTrackAdded;
            AniEventToolEditorCache.OnTrackRemoved += OnEventTrackRemoved;
        }
        private void OnDisable()
        {
            CompilationPipeline.compilationStarted -= OnCompileStart;
            CompilationPipeline.compilationFinished -= OnCompileFinished;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorSceneManager.sceneOpened -= OnSceneChanged;
            EditorSceneManager.sceneUnloaded -= OnSceneUnloaded;
            AniEventToolEditorCache.OnTrackAdded -= OnEventTrackAdded;
            AniEventToolEditorCache.OnTrackRemoved -= OnEventTrackRemoved;

            InitwindowState();
            m_EventGroupTrackList.Clear();
            m_MoveTrackList.Clear();
            AniEventToolEditorCache.Clear();
            if (SelectedController)
                AniEventToolEditorCache.DestroyImmediate(SelectedController.gameObject);
            Selection.activeObject = null;

        }
        private void OnDestroy()
        {
            //Release cached datas from memory
            Clear();
            AniEventToolEditorCache.DestroyImmediateChildren(m_State.objectRootTr);
            //ReleaseCameraSetting();
            GameObject.DestroyImmediate(m_State.objectRootTr.gameObject);

#if USE_CHJ_SOUND
            if (m_SoundManager != null)
            {
                m_SoundManager.Clear();
                m_SoundManager = null;
            }
#endif
            //if (sceneCamera != null)
            //    sceneCamera.cameraType = CameraType.SceneView;


            ClosePrefabPicker();
        }

        private void Init()
        {
            if (m_Window == null)
                m_Window = this;

            InitwindowState();
            InitializeTimeArea();
            m_beforeTime = Time.realtimeSinceStartup;
            //InitCameraSetting();

            if (m_IsExitEditor)
                return;
#if USE_CHJ_SOUND
            InitSoundManager();
#endif
            if (m_OriginPrefab != null && SelectedController == null)
                LoadSelectedPrefabData(m_OriginPrefab);
            if (m_AttachObj != null && m_AttachObj.PrefabObject != null)
                LoadAttachObjPrefab(m_AttachObj.PrefabObject, "WeaponSocket", m_AttachSocketIdx);

            UpdateEventGroupGUIList();

        }

        private void InitwindowState()
        {
            if (m_State != null)
                m_State.Init();
            else
                m_State = new WindowState();

            m_State.frameRate = m_LastFrameRate;
        }

#if USE_CHJ_SOUND
        private void InitSoundManager()
        {
            SoundManager[] intances = FindObjectsOfType(typeof(SoundManager)) as SoundManager[];
            foreach (SoundManager intance in intances)
            {
                DestroyImmediate(intance.gameObject);
            }

            if (m_SoundManager == null || m_SoundManager.Editor_GetAllSoundInfo.Count == 0)
            {
                m_SoundManager = SoundManager.Instance;
                m_SoundManager.Editor_Initialize();
                m_SoundManager.gameObject.hideFlags = HideFlags.HideAndDontSave;
                m_SoundManager.Mixer.hideFlags = HideFlags.HideAndDontSave;
                m_SoundManager.transform.SetParent(m_State.objectRootTr);
            }
        }
#endif
        private void Clear()
        {
            if (m_State != null)
                m_State.Clear();
            else
                m_State = new WindowState();

            m_State.frameRate = m_LastFrameRate;

            //if (m_Camera)
            //    m_Camera.cameraType = CameraType.SceneView;
            ClearAttachObj();
            m_EventGroupTrackList.Clear();
            UpdateEventGroupGUIList();
            AniEventToolEditorCache.Clear();

        }

        private void OnGUI()
        {
            DrawWindowGUI();

            EventOnTimelineRuler();
            CheckDraggingEvent();

            EventType rawType = Event.current.rawType;
            Vector2 mousePosition = Event.current.mousePosition; // mousePosition is also affected by this bug and does not reflect the original position after a Use()

            EventTrackInputControl.Instance.OnGUI(rawType, mousePosition);
        }
        private void Update()
        {
            //if (sceneCamera != null)
            //    sceneCamera.cameraType = IsReadyToPlayAni ? CameraType.Game : CameraType.SceneView;

            //UpdateCamera();
            if (IsReadyToPlayAni == false)
                return;

            UpdatePlayAnimation();
            Repaint();
        }

        #region EventCallbacks
        private void OnCompileStart(object obj)
        {
            m_isCompiling = true;
            Stop();
            OnDestroy();
        }
        private void OnCompileFinished(object obj)
        {
            m_isCompiling = false;
            m_IsExitEditor = false;
            Init();
        }
        private void OnPlayModeChanged(PlayModeStateChange playMode)
        {
            if (AniEventToolPreferences.settings.saveOnPlay)
                Editor_SaveEventsToJSON();
            switch (playMode)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    { }
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    {
                        Clear();
                        //ResetCameraSetting();
                        //if (sceneCamera != null)
                        //    sceneCamera.cameraType = CameraType.SceneView;
#if USE_CHJ_SOUND
                        if (m_SoundManager != null)
                            DestroyImmediate(m_SoundManager.gameObject);
#endif

                        ClosePrefabPicker();

                        m_State.objectRootTr.gameObject.SetActive(false);
                        m_IsExitEditor = true;
                    }
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    {
                        m_IsExitEditor = false;
                        Init();
                        m_State.objectRootTr.gameObject.SetActive(true);
                    }
                    break;
            }
        }
        private void OnProjectChanged() { }
        public void OnSceneChanged(Scene _scene, OpenSceneMode _mode)
        {
            if (_mode == OpenSceneMode.Single)
            {
                Clear();
                Init();
            }
        }
        public void OnSceneUnloaded(Scene _scene)
        {
            Clear();
        }
        private void OnEventGroupListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateEventGroupGUIList();
        }
        private void OnEventTrackAdded(AniEventTrackBase eventTrack)
        {
            if (eventTrack.GetType().IsSubclassOf(typeof(MoveEventTrackBase)))
            {
                OnMoveEventAdded(eventTrack);
            }
        }
        private void OnEventTrackRemoved(AniEventTrackBase eventTrack)
        {
            if (eventTrack.GetType().IsSubclassOf(typeof(MoveEventTrackBase)))
            {
                OnMoveEventRemoved(eventTrack);
            }
        }
        public void OnJsonFileModified()
        {
            if (m_IsExitEditor)
                return;

            Clear();
            Init();
        }
        #endregion

        #region Anim, Effects Control
        void SetPlaying(bool start)
        {
            if (start && !m_State.playing)
            {
                Play();
            }

            if (!start && m_State.playing)
            {
                Pause();
            }

            //analytics.SendPlayEvent(start);
        }
        void Play()
        {
            m_State.playing = true;

            float endFrame = State.SelectedClip.frameRate * State.duration;
            if (m_sampleFrame >= endFrame)
            {
                m_sampleFrame = 0.0f;

                m_State.time = 0.0f;
            }
        }
        internal void Pause()
        {
            m_State.playing = false;
        }
        void Stop()
        {
            m_State.playing = false;
            m_sampleFrame = 0.0f;
            m_State.time = 0.0f;

            GameObject selectedObj = SelectedController?.gameObject ?? null;
            if (selectedObj != null && State.SelectedClip != null)
                State.SelectedClip.SampleAnimation(selectedObj, 0.0f);

            Repaint();
        }

        private void UpdatePlayAnimation()
        {
            if (IsReadyToPlayAni == false)
                return;

            GameObject selectedObj = SelectedController.gameObject;

            m_deltaTime = Time.realtimeSinceStartup - m_beforeTime;

            if (m_State.playing)
            {
                float endFrame = State.SelectedClip.frameRate * State.duration;
                float timePerFrame = State.duration / endFrame;

                m_State.time += m_deltaTime * State.playSpeed;
                m_sampleFrame = (float)m_State.time / timePerFrame;

                if (m_State.time >= State.duration)
                {
                    if (m_State.loop)
                    {
                        m_State.time = 0.0f;

                        StopEvents();
                        //m_aniEvent.EDStopSound(m_curPlayingAniClip.name);
                        //m_aniEvent.EDStopStepForward();
                        //m_aniEvent.EDStopPushed();
                    }
                    else
                    {
                        m_State.time = State.duration - 0.0001f;
                        Pause();
                    }
                }
            }
            State.SelectedClip.SampleAnimation(selectedObj, (float)m_State.time);
            UpdateControllerTransform((float)m_State.time);
            UpdatePlayEvents((float)m_State.time);
            Repaint();
            m_beforeTime = Time.realtimeSinceStartup;
        }
        private void UpdatePlayEvents(float currentTime)
        {
            foreach (AniEventGroupTrack groupTrack in m_EventGroupTrackList)
                foreach (AniEventTrackBase eventTrack in groupTrack.ChildEventTracks)
                    eventTrack.PlayEvent(currentTime);
        }
        private void StopEvents()
        {
            foreach (AniEventGroupTrack groupTrack in m_EventGroupTrackList)
                foreach (AniEventTrackBase eventTrack in groupTrack.ChildEventTracks)
                    eventTrack.StopEvent();
        }
        #endregion

        private void LoadSelectedPrefabData(GameObject prefab)
        {
            if (prefab == null)
                return;

            Scene mainScene = SceneManager.GetActiveScene();

            GameObject prefabInstance = AniEventToolEditorCache.InstantiatePrefab(prefab, mainScene);
            //get callback when prefab is loaded


            prefabInstance.TryGetComponent(out AniEventControllerBase selectedController);
            if (selectedController == null)
            {
                Debug.Log("<color=yellow>�ش� ������ ���Ͽ� AniEventController ������Ʈ�� �����ϴ�!</color>");
                AniEventToolEditorCache.DestroyImmediate(prefabInstance);
                Clear();
                return;
            }
            selectedController.transform.SetParent(m_State.objectRootTr, false);
            selectedController.gameObject.hideFlags = HideFlags.DontSave; //HideFlags.HideAndDontSave;
            selectedController.gameObject.transform.position = Vector3.zero;
            selectedController.gameObject.transform.rotation = Quaternion.identity;
            selectedController.Editor_SetDrawMeshInfo();
            selectedController.Editor_SetBoneInfo();
            selectedController.Editor_GetAnimations();

            m_State.SelectedController = selectedController;

            OnSelectedCharactorChanged();
        }

        internal void OnSelectedCharactorChanged()
        {
            ReleaseTracks();
            InitwindowState();

            ChangeAniClip();
#if USE_CHJ_SOUND
            //���� ���̺� �ε�
            if (IsSoundTableLoaded == false)
                Debug.Log("<color=yellow>No sound table datas to load!</color>");
#endif
            //ResetCameraSetting();
            string path = AniEventToolPreferences.JSONFilePath + SelectedController.name + ".json";

            //SelectedController.Editor_LoadEventFile(path);
            Editor_LoadEventsFromJSON(path, SelectedController);
            AniEventToolEditorCache.Clear();
            AniEventToolEditorCache.CacheAllEventList(SelectedController);

            UpdateEventGroupList();
            UpdateEventGroupGUIList();
            ReCalculateTimeAreaRange();
        }

        internal void OnAnimationStateChanged()
        {
            ReleaseTracks();
            ChangeAniClip();
            UpdateEventGroupList();
            UpdateEventGroupGUIList();
            ReCalculateTimeAreaRange();
        }

        private void ReleaseTracks()
        {
            Stop();
            EventTrackInputControl.Instance.DeselectTrackGUI();
            if (m_EventGroupTrackList.IsNullOrEmpty() == false)
            {
                foreach (AniEventGroupTrack prevGroupTrack in m_EventGroupTrackList)
                    foreach (AniEventTrackBase childTrack in prevGroupTrack.ChildEventTracks)
                    {
                        childTrack.OnRelease();
                        OnEventTrackRemoved(childTrack);
                    }
            }

        }
        private void ChangeAniClip()
        {
            if (!SelectedController.Editor_TryGetAnimationClip(m_State.aniStateSelection, out AnimationClip selectedClip))
            {
                m_State.aniStateSelection = 0;
                SelectedController.Editor_TryGetAnimationClip(0, out selectedClip);
            }
            SelectedController.Editor_ResetTransform();

            if (selectedClip == null)
                Debug.LogError("<color=yellow>Animator�� �ִϸ��̼� Ŭ���� �����ϴ�.</color>");
            else
            {
                m_State.playSpeed = 1;
                m_State.SelectedClip = selectedClip;
                m_State.duration = selectedClip.length;
            }
        }
        private void UpdateEventGroupList()
        {
            List<AniEventGroup> eventGroupList = SelectedController.Editor_GetEventList(m_State.aniStateSelection);//new List<AniEventGroup>();


            m_EventGroupTrackList.Clear();
            m_MoveTrackList.Clear();
            List<AniEventGroupTrack> groupTrackList = new List<AniEventGroupTrack>();
            foreach (AniEventGroup eventGroup in eventGroupList)
            {
                AniEventGroupTrack groupTrack = AniEventToolEditorCache.GetEventTrackGUI(eventGroup).EventTrack as AniEventGroupTrack;
                groupTrackList.Add(groupTrack);

                foreach (AniEventTrackBase childTrack in groupTrack.ChildEventTracks)
                    OnEventTrackAdded(childTrack);
            }

            if (groupTrackList.IsNullOrEmpty() == false)
                groupTrackList.Sort((group1, group2) => group1.index.CompareTo(group2.index));

            m_EventGroupTrackList.AddRange(groupTrackList);
        }

        private void LoadAttachObjPrefab(GameObject objFile, string targetBoneKey, int socketSideIdx)
        {
            GameObject prefabInstance = null;
            string socketSideKey = GetSocketSideKey(socketSideIdx);
            string socketName = SelectedController.GetBoneNameList.Find(bone => bone.Contains(targetBoneKey, StringComparison.OrdinalIgnoreCase) && bone.Contains(socketSideKey, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(socketName) == false)
            {
                Scene mainScene = SceneManager.GetActiveScene();
                prefabInstance = AniEventToolEditorCache.InstantiatePrefab(objFile, mainScene) as GameObject;
                Transform boneTr = SelectedController.Editor_GetBone(socketName);
                prefabInstance.transform.position = boneTr.position;
                prefabInstance.transform.rotation = boneTr.rotation;
                prefabInstance.transform.SetParent(boneTr);
            }
            else
            {
                DisplayDialog.ShowDisplayDialog($"�̸��� {targetBoneKey}, {socketSideKey}�� ������ ���� ã�� �� �����ϴ�!",
                    () =>
                    {

                    });
            }

            if (prefabInstance != null && m_AttachObj != null && m_AttachObj.ObjectInstance != null)
            {
                AniEventToolEditorCache.DestroyImmediate(m_AttachObj.ObjectInstance);
            }

            if (m_AttachObj != null)
                m_AttachObj.ReNew(objFile, prefabInstance);
            else
                m_AttachObj = new CachedObject(objFile, prefabInstance);
            m_AttachSocketIdx = socketSideIdx;

        }
        private string GetSocketSideKey(int socketSideIdx)
        {
            switch (socketSideIdx)
            {
                case 0: return "_R";
                case 1: return "_L";
                default: return "_None";
            }
        }
        private void ClearAttachObj()
        {
            m_AttachObj?.Clear();
        }

        void AddEventGroup()
        {
            AniEventGroup newEventGroup = SelectedController.Editor_AddEventGroup(State.aniStateSelection);//AniEventToolEditorCache.GetAniEvent(AniEventType.Group) as AniEventGroup;
            int index = m_EventGroupTrackList.IsNullOrEmpty() ? 0 : m_EventGroupTrackList.Max(item => item.index) + 1;
            newEventGroup.index = index;

            AniEventGroupTrack newGroupTrack = AniEventToolEditorCache.GetEventTrackAsset(newEventGroup) as AniEventGroupTrack;
            m_EventGroupTrackList.Add(newGroupTrack);
            UpdateEventGroupGUIList();
            Repaint();
        }
        internal void AddChildEvent(AniEventBase evt, AniEventGroup parentGroup)
        {
            evt.startTime = (float)State.time;

            if (evt is not AniEventGroup)
                parentGroup.AddChildEvent(evt);

            EventTrackGUIBase trackGUI = AniEventToolEditorCache.GetEventTrackGUI(evt, parentGroup);
            EventTrackInputControl.Instance.SelectTrackGUI(trackGUI);
            UpdateEventGroupGUIList();
            Repaint();

            foreach (AniEventGroupTrack groupTrack in m_EventGroupTrackList)
                foreach (AniEventTrackBase eventTrack in groupTrack.ChildEventTracks)
                    eventTrack.Refresh();
        }
        internal void DeleteEvent(EventTrackGUIBase eventTrackGUI)
        {
            if (eventTrackGUI.EventTrack is AniEventGroupTrack aniEventGroupTrack)
                m_EventGroupTrackList.Remove(aniEventGroupTrack);
            else
            {
                AniEventGroup parentGroup = eventTrackGUI.EventTrack.ParentGroupTrack.eventGroup;
                parentGroup.RemoveEvent(eventTrackGUI.EventTrack.GetAniEvent);
            }

            AniEventToolEditorCache.DeleteEventTrack(eventTrackGUI);
            UpdateEventGroupGUIList();
            Repaint();

            foreach (AniEventGroupTrack groupTrack in m_EventGroupTrackList)
                foreach (AniEventTrackBase eventTrack in groupTrack.ChildEventTracks)
                    eventTrack.Refresh();
        }

        public void Editor_SaveEventsToJSON()
        {
            if (SelectedController == null)
                return;
            Selection.activeObject = null;
            string path = AniEventToolPreferences.JSONFilePath + SelectedController.name + ".json";

            string[] stateNames = SelectedController.Editor_GetAniStateNames();
            List<AniStateInfo> animInfoList = new List<AniStateInfo>();
            List<(AnimInfo, List<AniEventGroup>)> eventList = new List<(AnimInfo, List<AniEventGroup>)>();
            for (int i = 0; i < stateNames.Length; i++)
            {
                string stateName = stateNames[i];
                if (AniEventToolEditorCache.TryGetEventGroupList(stateName, out List<AniEventGroup> originEventGroupList))
                {
                    int eventCount = 0;
                    foreach (AniEventGroup eventGroup in originEventGroupList)
                    {
                        AniEventToolEditorCache.GetEventTrackAsset(eventGroup).ApplyToEventData();
                        eventCount += eventGroup.Editor_GetValidEventCount;
                    }

                    if (SelectedController.Editor_GetValidStateInfo(i, out AnimInfo animInfo, out AniStateInfo stateInfo) == false)
                        continue;

                    animInfoList.Add(stateInfo);
                    eventList.Add((animInfo, originEventGroupList));
                }
            }
            if(CommonUtil.SaveEventFileToJSON(animInfoList, eventList, path))
                Debug.Log($"<color=green>�ִϸ��̼� �̺�Ʈ ������ ����Ǿ����ϴ�. {path}</color>");
            else
                Debug.LogError($"<color=red>�ִϸ��̼� �̺�Ʈ ���� ���忡 �����߽��ϴ�. {path}</color>");

            AssetDatabase.Refresh();
        }

        public bool Editor_LoadEventsFromJSON(string path, AniEventControllerBase selectedController)
        {
            string relativePath = path;
            relativePath = "Assets" + relativePath.Remove(0, Application.dataPath.Length);
            Animator animator = selectedController.GetComponent<Animator>();
            TextAsset file = AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath);

            if (file == null)
                return false;
            List<AnimInfo> currentAnimInfos = selectedController.Editor_GetAniEventDic.Keys.ToList();
            List<(AnimInfo, List<AniEventGroup>)> loadedData;
            bool readJsonResult = CommonUtil.LoadEventDataFromJSON(new JsonObject(file.text), out loadedData);
            if (readJsonResult == false)
            {
                Debug.LogError("������ �о�� �� �����ϴ�.");
                return false;
            }

            foreach ((AnimInfo, List<AniEventGroup>) kv in loadedData)
            {
                selectedController.Editor_SetAllEventsData(kv.Item1, kv.Item2);
            }
            EventFile = file;
            AssetDatabase.Refresh();

            return false;
        }

        void TEST()
        {
            //Camera camera = SceneView.lastActiveSceneView.camera;

            //camera.cameraType = CameraType.Preview;
            //camera.enabled = false;
            //camera.clearFlags = CameraClearFlags.Depth;
            //camera.fieldOfView = 15f;
            //camera.farClipPlane = 10f;
            //camera.nearClipPlane = 2f;
            //camera.renderingPath = RenderingPath.Forward;
            //camera.useOcclusionCulling = false;
            //IsDirty = !IsDirty;
        }
        void DrawTestButton()
        {
            if (GUILayout.Button("test", EditorStyles.toolbarButton, GUILayout.MaxWidth(70), GUILayout.MaxHeight(19)))
            {
                TEST();
            }
        }
    }
}
