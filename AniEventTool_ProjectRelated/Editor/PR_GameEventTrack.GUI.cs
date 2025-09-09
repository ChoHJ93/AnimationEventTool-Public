using UnityEngine;
using UnityEditor;


namespace AniEventTool.Editor.ProjectRelated
{
    using AniEventTool.ProjectRelated;
    public class PR_GameEventTrackGUI : EventTrackGUI<PR_GameEventTrack>
    {
        private PR_GameEventTrack _track;
        private PR_GameEventTrack m_Track
        {
            get
            {
                if (_track == null)
                    _track = EventTrack as PR_GameEventTrack;
                return _track;
            }
        }

        public override void Init(AniEventTrackBase aniEventTrack)
        {
            base.Init(aniEventTrack);
        }

        public override void SetContentBounds(Rect bounds)
        {
            base.SetContentBounds(bounds);
        }
        public override void DrawContent(float rectX, float contentViewWidth)
        {
            if (contentViewWidth < WindowConstants.trackBindingPadding)
                return;

            if (m_Track.endTime <= 0)
                contentViewWidth = controlRect.height;

            base.DrawContent(rectX, contentViewWidth);
        }
        protected override void DrawContent(Rect contentRect)
        {
            eGameEventType eventType = m_Track.gameEventType;
            switch (eventType)
            {
                case eGameEventType.None:
                    DrawContent_NoneType(contentRect);
                    break;
                default:
                    DrawContent_SimpleControl(contentRect);
                    break;
            }
        }

        private void DrawContent_NoneType(Rect contentRect)
        {
            Rect iconRect = new Rect(contentRect);
            iconRect.width = iconRect.height;
            iconRect.size *= 0.7f;
            iconRect.x = contentRect.x - iconRect.width * 0.5f;
            iconRect.y += iconRect.height * 0.25f;

            GUIContent icon = CustomGUIStyles.eventKeyIcon_Gray;
            if (icon.image != null)
            {
                Color iconColor = isSelected ? Color.white : Color.gray;
                GUI.DrawTexture(iconRect, icon.image, ScaleMode.ScaleToFit, true, 1, iconColor, 0, 0);
            }
            else
                base.DrawContent(contentRect);
        }

        private void DrawContent_SimpleControl(Rect contentRect)
        {
            Rect iconRect = new Rect(contentRect);
            iconRect.width = iconRect.height;
            iconRect.size *= 0.7f;
            iconRect.x = contentRect.x - iconRect.width * 0.5f;
            iconRect.y += iconRect.height * 0.25f;

            Rect labelRect = new Rect(contentRect);
            labelRect.x += iconRect.width * 0.5f;
            labelRect.y += WindowConstants.trackBindingPadding * 0.6f;
            labelRect.height -= 10;

            bool drawDuration = false;
            GUIContent icon = GetIcon(m_Track.gameEventType, eTargetType.None, out drawDuration);
            if (icon?.image != null)
            {
                Color iconColor = isSelected ? Color.white : Color.gray;
                GUI.DrawTexture(iconRect, icon.image, ScaleMode.ScaleToFit, true, 1, iconColor, 0, 0);
            }

            if (drawDuration)
                base.DrawContent(contentRect);


            DrawLabel(labelRect);
        }

        private void DrawLabel(Rect labelRect)
        {
            if (!TryGetLabel(out GUIContent label, out float labelWidth, out Color labelColor))
                return;

            eGameEventType eventType = m_Track.gameEventType;
            Color guiColor = GUI.color;
            GUI.color = labelColor;
            labelRect.width += labelWidth;
            EditorGUI.LabelField(labelRect, label);
            GUI.color = guiColor;
        }

        private GUIContent GetIcon(eGameEventType gameEventType, eTargetType targetType, out bool drawDuration)
        {
            drawDuration = false;
            switch (gameEventType)
            {
                case eGameEventType.NextSkill:
                    return CustomGUIStyles.eventKeyIcon_Yellow_Circle;
                case eGameEventType.EnableMove:
                    return CustomGUIStyles.eventKeyIcon_NoneCircle;
                default:
                    return CustomGUIStyles.eventKeyIcon_Gray;

            }
        }

        private bool TryGetLabel(out GUIContent label, out float labelWidth, out Color labelColor)
        {
            eGameEventType eventType = m_Track.gameEventType;
            bool drawLabel = eventType == eGameEventType.NextSkill
                             || eventType == eGameEventType.EnableMove;
            label = new GUIContent(string.Empty);
            labelWidth = 0;
            labelColor = Color.white;

            switch (eventType)
            {
                case eGameEventType.NextSkill:
                    label.text = "���� ��ų";
                    labelWidth = 36;
                    break;
                case eGameEventType.EnableMove:
                    label.text = "�̵� ����";
                    label.text += m_Track.cancelSkill ? " (��ų ���)" : "";
                    labelWidth = 85;
                    break;
            }

            return drawLabel;
        }

        public override void OnContentClicked()
        {
            base.OnHeaderClicked();
        }

        public override void OnContentReleased()
        {
            base.OnContentReleased();
            AniEventToolWindow.Instance.OnMoveEventEdited();
        }
    }
}