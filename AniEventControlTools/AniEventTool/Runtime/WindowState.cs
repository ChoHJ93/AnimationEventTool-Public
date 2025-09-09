namespace AniEventTool
{
    using UnityEngine;
    using UnityEditor;
    using AniEventTool.Editor;

    [System.Serializable]
    [SerializeField]
    public class WindowState
    {
        public static readonly float defaultFrameRate = TimeUtilityReflect.ToFrameRateValue(StandardFrameRates.Fps60);

        private int _frame;
        private double _frameRate;
        private double _time;
        private double _beforeTime;
        private float _playSpeed = 1;
        private float _duration;
        private float _inverseFramRate;
        private AniEventControllerBase _eventContoller = null;
        private AnimationClip _animationClip = null;
        private Transform _objInstanceRootTr;

        int _cameraType = 0;
        Camera _gameCamera = null;

        public int frame { get { return _frame; } set { _frame = value < 0 ? 0 : value; } }
        public double frameRate { get { return _frameRate <= 0 ? defaultFrameRate : _frameRate; } set { _frameRate = value <= 0 ? defaultFrameRate : value; _inverseFramRate = (float)value < 0 ? 1 / defaultFrameRate : 1 / (float)value; } }
        public float inverseFrameRate => _inverseFramRate;
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
        public float duration { get { return _duration; } set { _duration = value < 0 ? 0 : value; } }
        public float playSpeed { get { return _playSpeed <= 0 ? 1 : _playSpeed; } set { _playSpeed = value > 0 ? value : 1; } }
        public AniEventControllerBase SelectedController { get { return _eventContoller; } set { _eventContoller = value; } }
        public Transform GetControllerTr(bool checkRootMotion = false) { return checkRootMotion && useRootMotion ? _eventContoller.Editor_RootBoneTr : _eventContoller.transform; }
        public AnimationClip SelectedClip
        {
            get { return _animationClip; }
            set
            {
                isClipSelected = value != null;
                _animationClip = value;
                AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(_animationClip);
                useRootMotion = !clipSettings.keepOriginalPositionXZ && !clipSettings.loopBlendPositionXZ;
            }
        }
        public bool useRootMotion { get; private set; }
        public Transform objectRootTr
        {
            get
            {
                if (_objInstanceRootTr == null)
                {
                    _objInstanceRootTr = new GameObject("�̺�Ʈ ��").transform;
                    _objInstanceRootTr.gameObject.hideFlags = HideFlags.DontSave;
                }
                return _objInstanceRootTr;
            }
        }

        public int cameraTypeFlag { get { return _cameraType; } set { _cameraType = value; } }
        public Camera gameCamera { get { return _gameCamera; } set { _gameCamera = value; } }


        public bool isClipSelected { get; private set; }
        public bool playing;
        public bool loop;
        //public int animationSelection;
        public int aniStateSelection;
        public Vector2 timeAreaShowRange;

        public WindowState()
        {
            time = 0f;
            duration = 1f;
            frame = 0;

            isClipSelected = false;
            playing = false;
            loop = false;
            //animationSelection = 0;
            aniStateSelection = 0;
        }
        public void Init()
        {
            time = 0f;
            duration = 1f;
            frame = 0;
            playSpeed = 1;
            isClipSelected = false;
            playing = false;
            loop = false;
            //aniStateSelection = 0;
        }
        public void Clear()
        {
            Init();
            if (_eventContoller != null)
                GameObject.DestroyImmediate(_eventContoller.gameObject);
            _eventContoller = null;
        }
    }
}
