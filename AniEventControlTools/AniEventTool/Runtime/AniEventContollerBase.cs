using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AniEventTool
{
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using System.Threading;

    public abstract class AniEventControllerBase : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        protected Animator m_Animator;
        [SerializeField]
        [HideInInspector]
        protected Transform m_Transform;

        protected Dictionary<InGameAniInfo, InGameAniEventGroup> m_dicInGameAniEvent = new Dictionary<InGameAniInfo, InGameAniEventGroup>();
        protected Dictionary<int, InGameAniInfo> m_dicHashToAnimInfo = new Dictionary<int, InGameAniInfo>();
        protected Dictionary<string, Transform> m_dicBone = new Dictionary<string, Transform>();

        protected bool jsonLoaded = false;
        protected bool isInitialized = false;
        protected InGameAniInfo m_CurrentAnimInfo = null;
        protected InGameAniEventGroup m_CurrentEventGroup = null;

        #region Getter
        public Transform ControllerTr => m_Transform;
        protected bool isRootMotion => m_Animator.applyRootMotion;
        public Vector3 ControllerPos => m_Transform.position;
        public Quaternion ControllerRot => m_Transform.rotation;
        protected int LastStateHash => lastStateHash;
        protected bool EventExist => m_CurrentAnimInfo != null && m_CurrentEventGroup != null && m_CurrentEventGroup.EventsCount > 0;
        protected InGameAniInfo CurrentAnimInfo => m_CurrentAnimInfo;

        public Dictionary<string, Transform> DicBone => m_dicBone;
        #endregion

        #region Variables for Animations
        protected int lastStateHash = 0; // ���������� ����� ������Ʈ �ؽ�
        protected float lastStateTime = 0f; // ���������� ����� ������Ʈ�� ��� �ð�
        protected AnimatorTransitionInfo lastTransitionInfo;

        protected Vector3 m_aniStartPos = Vector3.zero;
        protected Vector3 m_aniStartRot = Vector3.zero;
        protected float m_stateOriginSpeed = 1;
        protected float m_deltaTime;
        protected float m_beforeTime;
        protected float m_normalizedTime;

        protected float m_curAniTime = 0.0f;
        protected int m_loopCount = 0;


        protected eAniStateExitType GetStateExitType(int lastHash, float lastNormalizedTime, int curHash, float curNormalizeTime, AnimatorTransitionInfo transitionInfo)
        {
            eAniStateExitType exitType = eAniStateExitType.None;
            if (curHash.Equals(lastHash) == false)
            {
                //if(lastHash != 0 && transitionInfo.nameHash == 0)
                if (transitionInfo.anyState)
                {
                    exitType = eAniStateExitType.Forced;
                }
                else
                {
                    exitType = eAniStateExitType.Transit;
                }
            }
            else if (curNormalizeTime < lastNormalizedTime)
            {
                exitType = eAniStateExitType.Replay;
            }

#if UNITY_EDITOR
            string lastStateName = m_dicAniStateNames.TryGetValue(lastHash, out string LStateName) ? LStateName : "None";
            string curStateName = m_dicAniStateNames.TryGetValue(curHash, out string CStateName) ? CStateName : "None";
            //Debug.Log($"<color=yellow>State Changed : LastState : {lastStateName} -> CurState : {curStateName}</color> / State Exit Type : {exitType}\n{Time.realtimeSinceStartupAsDouble}");
#endif

            return exitType;
        }

        public float StateOriginSpeed => m_stateOriginSpeed;
        public float AniSpeed
        {
            get { return (m_Animator?.speed ?? 1) * m_stateOriginSpeed; }
            set
            {
                if (m_Animator != null)
                {
                    if (m_Animator.speed != value)
                    {
                        m_Animator.speed = value;
                        OnAniSpeedChange?.Invoke(value);
                    }
                }
            }
        }

        /// <summary>
        /// ������Ʈ ���� �� �� ȣ��
        ///  <para>m_CurrentAnimInfo, m_CurrentEventGroup�� ���ŵ� ��</para>
        ///  <para>�� �������� m_SkillController.GetCurActiveSkill�� �̹� ��� �� ��ų�� �ٲ������</para>
        ///  <para>* bool isReplay -> ���� ������Ʈ�� �ٽ� ����ϴ� ���� �� true</para>
        /// </summary>
        public UnityAction<eAniStateExitType> OnAniStateChange { get; set; }
        public UnityAction<float> OnAniSpeedChange { get; set; }
        #endregion

        #region Variables for EffectEvent
        //���� pooling �� ����
        protected const int effectInitCount = 2;
        protected const int effectMaxCount = 5;
        protected Dictionary<string, Stack<EffectEventComponent>> m_dicEffectPool = new Dictionary<string, Stack<EffectEventComponent>>();
        public Dictionary<string, Stack<EffectEventComponent>> EffectPool => m_dicEffectPool;
        #endregion

        #region Variables for Hit & MoveEvent
        protected Collider[] m_HitTargets;
        protected List<IEnumerator> coHitTargetRoutines = null;

        protected float m_colliderRadius = 0.0f;
        protected LayerMask m_blockableLayerMask = 0;
        protected RaycastHit obstacleHit;

        protected Collider m_TraceTarget;
        protected CancellationTokenSource m_CancelTrace = new CancellationTokenSource();
        protected virtual Vector3 ColliderCenter => m_Transform.position + Vector3.up * 0.5f;
        #endregion


        #region MoveEvent Methods
        public virtual void MoveController(MoveEventBase moveEvent) { }
        public virtual void StopMoveController() { }
        #endregion

        #region HitEvent Methods
        public virtual void HitTargets(HitEvent hitEvent) { }
        #endregion


#if UNITY_EDITOR
        protected Dictionary<int, string> m_dicAniStateNames = new Dictionary<int, string>();
        protected Dictionary<AnimInfo, List<AniEventGroup>> m_dicAniEvent = new Dictionary<AnimInfo, List<AniEventGroup>>();
        public abstract Dictionary<AnimInfo, List<AniEventGroup>> Editor_GetAniEventDic { get; }
        public abstract Transform Editor_RootBoneTr { get; }
        public abstract List<AniStateInfo> Editor_GetAniStateInfoList { get; }
        public abstract List<string> GetBoneNameList { get; }
        public abstract int Editor_GetAniStateCount { get; }

        public abstract void Editor_SetDrawMeshInfo();
        public abstract void Editor_DrawMesh(PreviewRenderUtility previewRenderUtility, int targetLayer);

        public abstract string Editor_GetBonePath(string boneName);
        public abstract void Editor_SetBoneInfo();
        public abstract string Editor_GetBoneName(int index);
        public abstract Transform Editor_GetBone(int index);

        public abstract Transform Editor_GetBone(string boneName);
        public abstract void Editor_GetAnimations();
        public abstract bool Editor_GetAllEventGroups(out List<AniEventGroup> allEventGroups);
        public abstract string[] Editor_GetAniStateNames();
        public abstract string[] Editor_GetAnimationNames();
        public abstract bool Editor_TryGetAnimationClip(int selection, out AnimationClip animationClip);
        public abstract List<AniEventGroup> Editor_GetEventList(int selection);
        public abstract AniEventGroup Editor_AddEventGroup(int selection);
        public abstract bool Editor_GetValidStateInfo(int selection, out AnimInfo animInfo, out AniStateInfo stateInfo);
        public abstract void Editor_SetAllEventsData(AnimInfo key, List<AniEventGroup> value);
        public abstract void Editor_Release();
        public abstract void Editor_SetMoveEvent(MoveEventBase moveEvent, Vector3 lastEndPos);
        public abstract void Editor_UpdateTransform(float currentTime);
        public abstract void Editor_ResetTransform();
#endif
    }
}
