using UnityEngine;
using System;

namespace AniEventTool.Editor
{
    using UnityEditor;
    using UnityEngine.Events;
    using ContextMenuItem = CustomEditorUtil.ContextMenuItem;
    public class EditorAniEventUtil
    {
        public static AniEventBase CreateEventInstance(Type eventType)
        {
            if (eventType == null)
                return null;

            return Activator.CreateInstance(eventType) as AniEventBase;
        }

        public static bool CreateEventContextItem(Type eventType, int priority, GenericMenu.MenuFunction menuItemCallback, out ContextMenuItem menuItem)
        {
            menuItem = new ContextMenuItem();

            if (eventType == null)
                return false;

            string eventTypeName = eventType.Name.RemoveAllOccurrences("PR_");
            string menuName = $"Add {eventTypeName}".SplitCamelCase();
            menuItem = CustomEditorUtil.CreateContextMenuItem(menuName, priority, menuItemCallback, IsContextMenuItemEnabled(eventType));

#if USE_CHJ_SOUND
            if (aniEventType == AniEventType.SOUND && AniEventToolWindow.Instance.IsSoundTableLoaded == false)
                menuItem.isEnabled = false;
            else
#endif
            return true;
        }
        private static bool IsContextMenuItemEnabled(Type eventType)
        {
            if (eventType == typeof(AniSpeedEvent)
                || eventType == typeof(ProjectileEvent))
                return false;

            return true;
        }
    }

}
