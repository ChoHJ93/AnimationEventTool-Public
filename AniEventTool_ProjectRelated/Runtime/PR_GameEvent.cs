using UnityEngine;

namespace AniEventTool.ProjectRelated
{
    /// <summary>
    /// Project related class.
    /// </summary>
    [System.Serializable]
    public class PR_GameEvent : AniEventBase
    {
#if PROJECT_RELATED_SAMPLE
        Unit m_Unit = null;
#endif // PROJECT_RELATED_SAMPLE
        public eGameEventType eventType;
        public bool cancelSkill;

        public PR_GameEvent()
        {
            eventType = eGameEventType.None;
        }

        public override bool IsValidEventData
        {
            get
            {
                switch (eventType)
                {
                    case eGameEventType.NextSkill: return true;
                    case eGameEventType.EnableMove: return true;
                }
                return false;
            }
        }

        public override bool ReadPropertiesFromJson(ref JsonObject jsonData)
        {
            if (jsonData == null || jsonData.Count == 0)
                return false;

            jsonData.GetField(out int nEventType, "EventType", 0);
            eventType = (eGameEventType)nEventType;
            jsonData.RemoveField("EventType");
            if (!base.ReadPropertiesFromJson(ref jsonData))
                return false;

            switch (eventType)
            {
                case eGameEventType.EnableMove:
                    jsonData.GetField(out cancelSkill, "CancelSkill", false);
                    break;
                    //case eGameEventType.EffectData:
                    //    ReadEffectData(jsonData);
                    //    break;
            }

            return true;
        }

        public override void InitOnRuntime(AniEventControllerBase ownerController)
        {
            base.InitOnRuntime(ownerController);
        }
        public override void PlayEvent()
        {
#if PROJECT_RELATED_SAMPLE
            if (m_Unit == null)
                return;
#endif // PROJECT_RELATED_SAMPLE

            base.PlayEvent();
            if (eventType == eGameEventType.NextSkill)
            {
                // Trigger the next skill Evnet
            }
            else if (eventType == eGameEventType.EnableMove)
            {
                // Trigger the enable move Event
#if PROJECT_RELATED_SAMPLE
                m_Unit.UnitMove.SetEnableMove(true, cancelSkill);
#endif // PROJECT_RELATED_SAMPLE
            }
        }
        public override void StopEvent()
        {
#if PROJECT_RELATED_SAMPLE
            if (m_Unit == null)
                return;
#endif // PROJECT_RELATED_SAMPLE

            base.StopEvent();
        }
#if UNITY_EDITOR
        public override void WritePropertiesToJson(ref JsonObject jsonData)
        {
            jsonData.AddField("EventType", (int)eventType);
            base.WritePropertiesToJson(ref jsonData);

            switch (eventType)
            {
                case eGameEventType.EnableMove:
                    jsonData.AddField("CancelSkill", cancelSkill);
                    break;
            }
        }
#endif // UNITY_EDITOR
    }
}
