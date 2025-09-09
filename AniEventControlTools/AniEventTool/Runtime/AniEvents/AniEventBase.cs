using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AniEventTool
{

    [System.Serializable]
    public abstract class AniEventBase
    {
        [SerializeField] public int groupId;
        [SerializeField] public string name;
        [SerializeField] public int index;
        [SerializeField] public float startTime;
        [SerializeField] public float endTime;
        public abstract bool IsValidEventData { get; }

        #region Parameters-for-Ingame-Only
        protected AniEventControllerBase eventController = null;
        public bool isPlaying = false;

        public float duration => Mathf.Max(endTime - startTime, 0);
        #endregion
        public virtual bool ReadPropertiesFromJson(ref JsonObject jsonData)
        {
            if (jsonData == null || jsonData.Count == 0)
                return false;
            JsonObject defaultData = jsonData.GetField("Default");
            string[] split = defaultData.str.Split(',');
            groupId = int.Parse(split[0]);
            index = int.Parse(split[1]);
            name = split[2];
            startTime = float.Parse(split[3]);
            endTime = float.Parse(split[4]);

            return true;
        }

        #region Ingame-Only-Methods
        public virtual void InitOnRuntime(AniEventControllerBase ownerController) { eventController = ownerController; }
        public virtual void PlayEvent() { isPlaying = true; }
        public virtual void StopEvent() { isPlaying = false; }

        public virtual void OnControllerDestroy() { }
        #endregion

#if UNITY_EDITOR
        public virtual void WritePropertiesToJson(ref JsonObject jsonData)
        {
            jsonData.AddField("Default", GetDefaultPropertiesToString);
        }
        private string GetDefaultPropertiesToString
        {
            get
            {
                string str = groupId.ToString();
                str += "," + index.ToString();
                str += "," + name.ToString();
                str += "," + startTime.ToString();
                str += "," + endTime.ToString();
                return str;
            }
        }
#endif
    }
}
