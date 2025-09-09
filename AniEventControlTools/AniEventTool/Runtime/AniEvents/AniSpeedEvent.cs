using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AniEventTool
{
    [System.Serializable]
    public class AniSpeedEvent : AniEventBase
    {
        public float speed;
        public AniSpeedEvent()
        {
            speed = 1;
        }
        public override bool IsValidEventData => speed > 0;
        public override bool ReadPropertiesFromJson(ref JsonObject jsonData)
        {
            if(!base.ReadPropertiesFromJson(ref jsonData))
                return false;

            jsonData.GetField(out speed, "Speed", 0f);
            return true;
        }

        public override void InitOnRuntime(AniEventControllerBase ownerController)
        {
            base.InitOnRuntime(ownerController);
        }
        public override void PlayEvent()
        {
            base.PlayEvent();
            eventController.AniSpeed = speed;
        }
        public override void StopEvent()
        {
            base.StopEvent();
            eventController.AniSpeed = eventController.StateOriginSpeed;
        }


#if UNITY_EDITOR
        public override void WritePropertiesToJson(ref JsonObject jsonData)
        {
            base.WritePropertiesToJson(ref jsonData);

            jsonData.AddField("Speed", speed);
        }
#endif
    }
}
