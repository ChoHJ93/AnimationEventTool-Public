using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    using System;
    using UnityEditor;
    using AniEventTool.Editor;
    using Object = UnityEngine.Object;

    [System.Serializable]
    public abstract class EventTrackGUIBase 
    {
        protected AniEventTrackBase eventTrack;
        public AniEventTrackBase EventTrack { get { return eventTrack; } }
        protected Rect controlRect;
        protected Rect headerBound;
        protected Rect contentBound;

        protected bool _isSelected = false;
        public bool isSelected //{ get; set; }
        {
            get { return _isSelected; }
            set
            {
                if (!value && isNameEditing)
                {
                    OnEditTrackName(false);
                }
                _isSelected = value;
            }
        }
        public bool isNameEditing { get; set; }
        protected bool focusEditingText = false;
        protected string editingText = string.Empty;

        #region GetProperties
        public Rect HeaderBound => headerBound;
        public Rect ContentBound => contentBound;
        #endregion

        public virtual void Init(AniEventTrackBase aniEventTrack)
        {
            this.eventTrack = aniEventTrack;
            isSelected = false;
        }
        public virtual void Release()
        {
            eventTrack = null;
        }

        ~EventTrackGUIBase()
        {
            //Debug.Log($"{GetType().Name} Destroying Instance");
        }

        public virtual void DrawHeader(Rect controlRect, Rect treeViewRect)
        {
            this.controlRect = controlRect;
            headerBound = new Rect(controlRect);
            headerBound.width = treeViewRect.width;
            headerBound.y += treeViewRect.y;


            Rect rect = new Rect(headerBound);
            rect.y = controlRect.y;

            const float buttonSize = WindowConstants.eventHeaderButtonSize;
            const float padding = WindowConstants.defaultPadding;

            rect.x += WindowConstants.kSubIndent;
            DrawHeaderBG(rect, WindowConstants.kSubIndent);
            rect.width -= WindowConstants.kBaseIndent;
            float allButtonWidth = (buttonSize + padding) * 4;//button count + 1
            DrawTrackLabel(rect, allButtonWidth);
            Rect buttonRect = new Rect(rect.xMax - allButtonWidth - buttonSize, rect.y + ((rect.height - buttonSize) * 0.5f), buttonSize, buttonSize);
            DrawLockButton(buttonRect, buttonSize + padding, 1);
            DrawEnableButton(buttonRect, buttonSize + padding, 2);
        }
        public virtual void DrawContent(float rectX, float contentViewWidth)
        {
            Rect rect = new Rect(controlRect);
            rect.x = rectX;
            rect.width = contentViewWidth;

            DrawContent(rect);
        }
        protected virtual void DrawContent(Rect contentRect)
        {
            EditorGUI.DrawRect(contentRect, isSelected ? CustomGUIStyles.colorSelectedContentBackground : CustomGUIStyles.colorContentBackground);
        }

        protected virtual void DrawHeaderBG(Rect rect, float indent)
        {
            rect.y += 3;
            rect.width -= indent;
            rect.height = WindowConstants.eventItemHeight - 4;

            EditorGUI.DrawRect(rect, isSelected ? CustomGUIStyles.hoverEventBackground : CustomGUIStyles.defaultEventBackground);
            GUI.Label(new Rect(rect.x, rect.y, CustomGUIStyles.eventSwatchStyle.fixedWidth, rect.height), GUIContent.none, CustomGUIStyles.eventSwatchStyle);
        }
        protected virtual void DrawTrackLabel(Rect rect, float allButtonWitdh)
        {
            Rect labelRect = new Rect(rect);
            labelRect.xMin += 10;
            labelRect.y += 6f;
            labelRect.width -= allButtonWitdh;
            labelRect.width = Math.Max(labelRect.width, 30);
            labelRect.height = WindowConstants.eventLabelHeight;

            if (isNameEditing)
            {
                string controlName = "AniEventTool-EditEventName";
                GUI.SetNextControlName(controlName);

                editingText = EditorGUI.TextField(labelRect, editingText);
                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.type == EventType.KeyDown
                        && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Escape)
                        && GUI.GetNameOfFocusedControl() == controlName)
                    {
                        OnEditTrackName(false);
                    }
                }
                if (focusEditingText)
                {
                    EditorGUI.FocusTextInControl(controlName);
                    focusEditingText = false;
                }
            }
            else
            {
                EditorGUI.LabelField(labelRect, eventTrack.eventName);
            }
        }
        protected void DrawEnableButton(Rect buttonRect, float widthWithPadding, int order)
        {
            Color color = GUI.color;

            buttonRect.x += (widthWithPadding) * order;

            EditorGUI.BeginChangeCheck();
            bool isEventEnable = eventTrack.isEnable;
            GUI.color = isEventEnable ? Color.white : Color.black;
            GUIContent icon = isEventEnable ? CustomGUIStyles.eventEnableState : CustomGUIStyles.eventDisableState;
            isEventEnable = GUI.Toggle(buttonRect, eventTrack.isEnable, icon, GUIStyle.none);
            if (EditorGUI.EndChangeCheck())
            {
                eventTrack.isEnable = isEventEnable;
            }
            GUI.color = color;
        }
        protected void DrawLockButton(Rect buttonRect, float widthWithPadding, int order)
        {
            //Color color = GUI.color;

            buttonRect.x += (widthWithPadding) * order;

            EditorGUI.BeginChangeCheck();
            bool isEventLocked = GUI.Toggle(buttonRect, eventTrack.isLocked, GUIContent.none, CustomGUIStyles.eventLockButton);
            if (EditorGUI.EndChangeCheck())
            {
                eventTrack.isLocked = isEventLocked;
            }
            //GUI.color = color;
        }
        public virtual void ReDraw()
        {

        }

        public virtual void SetContentBounds(Rect bounds)
        {
            contentBound = bounds;
        }

        public virtual void OnHeaderClicked()
        {
            Selection.activeObject = eventTrack as ScriptableObject;
            if (isSelected && isNameEditing == false)
            {
                OnEditTrackName(true);
            }
        }
        public virtual void OnContentClicked()
        {
            Selection.activeObject = eventTrack.GetCachedObject?.ObjectInstance;
        }
        public virtual void OnContentReleased()
        {

        }
        protected virtual void OnEditTrackName(bool isEditing)
        {
            if (eventTrack == null)
                return;

            isNameEditing = isEditing;
            if (isEditing)
            {
                editingText = eventTrack.eventName;
                focusEditingText = true;
            }
            else
            {
                eventTrack.eventName = editingText;
                editingText = string.Empty;
            }
        }
    }

    [System.Serializable]
    public class EventTrackGUI<T> : EventTrackGUIBase where T : AniEventTrackBase
    {
        public override void Init(AniEventTrackBase aniEventTrack)
        {
            if (aniEventTrack is not T)
                throw new InvalidOperationException($"Track GUI Type({typeof(EventTrackGUI<T>).Name}) is not Match with Event Track{aniEventTrack.GetType().Name}");

            base.Init(aniEventTrack);
        }

    }
}