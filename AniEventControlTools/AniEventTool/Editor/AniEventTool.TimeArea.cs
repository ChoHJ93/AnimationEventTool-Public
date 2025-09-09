namespace AniEventTool.Editor
{
    using System;
    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    using AniEventTool.Editor;

    public partial class AniEventToolWindow
    {
        [NonSerialized] TimeArea m_TimeArea = null;
        internal TimeArea TimeArea => m_TimeArea;

        private double m_LastFrameRate = -1;
        private bool m_TimeAreaDirty = true;

        void InitializeTimeArea()
        {
            if (m_TimeArea != null)
                m_TimeArea = null;
            m_TimeArea = new TimeArea(false)
            {
                hRangeLocked = false,
                vRangeLocked = true,
                margin = 10,
                scaleWithWindow = true,
                hSlider = true,
                vSlider = false,
                hBaseRangeMin = 0.0f,
                hBaseRangeMax = (float)TimeUtilityReflect.k_MaxTimelineDurationInSeconds,
                hRangeMin = 0.0f,
                hScaleMax = WindowConstants.maxTimeAreaScaling,
                rect = TimeAreaRect
            };

            m_TimeAreaDirty = true;
            InitTimeAreaFrameRate();
            SyncTimeAreaShownRange();
        }
        void DrawTimelineRuler()
        {
            if (IsReadyToPlayAni == false)
                return;
            Rect rect = TimeAreaRect;
            m_TimeArea.rect = new Rect(rect.x, rect.y, rect.width, position.height - rect.y);

            if (m_LastFrameRate != m_State.frameRate)
                InitTimeAreaFrameRate();

            SyncTimeAreaShownRange();

            m_TimeArea.BeginViewGUI();
            m_TimeArea.TimeRuler(rect, (float)m_State.frameRate, true, false, 1.0f, TimeArea.TimeFormat.TimeFrame);
            m_TimeArea.EndViewGUI();
        }
        void InitTimeAreaFrameRate()
        {
            m_LastFrameRate = TimeUtilityReflect.ToFrameRateValue(StandardFrameRates.Fps60);//m_State.referenceSequence.frameRate;
            m_State.frameRate = m_LastFrameRate;
            m_TimeArea.hTicks.SetTickModulosForFrameRate((float)m_LastFrameRate);
        }
        void SyncTimeAreaShownRange()
        {
            var range = timeAreaShownRange;
            if (!Mathf.Approximately(range.x, m_TimeArea.shownArea.x) || !Mathf.Approximately(range.y, m_TimeArea.shownArea.xMax))
            {
                // set view data onto the time area
                if (m_TimeAreaDirty)
                {
                    m_TimeArea.SetShownHRange(range.x, range.y);
                    m_TimeAreaDirty = false;
                }
                else
                {
                    // set time area data onto the view data
                    TimeAreaChanged();
                }
            }

            m_TimeArea.hBaseRangeMax = (float)m_State.duration;
        }
        void DrawTimeOnSlider()
        {
            if (m_IsCursorDragging)
            {
                var colorDimFactor = EditorGUIUtility.isProSkin ? 0.7f : 0.9f;
                var c = CustomGUIStyles.timeCursor.normal.textColor * colorDimFactor;

                float time = Mathf.Max((float)m_State.time, 0);
                float duration = (float)m_State.duration;

                m_TimeArea.DrawTimeOnSlider(time, c, duration, WindowConstants.kDurationGuiThickness);
            }
        }

        void DrawTimeCursor(bool drawLine = true, bool drawHead = true, bool showTimeText = false)
        {
            if (IsReadyToPlayAni == false)
                return;

            Rect clipRect = new Rect(EventTrackViewRect);
            clipRect.height += toolbarHeight - WindowConstants.defaultPadding;
            clipRect.y = toolbarHeight + WindowConstants.defaultPadding;

            using (new GUIViewportScope(clipRect))
            {
                Vector2 windowCoordinate = new Vector2(EventTrackViewRect.min.x, toolbarHeight);
                //windowCoordinate.y += 4.0f;

                windowCoordinate.x = m_TimeArea.TimeToPixel((float)m_State.time, TimeAreaRect);

                float widgetWidth = CustomGUIStyles.timeCursor.fixedWidth;
                float widgetHeight = CustomGUIStyles.timeCursor.fixedHeight;
                Rect boundingRect = new Rect((windowCoordinate.x - widgetWidth / 2.0f), windowCoordinate.y, widgetWidth, widgetHeight);


                // Do not paint if the time cursor goes outside the timeline bounds...
                if (Event.current.type == EventType.Repaint)
                {
                    if (boundingRect.xMax < clipRect.xMin)
                        return;
                    if (boundingRect.xMin > clipRect.xMax)
                        return;
                }

                var top = new Vector3(windowCoordinate.x, CustomGUIStyles.timeCursor.fixedHeight + WindowConstants.kDurationGuiThickness);
                var bottom = new Vector3(windowCoordinate.x, clipRect.yMax);

                if (drawLine)
                {
                    Rect lineRect = Rect.MinMaxRect(top.x - 0.5f, top.y, bottom.x + 0.5f, bottom.y);
                    EditorGUI.DrawRect(lineRect, CustomGUIStyles.timeCursor.normal.textColor);
                }

                if (drawHead && Event.current.type == EventType.Repaint)
                {
                    float x = windowCoordinate.x - CustomGUIStyles.timeCursor.fixedWidth * 0.5f;

                    Rect bounds = new Rect(x, WindowConstants.timeAreaYPosition + WindowConstants.defaultPadding, CustomGUIStyles.timeCursor.fixedWidth, CustomGUIStyles.timeCursor.fixedHeight);
                    Color c = GUI.color;
                    GUI.color = Color.white;
                    CustomGUIStyles.timeCursor.Draw(bounds, new GUIContent(), false, false, false, false);
                    GUI.color = c;

                    //if (canMoveHead)
                    EditorGUIUtility.AddCursorRect(boundingRect, MouseCursor.MoveArrow);
                }
                /* Show TimeText */
                if (showTimeText)
                {
                    string text = ToTimeString(m_State.time, m_State.frameRate);

                    Vector2 position = boundingRect.position;
                    position.y = TimeAreaRect.y;
                    position.y -= WindowConstants.timAreaCursorTextHeight;
                    position.x -= WindowConstants.timAreaCursorTextWidth * 0.5f;//Mathf.Abs(WindowConstants.timAreaCursorTextWidth - bounds.width) / 2.0f;

                    Rect tooltipBounds = boundingRect;
                    tooltipBounds.position = position;
                    //m_Tooltip.bounds = tooltipBounds;
                    //m_Tooltip.Draw();

                    EditorGUI.DrawRect(tooltipBounds, CustomGUIStyles.colorTimelineBackground);
                    GUI.Label(tooltipBounds, text, CustomGUIStyles.displayBackground);
                }

            }
        }
        void EventOnTimelineRuler()
        {
            #region Click or Move cursor event
            if (TimeAreaRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    SetPlaying(false);
                    m_State.time = Math.Max(0.0, GetSnappedTimeAtMousePosition(Event.current.mousePosition, m_State.frameRate));
                    m_IsCursorDragging = true; // �巡�� ����
                    Event.current.Use(); // �̺�Ʈ �Һ�
                }

            }
            #endregion

        }
        void CheckDraggingEvent()
        {
            Rect draggingArea = new Rect(EventTrackViewRect);
            draggingArea.y = 0;
            draggingArea.height = position.height;
            //EditorGUI.DrawRect(EventTrackViewRect, Color.green);
            if (m_IsCursorDragging == false)
                return;
            if (Event.current.type == EventType.MouseDrag && draggingArea.Contains(Event.current.mousePosition))
            {
                m_State.time = Math.Max(0.0, GetSnappedTimeAtMousePosition(Event.current.mousePosition, m_State.frameRate));
                Event.current.Use(); // �̺�Ʈ �Һ�
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                m_IsCursorDragging = false; // �巡�� ����
                m_State.time = m_State.time;
                Event.current.Use(); // �̺�Ʈ �Һ�
            }
        }

        internal float PixelToFrame(float pixel)
        {
            return PixelToTime(pixel) * (float)m_State.frameRate;
        }
        internal float PixelToTime(float pixel)
        {
            return m_TimeArea.PixelToTime(pixel, TimeAreaRect);
        }
        internal float PixelDeltaToDeltaTime(float p)
        {
            return PixelToTime(p) - PixelToTime(0);
        }
        internal string ToTimeString(double time, double frameRate, string format = "f2")
        {
            return TimeUtilityReflect.TimeAsFrames(time, frameRate, format);
        }
        internal double FromTimeString(string timeString)
        {
            double newTime = TimeUtilityReflect.FromTimeString(timeString, m_State.frameRate, -1);
            //if (newTime >= 0.0)
            //{
            //    return m_State.timeReferenceMode == TimeReferenceMode.Global ?
            //        m_State.editSequence.ToLocalTime(newTime) : newTime;
            //}

            return newTime >= 0.0 ? newTime : m_State.time;
        }
        internal double GetSnappedTimeAtMousePosition(Vector2 mousePos, double frameRate)
        {
            return SnapToFrameIfRequired(ScreenSpacePixelToTimeAreaTime(mousePos.x), frameRate);
            static double SnapToFrameIfRequired(double currentTime, double frameRate)
            {
                return TimeUtilityReflect.RoundToFrame(currentTime, frameRate);
            }
        }
        float ScreenSpacePixelToTimeAreaTime(float p)
        {
            // transform into track space by offsetting the pixel by the screen-space offset of the time area
            p -= TimeAreaRect.x;
            return TrackSpacePixelToTimeAreaTime(p);
        }
        float TrackSpacePixelToTimeAreaTime(float p)
        {
            p -= m_TimeArea.translation.x;

            if (m_TimeArea.scale.x > 0.0f)
                return p / m_TimeArea.scale.x;

            return p;
        }
        internal void SetTimeAreaShownRange(float min, float max)
        {
            m_TimeArea.SetShownHRange(min, max);
            TimeAreaChanged();
        }
        internal void TimeAreaChanged()
        {
            if (m_State.isClipSelected)
            {
                m_State.timeAreaShowRange = new Vector2(m_TimeArea.shownArea.x, m_TimeArea.shownArea.xMax);
            }

            //if (editSequence.asset != null)
            //{
            //    editSequence.viewModel.timeAreaShownRange = new Vector2(m_TimeArea.shownArea.x, m_TimeArea.shownArea.xMax);
            //}
        }
        internal void EnsurePlayHeadIsVisible(double time)
        {
            double minDisplayedTime = PixelToTime(TimeAreaRect.xMin);
            double maxDisplayedTime = PixelToTime(TimeAreaRect.xMax);

            double currentTime = time;
            if (currentTime >= minDisplayedTime && currentTime <= maxDisplayedTime)
                return;

            float displayedTimeRange = (float)(maxDisplayedTime - minDisplayedTime);
            float minimumTimeToDisplay = (float)currentTime - displayedTimeRange / 2.0f;
            float maximumTimeToDisplay = (float)currentTime + displayedTimeRange / 2.0f;
            SetTimeAreaShownRange(minimumTimeToDisplay, maximumTimeToDisplay);
        }

        internal void ReCalculateTimeAreaRange() 
        {
            if (m_EventGroupTrackList.IsNullOrEmpty())
                return;
            timeAreaShownRange = new Vector2(-1, m_State.duration);
            float lastEndTime = 0;
            foreach (AniEventGroupTrack groupTrack in m_EventGroupTrackList) 
            {
                if (groupTrack.ChildEventTracks.IsNullOrEmpty())
                    continue;

                float endTime = groupTrack.ChildEventTracks.Max(childTrack => childTrack.endTime);
                lastEndTime = endTime > lastEndTime ? endTime : lastEndTime;
            }
            if (lastEndTime > State.duration)
                State.duration = lastEndTime;
                //timeAreaShownRange = new Vector2(-1, lastEndTime);
        }
    }
}