using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AniEventTool
{
    [System.Serializable]
    public class SoundEvent : AniEventBase
    {
        public string tableName;
        public string soundName;
        public bool isLoop;
#if USE_CHJ_SOUND
        #region Parameters-for-Ingame-Only
        public SoundManager.eSoundType soundType;
        #endregion
#endif
        public override bool IsValidEventData => string.IsNullOrWhiteSpace(soundName) == false;

        #if USE_CHJ_SOUND
        public override bool LoadFromJsonData(JsonObject jsonData)
        {
            if (base.LoadFromJsonData(jsonData))
            {
                jsonData.GetField(out tableName, "TableName", string.Empty);
                jsonData.GetField(out soundName, "SoundName", string.Empty);
                evt.soundType = SoundManager.GetSoundType(evt.tableName);
                return true;
            }
            return false;
        }
#endif

#if UNITY_EDITOR
        public override void WritePropertiesToJson(ref JsonObject jsonData)
        {
            base.WritePropertiesToJson(ref jsonData);

            jsonData.AddField("TableName", tableName);
            jsonData.AddField("SoundName", soundName);
        }
#endif
    }
}
