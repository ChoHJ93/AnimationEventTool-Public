using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AniEventTool
{
    [System.Serializable]
    public class RangeInfo //: PropertyAttribute
    {
        public eRangeType rangeType;
        public Vector3 center;
        public Vector3 rotation;
        public Vector3 size;    //for box type
        public float radius;    //for sphere type / ray type (distance)
        public RangeInfo()
        {
            rangeType = eRangeType.Ray;
            center = Vector3.zero;
            rotation = Vector3.zero;
            size = Vector3.one;
            radius = 1;
        }

        public bool isValid => size.magnitude > 0 || radius > 0;


#if UNITY_EDITOR
#endif
    }
    public class HitEvent : AniEventBase
    {
        public int layerMask;
        public eTargetType targetType;
        /// <summary>
        /// 0 == no limit
        /// </summary>
        public int targetCount;
        /// <summary>
        /// 0 == hit on one frame
        /// </summary>
        public int hitCount;     //for multi hit, Default = 1
        public float durationPerHit;
        public float lifeTime;   //for multi hit
        public bool stiffEffect;

        public bool attach;
        public bool followParentRot;

        public List<RangeInfo> ranges;

        public HitEvent()
        {
            layerMask = 0;
            targetType = eTargetType.None;
            targetCount = 0;
            hitCount = 1;
            durationPerHit = 0;
            lifeTime = 0;
            stiffEffect = false;
            attach = true;
            followParentRot = true;
            ranges = new List<RangeInfo>();
        }

        public override bool IsValidEventData => targetType != eTargetType.None && ranges.IsNullOrEmpty() == false && ranges.Exists(info => info.isValid);

        #region Ingame-Only
        public override void InitOnRuntime(AniEventControllerBase ownerController)
        {
            base.InitOnRuntime(ownerController);
        }
        public override void PlayEvent()
        {
            base.PlayEvent();

            eventController.HitTargets(this);
        }
        #endregion

        public override bool ReadPropertiesFromJson(ref JsonObject jsonData)
        {
            if (!base.ReadPropertiesFromJson(ref jsonData))
                return false;

            jsonData.GetField(out layerMask, "LayerMask", 0);
            jsonData.GetField(out int nTargetType, "TargetType", 0);
            targetType = (eTargetType)nTargetType;
            jsonData.GetField(out targetCount, "TargetCount", 0);
            jsonData.GetField(out durationPerHit, "DurationPerHit", 0f);
            jsonData.GetField(out attach, "Attach", false);
            jsonData.GetField(out followParentRot, "FollowParentRot", false);
            jsonData.GetField(out lifeTime, "LifeTime", 0f);
            jsonData.GetField(out stiffEffect, "StiffEffect", false);
            jsonData.GetField(out hitCount, "HitCount", 0);

            JsonObject rangeDatas = jsonData.GetField("Range");
            for (int i = 0; i < rangeDatas.Count; i++)
            {
                JsonObject rangeData = rangeDatas.list[i];
                RangeInfo rangeInfo = new RangeInfo();

                rangeData.GetField(out int nRangeType, "RangeType", 0);
                rangeInfo.rangeType = (eRangeType)nRangeType;
                string[] strAdded = rangeData.GetField("Center").str.Split('|');
                rangeInfo.center = new Vector3(float.Parse(strAdded[0]), float.Parse(strAdded[1]), float.Parse(strAdded[2]));
                strAdded = rangeData.GetField("Rotation").str.Split('|');
                rangeInfo.rotation = new Vector3(float.Parse(strAdded[0]), float.Parse(strAdded[1]), float.Parse(strAdded[2]));
                strAdded = rangeData.GetField("Size").str.Split('|');
                rangeInfo.size = new Vector3(float.Parse(strAdded[0]), float.Parse(strAdded[1]), float.Parse(strAdded[2]));
                rangeData.GetField(out rangeInfo.radius, "Radius", 0f);
                ranges.Add(rangeInfo);
            }

            return true;
        }
#if UNITY_EDITOR
        public override void WritePropertiesToJson(ref JsonObject jsonData)
        {
            base.WritePropertiesToJson(ref jsonData);

            jsonData.AddField("LayerMask", layerMask);
            jsonData.AddField("TargetType", (int)targetType);
            jsonData.AddField("TargetCount", targetCount);
            jsonData.AddField("DurationPerHit", durationPerHit);
            jsonData.AddField("Attach", attach);
            jsonData.AddField("FollowParentRot", followParentRot);
            jsonData.AddField("LifeTime", lifeTime);
            jsonData.AddField("StiffEffect", stiffEffect);
            jsonData.AddField("HitCount", hitCount);

            JsonObject rangeJObj = new JsonObject(JsonObject.Type.ARRAY);
            foreach (RangeInfo rangeInfo in ranges)
            {
                JsonObject rangeObject = new JsonObject(JsonObject.Type.OBJECT);
                rangeObject.AddField("RangeType", (int)rangeInfo.rangeType);
                rangeObject.AddField("Center", $"{rangeInfo.center.x}|{rangeInfo.center.y}|{rangeInfo.center.z}");
                rangeObject.AddField("Rotation", $"{rangeInfo.rotation.x}|{rangeInfo.rotation.y}|{rangeInfo.rotation.z}");
                rangeObject.AddField("Size", $"{rangeInfo.size.x}|{rangeInfo.size.y}|{rangeInfo.size.z}");
                rangeObject.AddField("Radius", rangeInfo.radius);

                rangeJObj.Add(rangeObject);
            }
            jsonData.AddField("Range", rangeJObj);
        }
#endif
    }
}
