using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR && USE_CHJ_SOUND
namespace AniEventTool
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using eSoundType = SoundManager.eSoundType;
    public class SoundEventTrack : AniEventTrack<SoundEvent>
    {
        [SerializeField]
        AudioSource _audioSource = null;
        AudioSource audioSource
        {
            get
            {
                if (_audioSource == null)
                {
                    GameObject goAudioSource = new GameObject($"AudioSource_{data.index:D2}_{data.groupId:D2}");
                    _audioSource = SoundManager.Instance.Editor_CreateSEAudioSource(goAudioSource);
                    string id = _audioSource.gameObject.name.GetHashCode(StringComparison.Ordinal).ToString();
                    if (cachedObject == null || cachedObject.ObjectInstance == null)
                        cachedObject = new CachedObject(id, _audioSource.gameObject);
                    else
                        cachedObject.ReNew(id, _audioSource.gameObject);
                    _audioSource.transform.SetParent(windowState.objectRootTr);
                }
                return _audioSource;
            }
        }

        public eSoundType soundType => SoundManager.GetSoundType(tableName);
        [SerializeField] public string tableName;
        [SerializeField] public string soundName;
        private SoundEvent soundEvent => data;

        #region properties forEditor
        private bool isPlayed = false;
        private bool prevWindowPlayState = false;

        [SerializeField] private SoundInfo soundInfo;
        #endregion

        protected override void Init(WindowState windowState, SoundEvent aniEvent, AniEventGroupTrack parentTrackAsset = null)
        {
            base.Init(windowState, aniEvent, parentTrackAsset);

            soundName = aniEvent.soundName;
            tableName = aniEvent.tableName;
            if (string.IsNullOrEmpty(soundName) == false && SoundManager.Instance.Editor_TryGetFXSoundInfo(soundType, soundName, out SoundInfo info))
            {
                audioSource.gameObject.hideFlags = HideFlags.HideAndDontSave;
                SetSoundInfo(info);
                SetEndTime();
            }
        }

        public override void PlayEvent(float currentTime)
        {
            bool windowPlayState = windowState.playing;
            bool playStateChanged = prevWindowPlayState != windowPlayState;
            if (playStateChanged)
            {
                prevWindowPlayState = windowPlayState;
                if (windowPlayState)
                    isPlayed = false;
            }

            base.PlayEvent(currentTime);

            if (windowState?.SelectedController == null || audioSource == null)
                return;
            bool isInTime = currentTime >= startTime && currentTime <= endTime;
            if (isInTime)
            {
                bool playSound = isInTime && isEnable && windowState.playing && !isPlayed;
                if (playSound)
                {
                    SoundManager.Instance.PlaySFX(audioSource, soundName, soundType);
                    isPlayed = true;
                }
            }
        }
        public override void StopEvent()
        {
            base.StopEvent();
        }

        public override void ApplyToEventData()
        {
            if (string.IsNullOrEmpty(soundName) || string.IsNullOrWhiteSpace(soundName))
                return;

            base.ApplyToEventData();
            data.tableName = tableName;
            data.soundName = soundName;

            List<AudioClip> clipList = new List<AudioClip>(soundInfo.clips);
            List<float> volumeList = new List<float>(soundInfo.volume);
            for (int i = soundInfo.clips.Length - 1; i >= 0; i--)
            {
                if (soundInfo.clips[i] == null)
                {
                    clipList.RemoveAt(i);
                    volumeList.RemoveAt(i);
                }
            }
            soundInfo.clips = clipList.ToArray();
            soundInfo.volume = volumeList.ToArray();

            SoundManager.Instance.Editor_ApplySoundInfo(soundName, soundInfo);
        }
        public override void OnResourceModified()
        {
            base.OnResourceModified();
        }

        private void SetEndTime()
        {
            if (soundInfo == null || soundInfo.clips == null || soundInfo.clips.Length == 0)
                return;

            float duration = 0;
            foreach (AudioClip audioClip in soundInfo.clips)
            {
                if (audioClip.length > duration)
                    duration = audioClip.length;
            }
            endTime = duration + startTime;
        }

#if UNITY_EDITOR // for inspector
        [SerializeField] private string[] _tableNames = null;
        public string[] tableNames
        {
            get
            {
                if (_tableNames == null || _tableNames.Length != SoundManager.Instance.Editor_GetSoundTables.Length)
                {
                    SoundTable[] soundTables = SoundManager.Instance.Editor_GetSoundTables;
                    _tableNames = new string[soundTables.Length + 1];
                    _tableNames[0] = "None";
                    for (int i = 1; i < soundTables.Length + 1; i++)
                    {
                        _tableNames[i] = soundTables[i - 1].name;
                    }
                }
                return _tableNames;
            }
        }

        [SerializeField] private string[] _soundInfoNames = null;
        public string[] soundInfoNames
        {
            get
            {
                if (_soundInfoNames == null || _soundInfoNames.Length == 0)
                    _soundInfoNames = new string[] { "None" };
                if (selectedTableID > 0 && (SoundManager.Instance.Editor_GetSoundTables[selectedTableID - 1].sounds.Length + 1) != _soundInfoNames.Length) 
                {
                    SetSoundNameArray();
                }
                return _soundInfoNames;
            }
        }

        [SerializeField] public int selectedTableID = 0;
        [SerializeField] public int selectedSoundID = 0;
        public void SetSoundInfo(SoundInfo originInfo)
        {
            SoundInfo copiedInfo = new SoundInfo();
            copiedInfo.Editor_Clone(originInfo);
            soundInfo = copiedInfo;
        }
        public bool DrawSoundInfo => selectedTableID > 0 && selectedSoundID > 0
            && soundInfo != null && !string.IsNullOrEmpty(soundInfo.name) && soundInfo.clips != null && soundInfo.clips.Length > 0;

        public void OnInspectorModified()
        {
            if (selectedTableID > 0)
            {
                SetSoundNameArray();
            }

            if (selectedSoundID > 0)
            {
                soundName = _soundInfoNames[selectedSoundID];
                SetSoundInfo(SoundManager.Instance.Editor_GetSoundInfo(soundType, soundName));
            }
            else
            {
                tableName = string.Empty;
                soundName = string.Empty;
                soundInfo = null;
            }
            if (soundInfo != null)
            {
                audioSource.gameObject.SetActive(true);
                SetEndTime();
            }
        }

        private void SetSoundNameArray() 
        {

            int tableID = selectedTableID - 1;
            tableName = _tableNames[selectedTableID];

            _soundInfoNames = new string[SoundManager.Instance.Editor_GetSoundTables[tableID].sounds.Length + 1];
            _soundInfoNames[0] = "None";
            for (int i = 1; i < _soundInfoNames.Length; i++)
            {
                _soundInfoNames[i] = SoundManager.Instance.Editor_GetSoundTables[tableID].sounds[i - 1].name;
            }
        }

        public void Editor_PlayClip(AudioClip clip)
        {
            if (clip == null)
                return;

            audioSource.PlayOneShot(clip);
        }
#endif
    }
}
#endif