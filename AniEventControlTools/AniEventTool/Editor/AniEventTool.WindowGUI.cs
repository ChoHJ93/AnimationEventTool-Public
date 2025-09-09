namespace AniEventTool.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using ContextMenuItem = CustomEditorUtil.ContextMenuItem;

    public partial class AniEventToolWindow
    {
        const float handleWidth = 1.5f;
        const float timeAreaHeight = 22.0f;
        const float toolbarHeight = 21f;
        const int visibilityBufferInPixels = 10;
        const float TrackHeightWithPadding = WindowConstants.eventItemHeight + WindowConstants.defaultPadding;

        readonly Color resizeHandleColor = new Color(0.156f, 0.156f, 0.156f);

        [SerializeField] List<EventTrackGUIBase> m_TrackGUIList = new List<EventTrackGUIBase>();
        internal List<EventTrackGUIBase> GetTrackGUIList => m_TrackGUIList;

        internal Rect TimeAreaRect
        {
            get
            {
                float x = position.width * eventListWidth_Ratio + 1;
                float y = toolbarHeight;
                float width = position.width * (1 - eventListWidth_Ratio) - 1;
                return new Rect(x, y, width, timeAreaHeight);

            }
        }
        internal Rect EventLisViewRect
        {
            get
            {
                float y = toolbarHeight + timeAreaHeight;
                float width = position.width * eventListWidth_Ratio - 1;
                float height = position.height - y;
                return new Rect(0, y, width, height);
            }
        }
        internal Rect EventTrackViewRect
        {
            get
            {
                float x = position.width * eventListWidth_Ratio + 1;
                float y = toolbarHeight + timeAreaHeight;
                float width = position.width * (1 - eventListWidth_Ratio) - 1;
                float height = position.height - y - GUI.skin.horizontalScrollbar.fixedHeight;
                return new Rect(x, y, width, height);
            }
        }

        internal Rect EventTrackRect
        {
            get
            {
                float y = toolbarHeight + timeAreaHeight;
                Rect rect = new Rect(position);
                rect.x = 0;
                rect.y = y;
                rect.height -= y - GUI.skin.horizontalScrollbar.fixedHeight;
                return rect;
            }
        }

        [SerializeField] float eventListWidth_Ratio = 0.2f;
        [SerializeField] Vector2 m_eventViewScrollPos = Vector2.zero;
        public Vector2 ScrollPos => m_eventViewScrollPos;
        bool m_IsResizingX = false;

        internal void UpdateEventGroupGUIList()
        {
            m_TrackGUIList.Clear();
            foreach (AniEventGroupTrack groupTrack in m_EventGroupTrackList)
            {
                m_TrackGUIList.Add(AniEventToolEditorCache.GetEventTrackGUI(groupTrack.GetAniEvent));
                foreach (AniEventTrackBase eventTrack in groupTrack.ChildEventTracks)
                {
                    m_TrackGUIList.Add(AniEventToolEditorCache.GetEventTrackGUI(eventTrack.GetAniEvent));
                }
            }
        }

        /// <summary>
        /// call in OnGUI / Call All Drawing GUI Methods
        /// </summary>
        private void DrawWindowGUI()
        {
            DrawResizeHandles();

            DrawToolbar();

            if (m_IsExitEditor)
                return;

            DrawEventTracks();
            DrawTimeCursor();
        }

        /// <summary>
        /// Call when Track's value Edited On Ispector
        /// </summary>
        internal void ReDrawGUI()
        {
            DrawWindowGUI();
        }
        private void DrawResizeHandles()
        {
            float width_HandleX = position.width * eventListWidth_Ratio;
            float height_HandleX = position.height - toolbarHeight;
            Rect rect_HandleX = new Rect(width_HandleX, toolbarHeight, handleWidth, height_HandleX);
            EditorGUI.DrawRect(rect_HandleX, resizeHandleColor);
            rect_HandleX.x -= 1;
            rect_HandleX.width += 2;
            EditorGUIUtility.AddCursorRect(rect_HandleX, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && rect_HandleX.Contains(Event.current.mousePosition))
                m_IsResizingX = true;

            if (m_IsResizingX)
            {
                eventListWidth_Ratio = Mathf.Clamp(Event.current.mousePosition.x / position.width, 0.1f, 0.9f);
                Repaint();
            }

            if (Event.current.type == EventType.MouseUp)
                m_IsResizingX = false;
        }

        private void DrawToolbar()
        {
            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    //using (new GUILayout.HorizontalScope())
                    //{
                    //}

                    TargetPrefabField();
                    //WeaoponObjField();
                    EditorGUI.BeginDisabledGroup(Application.isPlaying || SelectedController == null);
                    GotoBeginingButtonGUI();
                    PreviousFrameButtonGUI();
                    PlayButtonGUI();
                    NextFrameButtonGUI();
                    GotoEndFrameGUI();
                    StopButtonGUI();
                    LoopButtonGUI();

                    TimeCodeGUI();
                    AniStateSelectPopupGUI();
                    SaveButtonGUI();

                    GUILayout.FlexibleSpace();
                    EditorGUI.EndDisabledGroup();

                    //DrawCameraModePopup();

                    SettingButtonGUI();

                    //DrawTestButton();
                    //DrawSequenceSelector();
                    //DrawBreadcrumbs();
                    //DrawOptions();
                }

                using (new GUILayout.HorizontalScope())
                {
                    AddEventGroupButton();
                    DrawTimelineRuler();
                }
            }
        }

        private void DrawEventTracks()
        {
            Rect rect = EventTrackRect;
            using (new GUIViewportScope(rect))
            {
                //Draw BG
                EditorGUI.DrawRect(EventLisViewRect, CustomGUIStyles.colorEventListBackground);
                EditorGUI.DrawRect(EventTrackViewRect, CustomGUIStyles.colorEventListBackground);
                m_TimeArea.DrawMajorTicks(EventTrackViewRect, (float)m_State.frameRate);

                //Draw Tracks
                GUILayout.BeginArea(rect);
                m_eventViewScrollPos = EditorGUILayout.BeginScrollView(m_eventViewScrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Width(rect.width), GUILayout.Height(rect.height - GUI.skin.horizontalScrollbar.fixedHeight));

                DrawEventListView(EventLisViewRect);
                DrawEventTrackView(EventTrackViewRect);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }

        }

        #region Toolbar_GUIs
        void TargetPrefabField()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            Rect prefabRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.objectFieldMiniThumb, GUILayout.Width(150));
            GameObject prefabObj = EditorGUI.ObjectField(prefabRect, "", m_OriginPrefab, typeof(GameObject), false) as GameObject;
            EditorGUI.EndDisabledGroup();
            Rect searchBtnRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.toolbarButton, GUILayout.Width(25));
            searchBtnRect.x -= 15;
            if (GUI.Button(searchBtnRect, CustomGUIStyles.searchIcon, EditorStyles.toolbarButton))
            {
                OpenPrefabPicker();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
        }
        void OpenPrefabPicker()
        {
            ClosePrefabPicker();

            m_PrefabPickerWindow = EditorWindow.GetWindow<PrefabPickerWindow>();

            m_PrefabPickerWindow.Show(prefab =>
            {
                m_OriginPrefab = prefab;
                Clear();
                LoadSelectedPrefabData(m_OriginPrefab);
                Repaint();
            }, 

            () => 
            {
                Repaint(); 
            });
        }

        protected void ClosePrefabPicker() 
        {
            if (m_PrefabPickerWindow != null)
            {
                m_PrefabPickerWindow.Close();
                m_PrefabPickerWindow = null;
            }
        }

        void WeaoponObjField()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying || m_OriginPrefab == null);
            EditorGUI.BeginChangeCheck();
            Event e = Event.current;
            GameObject weaponObj = EditorGUILayout.ObjectField("", m_AttachObj?.PrefabObject ?? null, typeof(GameObject), false, GUILayout.Width(100)) as GameObject;
            string[] socketNames = { "R", "L" };//, "X" };
            int weaponSocketIdx = EditorGUILayout.Popup(m_AttachSocketIdx, socketNames, GUILayout.Width(30));
            if (e.type == EventType.MouseDown)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                if (!rect.Contains(e.mousePosition))
                    GUI.FocusControl(null);
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (weaponObj != null)
                    LoadAttachObjPrefab(weaponObj, "WeaponSocket", weaponSocketIdx);
                else
                {
                    ClearAttachObj();
                    m_AttachSocketIdx = weaponSocketIdx;
                }

                Repaint();
            }
            EditorGUI.EndDisabledGroup();
        }
        void GotoBeginingButtonGUI()
        {
            if (GUILayout.Button(CustomGUIStyles.gotoBeginingContent, EditorStyles.toolbarButton))
            {
                m_State.time = 0;
                EnsurePlayHeadIsVisible(m_State.time);
            }
        }
        void PreviousFrameButtonGUI()
        {
            if (GUILayout.Button(CustomGUIStyles.previousFrameContent, EditorStyles.toolbarButton))
            {
                if (State.SelectedClip == null)
                    return;

                m_State.time = TimeUtilityReflect.PreviousFrameTime(State.time, State.frameRate);
            }
        }
        void PlayButtonGUI()
        {
            EditorGUI.BeginChangeCheck();
            GUIContent buttonIcon = m_State.playing ? CustomGUIStyles.pauseIcon : CustomGUIStyles.playIcon;
            bool isPlaying = GUILayout.Toggle(m_State.playing, buttonIcon, EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                SetPlaying(isPlaying);
            }
        }
        void StopButtonGUI()
        {
            if (GUILayout.Button(CustomGUIStyles.stopIcon, EditorStyles.toolbarButton, GUILayout.MaxWidth(23), GUILayout.MaxHeight(19)))
            {
                Stop();
            }
        }
        void LoopButtonGUI()
        {
            EditorGUI.BeginChangeCheck();
            bool loop = GUILayout.Toggle(m_State.loop, CustomGUIStyles.loopIcon, EditorStyles.toolbarButton, GUILayout.MaxWidth(23), GUILayout.MaxHeight(19));
            if (EditorGUI.EndChangeCheck())
            {
                m_State.loop = !m_State.loop;
            }
        }
        void NextFrameButtonGUI()
        {
            if (GUILayout.Button(CustomGUIStyles.nextFrameContent, EditorStyles.toolbarButton))
            {
                if (State.SelectedClip == null)
                    return;

                m_State.time = TimeUtilityReflect.NextFrameTime(State.time, State.frameRate);
            }
        }
        void GotoEndFrameGUI()
        {
            if (GUILayout.Button(CustomGUIStyles.gotoEndContent, EditorStyles.toolbarButton))
            {
                m_State.time = m_State.duration;
                EnsurePlayHeadIsVisible(m_State.time);
            }
        }
        void TimeCodeGUI()
        {
            const string frameFieldHint = "SkillEditor-FrameCodeGUI";
            EditorGUI.BeginChangeCheck();
            string currentTime = ToTimeString(m_State.time, m_State.frameRate, "0.####");

            Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.toolbarTextField, GUILayout.Width(WindowConstants.timeCodeWidth));
            int id = GUIUtility.GetControlID(frameFieldHint.GetHashCode(), FocusType.Keyboard, r);
            string newCurrentTime = EditorGUI.TextField(r, GUIContent.none, currentTime, EditorStyles.toolbarTextField);
            if (EditorGUI.EndChangeCheck())
            {
                m_State.time = FromTimeString(newCurrentTime);
            }
        }
        void AniStateSelectPopupGUI()
        {
            if (SelectedController == null)
                return;
            EditorGUI.BeginChangeCheck();
            m_State.aniStateSelection = EditorGUILayout.Popup(m_State.aniStateSelection, SelectedController.Editor_GetAniStateNames(), EditorStyles.toolbarPopup, GUILayout.MaxWidth(180));
            if (EditorGUI.EndChangeCheck())
            {
                OnAnimationStateChanged();
            }
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("", State.SelectedClip ?? null, typeof(AnimationClip), false, GUILayout.MaxWidth(120));
            EditorGUILayout.Toggle(State.useRootMotion, GUILayout.MaxWidth(12));
            CustomEditorUtil.DynamicLabelField("RootMotion");
            EditorGUI.EndDisabledGroup();
        }

        void SaveButtonGUI()
        {
            if (GUILayout.Button("save", EditorStyles.toolbarButton, GUILayout.MaxWidth(50), GUILayout.MaxHeight(19)))
            {
                Editor_SaveEventsToJSON();
            }

        }
        void SettingButtonGUI()
        {
            if (EditorGUILayout.DropdownButton(CustomGUIStyles.optionsCogIcon, FocusType.Keyboard, EditorStyles.toolbarButton))
            {
                List<ContextMenuItem> menuItems = new List<ContextMenuItem>();
                int priority = 0;

                menuItems.Add(CustomEditorUtil.CreateContextMenuItem("Open Preference", priority++, () => { SettingsService.OpenProjectSettings("AniEventTool"); }));
                
                CustomEditorUtil.ShowContextMenu(menuItems.ToArray());
            }

        }

        void AddEventGroupButton()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying || SelectedController == null || m_State == null);

            using (new GUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Width(EventLisViewRect.width)))
            {
                GUILayout.Space(WindowConstants.kBaseIndent);
                if (GUILayout.Button(CustomGUIStyles.newContent, EditorStyles.toolbarButton, GUILayout.Width(23)))
                {
                    AddEventGroup();
                }
            }

            EditorGUI.EndDisabledGroup();
        }


        #endregion

        #region EventTrack
        private void DrawEventListView(Rect rect)
        {
            rect.y = 0;

            Rect scopedRect = new Rect(rect);
            scopedRect.y += ScrollPos.y;
            scopedRect.yMax += ScrollPos.y;
            using (new GUIViewportScope(scopedRect))
            {
                foreach (var trackGUI in m_TrackGUIList)
                {
                    AniEventGroupTrack parentTrack = trackGUI.EventTrack.ParentGroupTrack;
                    if (parentTrack != null && parentTrack.foldoutState == false)
                        continue;

                    Rect controlRect = EditorGUILayout.GetControlRect(GUILayout.Height(TrackHeightWithPadding));
                    trackGUI.DrawHeader(controlRect, rect);
                }
            }
        }
        private void DrawEventTrackView(Rect rect)
        {
            rect.y = 0;
            float scrollY = ScrollPos.y - GUI.skin.horizontalScrollbar.fixedHeight;
            Rect scopedRect = new Rect(rect);
            scopedRect.y += scrollY;
            scopedRect.yMax += GUI.skin.horizontalScrollbar.fixedHeight; 
            using (new GUIViewportScope(scopedRect))
            {
                foreach (var trackGUI in m_TrackGUIList)
                {
                    if (trackGUI.EventTrack is AniEventGroupTrack && ((AniEventGroupTrack)trackGUI.EventTrack).foldoutState)
                        continue;

                    AniEventGroupTrack parentTrack = trackGUI.EventTrack.ParentGroupTrack;
                    if (parentTrack != null && parentTrack.foldoutState == false)
                        continue;

                    var chidEventTrack = trackGUI.EventTrack;
                    var childTrackGUI = AniEventToolEditorCache.GetEventTrackGUI(chidEventTrack.GetAniEvent);
                    Rect calculatedRect = CalculateContentRect(rect, childTrackGUI.EventTrack.startTime, childTrackGUI.EventTrack.endTime);
                    childTrackGUI.DrawContent(calculatedRect.x + TimeAreaRect.x, calculatedRect.width);
                    Rect bounds = new Rect(childTrackGUI.HeaderBound);
                    bounds.x = calculatedRect.x;
                    bounds.width = calculatedRect.width;
                    childTrackGUI.SetContentBounds(bounds);
                }
            }
        }
        #endregion

        Rect CalculateContentRect(Rect contentViewRect, float startTime, float endTime)
        {
            contentViewRect.x = 0;
            contentViewRect.y = 0;
            Rect calculatedRect = RectToTimeline(contentViewRect, startTime, endTime);

            calculatedRect.xMin = Mathf.Max(calculatedRect.xMin, contentViewRect.xMin);
            calculatedRect.xMax = Mathf.Min(calculatedRect.xMax, contentViewRect.xMax);

            if (calculatedRect.width > 0 && calculatedRect.width < 2)
            {
                calculatedRect.width = 5.0f;
            }
            calculatedRect.y = -WindowConstants.defaultPadding;
            calculatedRect.height = WindowConstants.eventItemHeight;
            return calculatedRect;
        }

        Rect RectToTimeline(Rect trackRect, float startTime, float endTime)
        {
            float offsetFromTimeSpaceToPixelSpace = m_TimeArea.translation.x + trackRect.xMin;
            float start = startTime;
            float end = endTime <= 0 ? start + Mathf.Min(1f, m_State.duration) : endTime;

            return Rect.MinMaxRect(
                Mathf.Round(start * m_TimeArea.scale.x + offsetFromTimeSpaceToPixelSpace), Mathf.Round(trackRect.yMin),
                Mathf.Round(end * m_TimeArea.scale.x + offsetFromTimeSpaceToPixelSpace), Mathf.Round(trackRect.yMax)
            );
        }

        public float TrackRectWidthToTimeline(float statTime, float endTime)
        {
            Rect rect = RectToTimeline(EventTrackRect, statTime, endTime);
            return rect.width;
        }
        #region NoticePopup
        internal void ShowMessagePopup()
        {
            bool userClickedOk = EditorUtility.DisplayDialog(
               "Title of the Dialog",
               "This is the message of the dialog.",
               "OK",
               "Cancel"
           );

            if (userClickedOk)
            {
                Debug.Log("User clicked OK.");
            }
            else
            {
                Debug.Log("User clicked Cancel.");
            }
        }
        #endregion
    }
}
