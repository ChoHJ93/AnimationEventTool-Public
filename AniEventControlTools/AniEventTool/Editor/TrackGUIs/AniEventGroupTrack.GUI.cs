using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    using System;
    using UnityEditor;
    using ContextMenuItem = CustomEditorUtil.ContextMenuItem;

    internal class AniEventGroupTrackGUI : EventTrackGUI<AniEventGroupTrack>
    {
        AniEventGroupTrack eventGroupTrack = null;
        public AniEventGroupTrack EventGroupTrack => eventGroupTrack;
        public bool foldoutState
        {
            get { return eventGroupTrack?.foldoutState ?? true; }
            set { if (eventGroupTrack != null) eventGroupTrack.foldoutState = value; }
        }
        public override void Init(AniEventTrackBase aniEventTrack)
        {
            base.Init(aniEventTrack);

            foldoutState = true;
            if (aniEventTrack is AniEventGroupTrack groupTrack)
            {
                this.eventTrack = groupTrack;
                eventGroupTrack = groupTrack;
            }
            else
                throw new InvalidOperationException($"Track GUI Type({typeof(AniEventGroupTrackGUI).Name}) is not Match with Event Track{aniEventTrack.GetType().Name}");
        }

        public override void Release()
        {
            base.Release();
            eventGroupTrack = null;
        }
        public override void DrawHeader(Rect controlRect, Rect treeViewRect)
        {
            base.DrawHeader(controlRect, treeViewRect);

            Rect rect = new Rect(headerBound);
            rect.y = controlRect.y;


            const float buttonSize = WindowConstants.eventHeaderButtonSize;
            const float padding = WindowConstants.defaultPadding;

            //base.Draw(rect, indent, eventTrack, index);
            foldoutState = EditorGUI.Foldout(rect, foldoutState, GUIContent.none);
            rect.x += WindowConstants.kBaseIndent;
            DrawHeaderBG(rect, WindowConstants.kBaseIndent);

            float allButtonWidth = (buttonSize + padding) * 4;//button count + 1
            DrawTrackLabel(rect, allButtonWidth);

            Rect buttonRect = new Rect(rect.xMax - allButtonWidth - buttonSize, rect.y + ((rect.height - buttonSize) * 0.5f), buttonSize, buttonSize);
            DrawLockButton(buttonRect, buttonSize + padding, 1);
            DrawEnableButton(buttonRect, buttonSize + padding, 2);
            DrawAddNewSubEventButton(buttonRect, buttonSize + padding, 3);
        }
        protected override void DrawContent(Rect contentRect)
        {
            base.DrawContent(contentRect);
        }
        protected override void DrawHeaderBG(Rect rect, float indent)
        {
            base.DrawHeaderBG(rect, indent);
        }
        public override void ReDraw()
        {
            base.ReDraw();
        }

        public override void SetContentBounds(Rect bounds)
        {
            base.SetContentBounds(bounds);
        }

        void DrawAddNewSubEventButton(Rect buttonRect, float widthWithPadding, int order)
        {
            buttonRect.x += (widthWithPadding) * order;

            if (GUI.Button(buttonRect, EditorGUIUtility.IconContent("CreateAddNew"), CustomGUIStyles.eventAddButton))
            {
                Type[] allEventTypes = CommonUtil.GetLeafDerivedTypes(typeof(AniEventBase));
                if (allEventTypes.IsNullOrEmpty())
                    return;
                List<ContextMenuItem> menuItems = new List<ContextMenuItem>();
                for (int i = 0; i < allEventTypes.Length; i++)
                {
                    Type type = allEventTypes[i];
                    GenericMenu.MenuFunction callback = () =>
                    {
                        AniEventBase childEvent = EditorAniEventUtil.CreateEventInstance(type);
                        AniEventGroup parentGroup = eventGroupTrack.eventGroup;
                        AniEventToolWindow.Instance.AddChildEvent(childEvent, parentGroup);
                    };
                    if (EditorAniEventUtil.CreateEventContextItem(type, i, callback, out ContextMenuItem menuItem))
                        menuItems.Add(menuItem);
                }
                //sort menu items by name
                menuItems.Sort((a, b) => string.Compare(a.menuName, b.menuName));
                menuItems.Sort((a, b) => b.isEnabled.CompareTo(a.isEnabled));
                CustomEditorUtil.ShowContextMenu(menuItems.ToArray());
            }
        }
    }
}