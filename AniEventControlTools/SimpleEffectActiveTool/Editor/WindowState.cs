using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.SimpleActiveTool.Editor
{
    [System.Serializable]
    [SerializeField]
    public class WindowState
    {
        private Transform _objInstanceRootTr;
        private EffectActiveController m_SelectedController;
        private List<AnimationClip> m_AnimationClips = new List<AnimationClip>();
        private List<string> m_AnimationClipNames = new List<string>();
        private int m_SelectedClipIndex = 0;

        public Transform objectRootTr
        {
            get
            {
                if (_objInstanceRootTr == null)
                {
                    _objInstanceRootTr = new GameObject("##EffectActiveTool_Root").transform;
                    _objInstanceRootTr.gameObject.hideFlags = HideFlags.DontSave;
                }
                return _objInstanceRootTr;
            }
        }
        public EffectActiveController SelectedController { get => m_SelectedController; set { m_SelectedController = value; } }
        public AnimationClip SelectedClip
        {
            get
            {
                if (m_AnimationClips.IsNullOrEmpty() == false)
                    return m_AnimationClips[m_SelectedClipIndex];
                else
                    return null;
            }
        }
        public List<string> AnimationClipNames { get => m_AnimationClipNames; }
        public int SelectedClipIndex { get => m_SelectedClipIndex; set { m_SelectedClipIndex = value; } }
        public AnimationClip[] GetClips { get => m_AnimationClips.ToArray(); }

        #region Variables_For_Playing
        private double _time;
        private double _beforeTime;
        private float _playSpeed = 1;
        public bool playing;
        public bool loop;
        public bool showWarningIcon = false;
        public string message = string.Empty;

        public double time
        {
            get { return _time; }
            set
            {
                double curTime = _time;
                _time = value < 0 ? 0 : value;

                if (_beforeTime.Equals(_time) == false)
                    _beforeTime = curTime;
            }
        }
        public bool IsTimeChanged => _time != _beforeTime;
        public float playSpeed { get { return _playSpeed <= 0 ? 1 : _playSpeed; } set { _playSpeed = value > 0 ? value : 1; } }
        public float duration { get { return m_AnimationClips.IsNullOrEmpty() ? 0 : m_AnimationClips[m_SelectedClipIndex].length; } }
        #endregion

        public void Init()
        {
            _time = 0f;
            _beforeTime = 0f;
            _playSpeed = 1f;
            m_SelectedClipIndex = m_SelectedClipIndex >= m_AnimationClips.Count ? 0 : m_SelectedClipIndex;
            playing = false;
            loop = false;
        }

        public void Clear()
        {
            m_SelectedClipIndex = 0;
            m_AnimationClips.Clear();
            m_AnimationClipNames.Clear();
        }
        public void OnDestroy()
        {
            if (_objInstanceRootTr != null)
                GameObject.DestroyImmediate(_objInstanceRootTr.gameObject);
        }
        public void AddClips(params AnimationClip[] clips)
        {
            m_AnimationClips?.AddRange(clips);
            m_AnimationClips.Sort((x, y) => x.name.CompareTo(y.name));

            foreach (var item in m_AnimationClips)
            {
                m_AnimationClipNames?.Add(item.name);
            }
        }
    }

}