using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    using System;
    using AniEventTool.Editor;
    using UnityEditor;

    public class EventTrackInputControl
    {
        private static EventTrackInputControl instance;
        public static EventTrackInputControl Instance
        {
            get
            {
                if (instance == null)
                    instance = new EventTrackInputControl();
                return instance;
            }
        }

        public static void Clear()
        {
            if (instance != null)
            {
                instance = null;
            }
        }

        readonly struct EventContentPoint
        {
            public readonly AniEventToolWindow Window;
            public readonly WindowState State;

            public readonly float Time;
            public readonly float Frame;
            public readonly float PosY;
            public readonly float ScrollPosY;
            public readonly Vector2 StartPixel;

            public EventContentPoint(AniEventToolWindow view, Vector2 mousePos)
            {
                Window = view;
                State = view.State;
                Time = view.PixelToTime(mousePos.x);
                Frame = view.PixelToFrame(mousePos.x);
                StartPixel = mousePos;
                PosY = mousePos.y;
                ScrollPosY = view.ScrollPos.y;
            }
            public Vector2 ToPixel()
            {
                return new Vector2(Window.TimeArea.TimeToPixel(Time, Window.TimeAreaRect), PosY - (Window.ScrollPos.y - ScrollPosY));
            }
        }

        private static readonly int HeaderControllerHash = "AniEventTool-TrackHeader-Input".GetHashCode();
        private static readonly int ContentControllerHash = "AniEventTool-TrackContent-Input".GetHashCode();
        private static readonly string EditingControlName = "AniEventTool-Editing";
        AniEventToolWindow Window => AniEventToolWindow.Instance;
        Rect m_Rect => Window.EventTrackRect;
        Vector2 RelativeMousePosition(Event evt, Rect areaRect) { return evt.mousePosition + Window.ScrollPos - areaRect.position; }
        int m_HeaderCtrlID = 0;
        int m_ContentCtrlID = 0;

        bool m_IsDragging = false;
        EventTrackGUIBase m_SelectedTrackGUI = null;
        EventContentPoint m_ContentPoint;
        Vector2 m_EndPixel;

        internal void SelectTrackGUI(EventTrackGUIBase targetTrack)
        {
            if (m_SelectedTrackGUI != null && m_SelectedTrackGUI.isSelected)
                m_SelectedTrackGUI.isSelected = false;

            m_SelectedTrackGUI = targetTrack;
            m_SelectedTrackGUI.OnHeaderClicked();
            m_SelectedTrackGUI.isSelected = true;
        }
        internal void DeselectTrackGUI()
        {
            if (m_SelectedTrackGUI != null)
            {
                m_SelectedTrackGUI.isSelected = false;

                if (Selection.activeObject != null && Selection.activeObject.Equals((ScriptableObject)m_SelectedTrackGUI.EventTrack)) 
                {
                    Selection.activeObject = null;
                }
            }
            m_SelectedTrackGUI = null;
            
        }
        internal void OnGUI(EventType rawType, Vector2 mousePosition)
        {
            if (Window == null || Window.IsEventExist == false)
                return;
            OnWorldInputEvent(rawType);

            if (m_Rect.Contains(mousePosition) == false)
                return;
            if (m_HeaderCtrlID == 0)
                m_HeaderCtrlID = GUIUtility.GetControlID(HeaderControllerHash, FocusType.Passive, Window.EventLisViewRect);

            if (m_ContentCtrlID == 0)
                m_ContentCtrlID = GUIUtility.GetControlID(ContentControllerHash, FocusType.Passive, Window.EventTrackViewRect);

            Event evt = Event.current;

            if (Window.EventLisViewRect.Contains(mousePosition))
                Input_HeaderRect(rawType, evt);

            if (Window.EventTrackViewRect.Contains(mousePosition))
                Input_ContentRect(rawType, evt);
            //else if (m_IsDragging)
            //{
            //    m_IsDragging = false;
            //    if (m_SelectedTrackGUI != null)
            //        m_SelectedTrackGUI.isSelected = false;
            //    m_SelectedTrackGUI = null;
            //}
        }
        private void OnWorldInputEvent(EventType rawType)
        {
            if (Window.IsFocused == false)
                return;

            Event evt = Event.current;
            if (rawType == EventType.MouseDown || evt.type == EventType.MouseDown)
            {

            }
            else if (rawType == EventType.MouseDrag || evt.type == EventType.MouseDrag)
            {
            }
            else if (rawType == EventType.MouseUp || evt.type == EventType.MouseUp)
            {
                if (m_IsDragging)
                {
                    Window.ReCalculateTimeAreaRange();
                }
            }
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.S && evt.control)
            {
                Window.Editor_SaveEventsToJSON();
            }

            //���� Ű
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Return)
            {
            }
        }
        #region HeaderRect
        internal void Input_HeaderRect(EventType rawType, Event evt)
        {
            if (rawType == EventType.MouseDown || evt.type == EventType.MouseDown)
            {
                //if (m_SelectedTrackGUI != null)
                //    m_SelectedTrackGUI.isSelected = false;

                if (!CanStartClickHeaderRect(evt))
                {
                    Selection.activeObject = null;
                    return;
                }

                m_SelectedTrackGUI.OnHeaderClicked();
                m_SelectedTrackGUI.isSelected = true;
                //Selection.activeObject = AniEventFactory.GetEditObj(m_SelectedTrackGUI.GetAniEventData);
                GUIUtility.hotControl = m_HeaderCtrlID;
                evt.Use();
                return;
            }

            if (evt.keyCode == KeyCode.Delete && m_SelectedTrackGUI != null)
            {
                Window.DeleteEvent(m_SelectedTrackGUI);
                m_SelectedTrackGUI = null;

                GUIUtility.hotControl = 0;
                evt.Use();

                return;
            }
        }
        private bool CanStartClickHeaderRect(Event evt)
        {
            if (evt.button != 0 || evt.alt)
                return false;

            return PickTrackHeaderGUI(RelativeMousePosition(evt, Window.EventLisViewRect));
        }
        private bool PickTrackHeaderGUI(Vector2 mousePos)
        {
            EventTrackGUIBase selectedGUI = PickTrackHeaderGUI(new Rect(mousePos.x, mousePos.y, 1, 1));
            if (m_SelectedTrackGUI != null && !m_SelectedTrackGUI.Equals(selectedGUI))
                m_SelectedTrackGUI.isSelected = false;
            m_SelectedTrackGUI = selectedGUI;
            return m_SelectedTrackGUI != null;
        }

        private EventTrackGUIBase PickTrackHeaderGUI(Rect area)
        {
            EventTrackGUIBase trackAtArea = null;
            foreach (EventTrackGUIBase eventTrackGUI in Window.GetTrackGUIList)
            {
                if (eventTrackGUI.HeaderBound.Overlaps(area))
                {
                    trackAtArea = eventTrackGUI;
                    break;
                }
            }
            return trackAtArea;
        }

        #endregion

        #region ContentRect
        private void Input_ContentRect(EventType rawType, Event evt)
        {
            if (rawType == EventType.MouseDown || evt.type == EventType.MouseDown)
            {
                m_IsDragging = false;
                //if (m_SelectedTrackGUI != null)
                //    m_SelectedTrackGUI.isSelected = false;

                if (!CanStartClickContentRect(evt))
                {
                    Selection.activeObject = null;
                    return;
                }


                m_SelectedTrackGUI.OnContentClicked();
                m_SelectedTrackGUI.isSelected = true;
                //AniEventToolDataCache.GetCachedObject(m_SelectedTrackGUI.EventTrack).ObjectInstance;

                Window.Pause();
                m_ContentPoint = new EventContentPoint(Window, evt.mousePosition);
                m_EndPixel = evt.mousePosition;
                GUIUtility.hotControl = m_ContentCtrlID;
                evt.Use();
                return;
            }

            switch (evt.GetTypeForControl(m_ContentCtrlID))
            {
                case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl != m_ContentCtrlID || m_SelectedTrackGUI == null)
                            return;

                        m_EndPixel = evt.mousePosition;
                        DragEventTrack(evt);

                        evt.Use();
                        return;
                    }
                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl != m_ContentCtrlID)
                            return;
                        if (m_IsDragging)
                        {
                            m_IsDragging = false;
                            if (m_SelectedTrackGUI != null)
                                m_SelectedTrackGUI.OnContentReleased();
                        }
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        return;
                    }
            }

            //if (GUIUtility.hotControl == m_ContentCtrlID)
            //{
            //    if (evt.type == EventType.Repaint)
            //    {
            //        Rect rect = CurrentRectangle();
            //        if (IsValidRect(rect))
            //        {
            //            using (new GUIViewportScope(m_Rect))
            //            {
            //                DrawRectangle(rect);
            //            }
            //        }
            //    }
            //}
        }

        private bool PickTrackContent(Vector2 mousePos)
        {
            EventTrackGUIBase selectedGUI = PickTrackContent(new Rect(mousePos.x, mousePos.y, 1, 1));
            if (m_SelectedTrackGUI != null && !m_SelectedTrackGUI.Equals(selectedGUI))
                m_SelectedTrackGUI.isSelected = false;
            m_SelectedTrackGUI = selectedGUI;
            return m_SelectedTrackGUI != null;
        }
        private EventTrackGUIBase PickTrackContent(Rect area)
        {
            EventTrackGUIBase trackAtArea = null;
            foreach (EventTrackGUIBase eventTrack in Window.GetTrackGUIList)
            {
                //if (eventTrack is not EventTrackGroup)
                if (eventTrack.ContentBound.Overlaps(area))
                {
                    trackAtArea = eventTrack;
                    break;
                }
            }
            return trackAtArea;
        }

        private bool CanStartClickContentRect(Event evt)
        {
            if (evt.button != 0 || evt.alt)
                return false;

            return PickTrackContent(RelativeMousePosition(evt, Window.EventTrackViewRect));
        }
        bool CanClearSelection(Event evt)
        {
            return !evt.control && !evt.command && !evt.shift;
        }
        static void DrawRectangle(Rect rect)
        {
            EditorStyles.selectionRect.Draw(rect, GUIContent.none, false, false, false, false);
        }

        static bool IsValidRect(Rect rect)
        {
            return rect.width >= 1.0f && rect.height >= 1.0f;
        }

        Rect CurrentRectangle()
        {
            var startPixel = m_ContentPoint.ToPixel();
            return Rect.MinMaxRect(
                Math.Min(startPixel.x, m_EndPixel.x),
                Math.Min(startPixel.y, m_EndPixel.y),
                Math.Max(startPixel.x, m_EndPixel.x),
                Math.Max(startPixel.y, m_EndPixel.y));
        }
        #endregion

        #region Functions - Control TrackEvent by Mouse Input
        private void DragEventTrack(Event evt)
        {
            const float hDeadZone = 5.0f;
            //const float vDeadZone = 5.0f;

            //bool vDone = m_VerticalMovementDone || Math.Abs(evt.mousePosition.y - m_MouseDownPosition.y) > vDeadZone;
            bool hDone = m_IsDragging == true || Math.Abs(evt.mousePosition.x - m_ContentPoint.StartPixel.x) > hDeadZone;
            //Debug.Log($"evt.mousePosition.x : {evt.mousePosition.x} - Window.EventTrackViewRect.x :{Window.EventTrackViewRect.x} = {evt.mousePosition.x - Window.EventTrackViewRect.x}" +
            //    $"\nevt.mousePosition.x : {evt.mousePosition.x} - m_ContentPoint.StartPixel.x:{m_ContentPoint.StartPixel.x} = {evt.mousePosition.x - m_ContentPoint.StartPixel.x}");

            if (!hDone)
            {
                m_IsDragging = true;

                const string undoName = "AniEventTrack-MoveHandle";
                //CustomEditorUtil.RegisterUndo(undoName, m_SelectedTrackGUI);
            }

            if (m_IsDragging)
            {
                float hMovedTime = Window.PixelDeltaToDeltaTime(evt.delta.x);
                m_SelectedTrackGUI.EventTrack.MoveTime(hMovedTime);
                //Window.m_Window.Repaint();
            }
        }

        #endregion
    }
}