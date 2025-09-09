namespace AniEventTool.Editor
{
    using UnityEditor;
    using UnityEngine;

    public class Editor_EventTrackInspector : Editor
    {
        GUIStyle _style_Bold = null;
        protected GUIStyle style_Bold 
        {
            get 
            {
                if (_style_Bold == null)
                {
                    _style_Bold = new GUIStyle(GUI.skin.label);
                    _style_Bold.fontStyle = FontStyle.Bold;
                    _style_Bold.normal.textColor = Color.white;
                }
                return _style_Bold;
            } 
        }
        public override void OnInspectorGUI ()
        {
            if (AniEventToolPreferences.settings.showSaveOnInspector)
            {
                if (GUILayout.Button("Save"))
                {
                    AniEventToolWindow.Instance.Editor_SaveEventsToJSON();
                }
            }
        }

        protected virtual void OnModified()
        {
            AniEventToolWindow.Instance.ReDrawGUI();
            AniEventToolWindow.Instance.Repaint();
        }

    }
}