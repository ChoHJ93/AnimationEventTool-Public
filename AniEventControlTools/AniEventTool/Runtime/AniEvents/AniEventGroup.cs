using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AniEventTool
{
    using static CommonUtil;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AniEventGroup : AniEventBase
    {
        #region fields - Use in Editor only
        /// <summary>
        /// *Only For Editor
        /// </summary>
        public string eventName = null;
        #endregion


        [SerializeField]
        public List<AniEventBase> aniEvents { get; set; }

#if UNITY_EDITOR

        public void AddChildEvent(AniEventBase evt)
        {
            if(aniEvents.IsNullOrEmpty())
                aniEvents = new List<AniEventBase>();

            if(aniEvents.Contains(evt) == false)
                aniEvents.Add(evt); 
        }
        public void RemoveEvent(AniEventBase evt)
        {
            if(aniEvents.IsNullOrEmpty())
                return;

            if(aniEvents.Contains(evt))
                aniEvents.Remove(evt);
        }
        public int Editor_GetValidEventCount {
            get 
            {
                int result = aniEvents.FindAll(aniEvents => IsValidEventData(aniEvents)).Count;
                return result;
            }}

        
        public List<AniEventBase> GetValidEventList(Type eventType)
        {
            List<AniEventBase> validEventList = new List<AniEventBase>();
            validEventList = aniEvents.FindAll(evt => evt.GetType().Equals(eventType));
            return validEventList;
        }

        public void ClearEvents()
        {
            aniEvents.Clear();

        }
#endif

        public override bool IsValidEventData => aniEvents.IsNullOrEmpty() == false && aniEvents.FindAll(evt => evt.IsValidEventData) != null;
        
    }

}
