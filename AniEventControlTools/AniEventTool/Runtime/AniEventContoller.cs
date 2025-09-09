using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool
{
    using System.Linq;

    /// <summary>
    /// T is the type of the class that Use AniEventController such as Unit, Player, Monster.. etc
    /// </summary>
    /// <typeparam name="T">such as Unit, Player, Monster.. etc</typeparam>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public partial class AniEventController<T> : AniEventControllerBase where T : MonoBehaviour
    {

        private void Awake()
        {
            jsonLoaded = LoadEventFileFromJson();
            if (m_Animator == null)
                m_Animator = GetComponent<Animator>();

            m_Transform = transform;
            m_colliderRadius = 0;
            Collider collider = GetComponent<Collider>();
            if (collider is CharacterController charCollider)
            {
                m_colliderRadius = charCollider.radius;
            }
            else if (collider is CapsuleCollider capCollider)
            {
                m_colliderRadius = capCollider.radius;
            }
            else if (collider is SphereCollider sphereCollider)
            {
                m_colliderRadius = sphereCollider.radius;
            }
            m_colliderRadius = Mathf.Max(0, m_colliderRadius);

            m_blockableLayerMask = 0;
            for (int i = 0; i < 32; i++)
            {
                if (Physics.GetIgnoreLayerCollision(gameObject.layer, i) == false)
                    m_blockableLayerMask |= 1 << i;
            }

            m_blockableLayerMask &= ~(1 << gameObject.layer);

            Init_ProjectRelated();
        }

        private void Start()
        {
            Init();
            CacheAniStateNames();
            CacheBones();
            isInitialized = true;

#if UNITY_EDITOR
            Editor_CacheAniStateNames();
#endif
        }
        private void OnAnimatorMove()
        {
            if (m_Animator == null || m_Transform == null)
                return;

            if (m_Animator.applyRootMotion)
            {
                CheckObstacleOnRootMotion();
                m_Transform.rotation *= m_Animator.deltaRotation;
            }

            if (!isInitialized)
                return;

            UpdateCheckAnimationEvent();

            if (!EventExist)
                return;

            UpdateTime();
            UpdatePlayEvent();
        }
        private void OnDestroy()
        {
            OnAniStateChange = null;
            StopAllCoroutines();
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;
#endif
            foreach (Stack<EffectEventComponent> effectPool in m_dicEffectPool.Values)
            {
                foreach (EffectEventComponent effectObj in effectPool)
                {
                    if (effectObj != null)
                        DestroyImmediate(effectObj.gameObject);
                }
            }
            foreach (InGameAniEventGroup eventGroup in m_dicInGameAniEvent.Values)
            {
                foreach (AniEventBase evt in eventGroup.aniEvents)
                {
                    evt.OnControllerDestroy();
                }
            }
        }

        #region Load Data from json file
        protected bool LoadEventFileFromJson()
        {
            string[] fileName = name.Split(' ');
            fileName[0] = fileName[0].Replace("(Clone)", "");
            string strJsonFilePath = $"JsonDatas/";
            TextAsset textAsset = Resources.Load<TextAsset>(strJsonFilePath + fileName[0]);
            return LoadEventFileFromJSON(textAsset);
        }
        protected bool LoadEventFileFromJSON(TextAsset file)
        {
            if (file == null)
                return false;

            JsonObject root = new JsonObject(file.text);

            List<(AnimInfo, List<AniEventGroup>)> loadedData;
            bool result = CommonUtil.LoadEventDataFromJSON(root, out loadedData);

            if (result == false)
                return false;

            foreach ((AnimInfo info, List<AniEventGroup> events) in loadedData)
            {
                InGameAniInfo aniInfo = new InGameAniInfo();
                aniInfo.stateName = info.stateName;
                aniInfo.clipName = info.clipName;
                aniInfo.endTime = info.endTime;
                m_dicInGameAniEvent.Add(aniInfo, new InGameAniEventGroup(events));

                foreach (AniEventBase evt in events.SelectMany(x => x.aniEvents))
                {
                    evt.InitOnRuntime(this);
                }
            }

            return true;
        }
#endregion

        #region Initialize
        protected virtual void Init()
        {
            AniSpeed = 1.0f;
            m_stateOriginSpeed = 1.0f;

            m_loopCount = 0;
            m_normalizedTime = 0.0f;
            m_deltaTime = 0f;
            m_beforeTime = 0.0f;
            m_curAniTime = 0.0f;
        }
        protected virtual void CacheAniStateNames()
        {
            if (m_Animator == null)
                return;
            m_dicHashToAnimInfo.Clear();
            AnimationClip[] clips = m_Animator.runtimeAnimatorController.animationClips;
            foreach (InGameAniInfo info in m_dicInGameAniEvent.Keys)
            {
                int stateHash = Animator.StringToHash(info.stateName);
                info.stateHash = stateHash;
                foreach (AnimationClip clip in clips)
                {
                    if (clip.name == info.clipName)
                    {
                        info.clip = clip;
                        info.inverseClipLength = 1 / clip.length;
                        break;
                    }
                }
#if UNITY_EDITOR
                if (info.clip == null)
                    Debug.LogError($"<color=orange>json �������� Ŭ���� ���� �̸��� Ŭ���� ã�� �� �����ϴ�!</color>\n<color=magenta>{gameObject.name} - State : {info.stateName} / Ani Clip : {info.clipName}</color>");
#endif
                m_dicHashToAnimInfo.Add(stateHash, info);
            }
        }
        protected virtual void CacheBones()
        {
            m_dicBone.Clear();

            Transform rootTr = m_Transform.FindChildAtDepthWithName(1, "Root");
            if (rootTr == null)
                rootTr = transform.Find("Bip001");

            if (rootTr != null)
            {
                Transform[] bones = rootTr.GetComponentsInChildren<Transform>();
                for (int i = 0; i < bones.Length; i++)
                {
                    if (m_dicBone.ContainsKey(bones[i].name))
                    {
                        Debug.Log($"<color=yellow>{rootTr.parent.gameObject.name} ���Ͽ� �ߺ��� �̸��� ���� �����մϴ�! -> {bones[i].name}\n�𵨸� ���ҽ��� �� �̸� ������ �ʿ��մϴ�.</color>");
                        continue;
                    }
                    m_dicBone.Add(bones[i].name, bones[i]);
                }
            }
        }
        public Transform GetBone(string boneName)
        {
            if (m_dicBone.ContainsKey(boneName) == false)
                return null;

            return m_dicBone[boneName];
        }
        #endregion

        protected virtual void CheckObstacleOnRootMotion()
        {
            if (m_Animator.deltaPosition.magnitude > 0)
            {
                float radius = m_colliderRadius;
                Vector3 nextPos = m_Transform.position + m_Animator.deltaPosition;
                Vector3 direction = m_Animator.deltaPosition.normalized;
                Vector3 directionXZ = new Vector3(direction.x, 0, direction.z);
                Vector3 from = m_Transform.position + Vector3.up * radius * 0.5f - directionXZ * radius;
                float checkDist = m_Animator.deltaPosition.magnitude + radius;

                // Perform collision detection
                bool hasHit = radius > 0 ?
                    Physics.SphereCast(from, radius, directionXZ, out obstacleHit, checkDist, m_blockableLayerMask) :
                    Physics.Raycast(from, directionXZ, out obstacleHit, checkDist, m_blockableLayerMask);

                if (hasHit)
                {
                    float correctedDistance = obstacleHit.distance - (radius > 0 ? radius : 0);
                    correctedDistance = Mathf.Max(0, correctedDistance);
                    nextPos = m_Transform.position + direction * correctedDistance;
                }
                // Apply the calculated position and adjust rotation
                m_Transform.position = nextPos;
            }
        }

        private void UpdateTime()
        {
            m_deltaTime = Time.timeSinceLevelLoad - m_beforeTime;
            m_curAniTime += m_deltaTime * AniSpeed;
            m_normalizedTime = m_curAniTime / m_CurrentAnimInfo.clip.length;

            m_beforeTime = Time.timeSinceLevelLoad;
        }
        protected virtual void UpdateCheckAnimationEvent()
        {
            if (m_Animator == null)
                return;
            AnimatorStateInfo currentStateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo nextStateInfo = m_Animator.GetNextAnimatorStateInfo(0);

            int currentStateHash = currentStateInfo.shortNameHash;
            int nextStateHash = nextStateInfo.shortNameHash;
            float currentStateTime = (Mathf.Min(currentStateInfo.normalizedTime, 0.999f)) % 1; // ���� ������Ʈ�� ��� �ð�
            float stateSpeed = currentStateInfo.speed * currentStateInfo.speedMultiplier;


            if (nextStateHash != 0)
            {
                currentStateHash = nextStateHash;
                currentStateTime = nextStateInfo.normalizedTime % 1;
                stateSpeed = nextStateInfo.speed * nextStateInfo.speedMultiplier;
                //Debug.Log("<color=yellow>�ִϸ��̼� ������Ʈ ����� ����!</color> ���� ������Ʈ Hash : " + m_dicHashToAnimInfo[nextStateHash].stateName);
            }
            bool moveToNextState = currentStateHash != lastStateHash;
            bool replaySameState = currentStateHash == lastStateHash && currentStateTime < lastStateTime;

            if (moveToNextState)
                lastTransitionInfo = default;

            if (m_Animator.IsInTransition(0))
            {
                lastTransitionInfo = m_Animator.GetAnimatorTransitionInfo(0);
            }

            if (moveToNextState || replaySameState)
            {
                eAniStateExitType exitType = GetStateExitType(lastStateHash, lastStateTime, currentStateHash, currentStateTime, lastTransitionInfo);

                OnAniStateChanged_ProjRelated(replaySameState);
                OnAniStateChanged(replaySameState);

                if (m_dicHashToAnimInfo.TryGetValue(currentStateHash, out InGameAniInfo currentAnimInfo) &&
                    m_dicInGameAniEvent.TryGetValue(currentAnimInfo, out InGameAniEventGroup eventGroup))
                {
                    m_CurrentAnimInfo = currentAnimInfo;
                    m_CurrentEventGroup = eventGroup;
                    m_stateOriginSpeed = stateSpeed;
                    OnPlayAni();
                    //Debug.Log("<color=green>�ִϸ��̼� ������Ʈ ����� �Ǵ� ��� �ð� �ʱ�ȭ��!</color> ���� ������Ʈ: " + currentAnimInfo.stateName);
                }
                else
                {
                    m_CurrentAnimInfo = null;
                    m_CurrentEventGroup = null;
                    m_stateOriginSpeed = 1;
                    //Debug.Log("<color=yellow>�ִϸ��̼� ������Ʈ �����! ���� ������Ʈ �̸��� ã�� �� �����ϴ�.</color>");
                }

                OnAniStateChange?.Invoke(exitType);
                lastStateHash = currentStateHash;
#if UNITY_EDITOR
                OnAniStateChanged_Editor(nextStateInfo);
#endif
            }

#if USE_CorrectionValue
            float correctionValue = 0.1f * (m_CurrentAnimInfo?.inverseClipLength ?? 0);//����������  m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime���� 0.03~0.1���� ������ �־ ����ϴ� ���� ��
            lastStateTime = Mathf.Max(0, currentStateTime - correctionValue);//0.1f);
#else
            lastStateTime = currentStateTime;
#endif
        }
        private void OnAniStateChanged(bool isReplay)
        {
            if (EventExist == false) return;
            StopAniEvents();
            AniSpeed = 1.0f;
        }
        private void OnPlayAni()
        {
            m_aniStartPos = m_Transform.position;
            m_aniStartRot = m_Transform.eulerAngles;
            m_loopCount = 0;
            m_normalizedTime = 0.0f;
            m_deltaTime = 0.0f;
            m_beforeTime = Time.timeSinceLevelLoad;
            m_curAniTime = 0.0f;
#if UNITY_EDITOR
            editor_EventTime = 0;
#endif
        }
        private void UpdatePlayEvent()
        {
            AnimationClip currentClip = m_CurrentAnimInfo.clip;

            float aniPlayTime = m_curAniTime;
            if (currentClip.isLooping)
                aniPlayTime = Mathf.Max(0f, aniPlayTime - m_loopCount);

            float eventTime = aniPlayTime;
#if UNITY_EDITOR
            editor_EventTime = m_normalizedTime;
#endif
            PlayAniEvents(eventTime);

#if USE_CHJ_SOUND
            PlayAniEvents(m_CurrentEventGroup.sounds.ToArray(), eventTime, PlayEvent_Sound);
#endif
            if (m_CurrentEventGroup.isPlaying == false)
                m_CurrentEventGroup.isPlaying = true;
        }

        private void PlayAniEvents(float time)
        {
            AniEventBase[] aniEvents = m_CurrentEventGroup.aniEvents.ToArray();
            if (aniEvents == null || aniEvents.Length == 0)
                return;

            foreach (AniEventBase evt in aniEvents)
            {
                if (evt == null)
                    return;

                bool bPlay = evt.endTime > 0 ? evt.startTime <= time && time < evt.endTime : evt.startTime <= time;
                bool bStop = evt.endTime > 0 ? evt.startTime > time && time >= evt.endTime : false;
                if (bPlay && !evt.isPlaying)
                {
                    evt.PlayEvent();
                }
                else if (bStop && evt.isPlaying)
                {
                    evt.StopEvent();
                }
            }
        }
        protected void StopAniEvents()//<T>(List<T> aniEvents) where T : AniEventBase
        {
            if (EventExist == false)
                return;

            if (m_CurrentEventGroup.isPlaying)
            {
                foreach (AniEventBase evt in m_CurrentEventGroup.aniEvents)
                {
                    evt.StopEvent();
                }
            }
            m_CurrentEventGroup.isPlaying = false;
        }

        public virtual void StopAllAniEvents()
        {
            StopAllHitRoutines();
            StopAniEvents();
        }

        #region EffectEvents
        private void RegisterEffect(ParticleEvent evt)
        {
            EffectEventComponent effectObj = evt.EffectComponent;
            if (m_dicEffectPool.ContainsKey(evt.prefabName))
                return;

            Stack<EffectEventComponent> pool = new Stack<EffectEventComponent>(effectMaxCount);
            for (int i = 0; i < effectInitCount; i++)
            {
                EffectEventComponent effectClone = GameObject.Instantiate(evt.prefab).GetComponent<EffectEventComponent>();
                effectClone.gameObject.name = evt.prefabName + $"({i + 1:D2})";
                //new GameObject(effectObj.gameObject.name + $"{i + 1:D2}").AddComponent<EffectEventComponent>();
                effectClone.Init();
                effectClone.gameObject.SetActive(false);
                pool.Push(effectClone);

#if UNITY_EDITOR
                effectClone.transform.SetParent(CommonUtil.Editor_ObjectPoolTr);
#endif
            }
            m_dicEffectPool.Add(evt.prefabName, pool);

        }
        private EffectEventComponent SpawnEffect(ParticleEvent evt)
        {
            if (evt.effectObj.activeSelf == false)
            {
                evt.effectObj.SetActive(true);
                return evt.EffectComponent;
            }
            //Debug.Log($"{evt.effectObj.name} is Active, pop another from pool!");
            if (!m_dicEffectPool.TryGetValue(evt.prefabName, out Stack<EffectEventComponent> pool))
            {
                Debug.Log($"Not Exist Key ==> SpawnKey : {evt.prefabName}");
                return null;
            }
            EffectEventComponent effect;
            if (pool.Count > 0)
            {
                effect = pool.Pop();
            }
            else
            {
                effect = GameObject.Instantiate(evt.prefab).GetComponent<EffectEventComponent>();
                effect.gameObject.name = evt.prefabName + "_Clone";
                //new GameObject(evt.prefabName + "_Clone").AddComponent<EffectEventComponent>();
                Debug.Log($"{effect.gameObject.name} Cloned");
                effect.Init();
            }
            effect.gameObject.SetActive(true);
            //Debug.Log($"{effect.gameObject.name} poped");
            return effect;
        }
        private void DespawnEffect(ParticleEvent evt, EffectEventComponent effectObj)
        {
            if (effectObj == null) return;
            if (!effectObj.gameObject.activeSelf) return;

            effectObj.gameObject.SetActive(false);

            if (evt.effectObj.Equals(effectObj.gameObject) == false)
            {
                if (!m_dicEffectPool.TryGetValue(evt.prefabName, out Stack<EffectEventComponent> pool))
                {
                    Debug.Log($"Not Exist Key ==> SpawnKey : {evt.prefabName}");
                    return;
                }

                pool.Push(effectObj);
                //Debug.Log($"{effectObj.name} pushed");
            }
        }

        private void PlayEvent_Effect(ParticleEvent particleEvent)
        {
            particleEvent.EffectComponent = SpawnEffect(particleEvent);

            Transform boneTr = m_dicBone.TryGetValue(particleEvent.boneName, out boneTr) ? boneTr : null;
            if (particleEvent.attach)
            {
                SetEffectToAttachPoint(particleEvent, particleEvent.effectObj.transform, boneTr);
                if (boneTr != null)
                    StartCoroutine(UpdateParticlePos(particleEvent, boneTr));
                else
                    StartCoroutine(UpdateParticlePos(particleEvent));
            }
            else
            {
                Vector3 bonePos = boneTr?.position ?? ControllerPos;
                Quaternion boneRot = boneTr?.rotation ?? Quaternion.identity;
                Vector3 parentPos = particleEvent.startOnBone ? bonePos : ControllerPos;
                if (particleEvent.ignoreY)
                    parentPos.y = m_Transform.position.y + 0.01f;
                Vector3 v = particleEvent.addedPosition;
                Quaternion parentRot = Quaternion.identity;
                if (particleEvent.followParentRot)
                {
                    parentRot = ControllerRot * boneRot;
                    v = parentRot * boneRot * particleEvent.addedPosition;
                }
                particleEvent.effectObj.transform.position = parentPos + v;
                particleEvent.effectObj.transform.rotation = parentRot * Quaternion.Euler(particleEvent.addedRotation);
            }
        }
        private void StopEvent_Effect(ParticleEvent particleEvent)
        {
            if (particleEvent.keep)
                StartCoroutine(EffectDelayDeactiveRoutine(particleEvent));
            else
                DespawnEffect(particleEvent, particleEvent.EffectComponent);
        }
        private void SetEffectToAttachPoint(ParticleEvent effect, Transform effectTr, Transform boneTr)
        {
            Quaternion parentRot = Quaternion.identity;
            if (effect.followParentRot == true)
                parentRot = m_Transform.rotation;

            Vector3 v = m_Transform.rotation * effect.addedPosition;
            //Quaternion boneRot = Quaternion.identity;
            if (boneTr != null)
            {
                v = boneTr.rotation * v;
                Vector3 bonePos = boneTr.position;
                if (effect.ignoreY == true)
                    bonePos.y = m_Transform.position.y + 0.01f;//posOnGround.y;

                effectTr.position = boneTr.position + v;

                parentRot = boneTr.rotation;
            }
            else
            {
                effectTr.position = m_Transform.position + v;
            }

            effectTr.rotation = parentRot * Quaternion.Euler(effect.addedRotation.x, effect.addedRotation.y, effect.addedRotation.z);
        }

        IEnumerator UpdateParticlePos(ParticleEvent effect, Transform boneTr)
        {
            GameObject effectObj = effect.effectObj;
            while (effectObj.activeSelf && boneTr != null)
            {
                SetEffectToAttachPoint(effect, effectObj.transform, boneTr);
                yield return new WaitForFixedUpdate();
            }
        }
        IEnumerator UpdateParticlePos(ParticleEvent effect)
        {
            Transform effectTr = effect.effectObj.transform;

            Vector3 relatedPos = m_Transform.InverseTransformPoint(ControllerPos, ControllerRot, Vector3.one, effectTr.position);
            Quaternion relatedRot = Quaternion.Inverse(ControllerRot) * effectTr.rotation;

            while (effectTr.gameObject.activeSelf)
            {
                effectTr.position = ControllerPos + ControllerRot * relatedPos;
                effectTr.rotation = ControllerRot * relatedRot;
                yield return null;
            }
        }
        IEnumerator EffectDelayDeactiveRoutine(ParticleEvent effect)
        {
            float delay = effect.lifeTime;
            bool deactiveLoop = effect.deactiveLoop;
            float loopLifeTime = effect.loopLifeTime;
            GameObject effectObj = effect.effectObj;
            EffectEventComponent eventComponent = effect.EffectComponent;

            yield return new WaitForSeconds(delay);
            if (effectObj == null)
                yield break;

            if (deactiveLoop)
            {
                eventComponent.SetParticleLoopValue(false);
                yield return new WaitForSeconds(loopLifeTime);
                eventComponent.SetParticleLoopValue(true);
            }

            DespawnEffect(effect, eventComponent);
        }
        #endregion

#if USE_CHJ_SOUND
        #region SoundEvents
        protected virtual void PlayEvent_Sound(SoundEvent soundEvent)
        {
            switch (soundEvent.soundType)
            {
                default:
                case SoundManager.eSoundType.SFX:
                case SoundManager.eSoundType.VOICE:
                    SoundManager.Instance.PlaySFX(null, soundEvent.soundName, soundEvent.soundType);
                    break;
                    //SoundManager.Instance.PlayVoice(null, soundEvent.soundName);
                    //break;
            }
        }
        private void PlayEventSound(SoundEvent[] sounds, float time)
        {
            if (sounds == null || sounds.Length == 0)
                return;

            foreach (SoundEvent sound in sounds)
            {
                if (sound == null)
                    continue;

                if (sound.startTime <= time)
                {
                    if (sound.isPlaying == false)
                    {
                        SoundManager.Instance.PlaySFX(null, sound.soundName);
                        sound.isPlaying = true;
                    }
                }
            }
        }
        #endregion
#endif

        #region HitEvents
        protected void StopAllHitRoutines()
        {
            foreach (IEnumerator coHitTargetRoutine in coHitTargetRoutines)
            {
                if (coHitTargetRoutine != null)
                    StopCoroutine(coHitTargetRoutine);
            }
            coHitTargetRoutines.Clear();
        }
        protected virtual void PlayEvent_HitEvent(HitEvent hitEvent) { }
        protected virtual void StopEvent_HitEvent(HitEvent hitEvent) { }
        protected virtual void OnExitState_HitEvent(HitEvent hitEvent) { }
        #endregion

        #region Project Related
        protected virtual void Init_ProjectRelated(int maxTargetCount = 20) { }
        protected virtual void OnAniStateChanged_ProjRelated(bool isReplay) { }
        #endregion

        #region Utility Methods
        protected Vector3 LocalToWorld(Vector3 localPoint, Vector3 position, Quaternion rotation)
        {
            Vector3 rotatedPoint = rotation * localPoint;
            Vector3 worldPoint = rotatedPoint + position;
            return worldPoint;
        }
        protected Transform GetNearestTrByAngle(float detectRange, float detectAngle, int layerMask, int blockableLayerMask)
        {
            Transform nearestTarget = null;
            {
                Transform controllerTr = ControllerTr;
                detectRange = detectRange <= 0 ? float.MaxValue : detectRange;
                int detectedCount = Physics.OverlapSphereNonAlloc(controllerTr.position, detectRange, m_HitTargets, layerMask);
                if (detectedCount <= 0)
                    return null;

                float minDist = float.MaxValue;
                float dist = 0;
                for (int i = 0; i < detectedCount; i++)
                {
                    Collider other = m_HitTargets[i];

                    if (other.gameObject.Equals(gameObject))
                        continue;
                    Transform targetTr = other.transform;
                    Vector3 targetPosXZ = new Vector3(targetTr.position.x, 0, targetTr.position.z);
                    Vector3 controllerPosXZ = new Vector3(controllerTr.position.x, 0, controllerTr.position.z);
                    Vector3 dir = (targetPosXZ - controllerPosXZ).normalized;
                    float angle = Vector3.Angle(dir, controllerTr.forward);
                    //if(m_CharacterController.gameObject.layer == (int)LayerEnum.Player)
                    //    Debug.Log($"<color=blue>Angle : {Vector3.Angle(dir, controllerTr.forward)} /  dot : {Vector3.Dot(crossProduct, Vector3.up)} {angle}<= {detectAngle} = {angle <= detectAngle}");

                    if (detectAngle <= 0 || angle <= detectAngle * 0.5f)
                    {
                        //��, ���� ���� ��� ��� ����
                        if (Physics.Raycast(ColliderCenter, dir, out RaycastHit rayHit, dir.magnitude, blockableLayerMask))
                            continue;
                        dist = Vector3.Distance(controllerTr.position, targetTr.position);
                        if (minDist > dist)
                        {
                            minDist = dist;
                            nearestTarget = targetTr;
                            m_TraceTarget = other;
                        }
                    }
                }
            }
            return nearestTarget;
        }

        protected List<T> GetTargetList(HitEvent hitEvent, Vector3 controllerPos, Quaternion controllerRot, Vector3 controllerForward)
        {
            int layerMask = hitEvent.layerMask;
            bool attach = hitEvent.attach;
            bool relativeRot = hitEvent.followParentRot;
            int maxCount = hitEvent.targetCount;

            controllerPos = attach ? controllerPos : m_aniStartPos;
            controllerRot = attach ? controllerRot : Quaternion.Euler(m_aniStartRot);
            //controllerPos = m_Animator.applyRootMotion && attach ? m_aniStartPos : controllerPos;
            List<T> targetList = new List<T>();
            foreach (RangeInfo info in hitEvent.ranges)
            {
                Vector3 center;
                if (attach)
                    center = relativeRot ? LocalToWorld(info.center, controllerPos, controllerRot) : controllerPos + info.center;
                else
                    center = controllerPos + (relativeRot ? controllerRot * info.center : info.center);

                Quaternion infoRot = Quaternion.Euler(info.rotation);
                Quaternion rotation = relativeRot ? controllerRot * infoRot : infoRot;
                float radius = info.radius;
                eRangeType rangeType = info.rangeType;
                List<T> targets = new List<T>();
                switch (rangeType)
                {
                    case eRangeType.Sphere:
                        targets = GetTargetList_Sphere(center, radius, layerMask, maxCount);
                        break;
                    case eRangeType.Capsule:
                        targets = GetTargetList_Capsule(center, rotation, info.size.z, radius, attach, relativeRot, layerMask, maxCount);
                        break;
                    case eRangeType.Box:
                        targets = GetTargetList_Box(center, info.size, rotation, attach, relativeRot, layerMask, maxCount);
                        break;
                    case eRangeType.Ray:
                        {
                            Vector3 dir = relativeRot ? controllerForward : Vector3.forward;
                            dir = infoRot * dir;
                            float dist = info.radius == 0 ? Mathf.Infinity : info.radius; // Vector3.Distance(from, to);
                            targets = GetTargetList_Ray(center, dir, dist, layerMask, maxCount);
                        }
                        break;
                }

                targetList = targetList.Concat(targets).Distinct().ToList();
            }

            return targetList;
        }
        protected List<T> GetTargetList_Sphere(Vector3 center, float radius, int layerMask, int maxCount)//, bool useColliderEvent = false)
        {
            int count = 0;
            List<T> targetList = new List<T>();
            int detectedCount = Physics.OverlapSphereNonAlloc(center, radius, m_HitTargets, layerMask);
            if (detectedCount > 0)
            {
                for (int i = 0; i < detectedCount; i++)
                {
                    Collider other = m_HitTargets[i];
                    Transform tfTarget = other.transform;//.parent;

#if HitEffect
                    Vector3 hitPos = tfTarget.position;
                    Vector3 hitDir = center - tfTarget.position;
                    Ray rayToTarget = new Ray(center, hitDir);
                    if (Physics.Raycast(rayToTarget, out RaycastHit hit, radius, layerMask))
                        hitPos = hit.point;

                    if (other.CompareTag(CommonType.LAYER_GROUND) || other.CompareTag(CommonType.LAYER_WALL))
                    {
                        HitEffect(tfTarget, hitPos, -hitDir);
                        continue;
                    }
#endif

                    if (tfTarget != null && tfTarget.TryGetComponent(out T controller))
                    {
                        targetList.Add(controller);
                        count++;
                        if (maxCount > 0 && count >= maxCount)
                            break;
                    }
                }
            }

            return targetList;
        }
        protected List<T> GetTargetList_Capsule(Vector3 center, Quaternion rotation, float height, float radius, bool attach, bool relativeRot, int layerMask, int maxCount)
        {

            Vector3[] capsulePoint = CustomMathUtils.GetCapsulePoints(center, height, radius, rotation);
            int count = 0;
            List<T> targetList = new List<T>();
            int detectedCount = Physics.OverlapCapsuleNonAlloc(capsulePoint[0], capsulePoint[1], radius, m_HitTargets, layerMask);
            if (detectedCount > 0)
            {
                for (int i = 0; i < detectedCount; i++)
                {
                    Collider other = m_HitTargets[i];
                    Transform tfTarget = other.transform;//.parent;
                    if (tfTarget != null && tfTarget.TryGetComponent(out T controller))
                    {
                        targetList.Add(controller);
                        count++;
                        if (maxCount > 0 && count >= maxCount)
                            break;
                    }
                }
            }

            return targetList;
        }
        protected List<T> GetTargetList_Box(Vector3 center, Vector3 size, Quaternion rotation, bool attach, bool relativeRot, int layerMask, int maxCount)
        {
            int count = 0;
            List<T> targetList = new List<T>();
            int detectedCount = Physics.OverlapBoxNonAlloc(center, size * 0.5f, m_HitTargets, rotation, layerMask);
            if (detectedCount > 0)
            {
                for (int i = 0; i < detectedCount; i++)
                {
                    Collider other = m_HitTargets[i];
                    Transform tfTarget = other.transform;//.parent;
                    if (tfTarget != null && tfTarget.TryGetComponent(out T controller))
                    {
                        targetList.Add(controller);
                        count++;
                        if (maxCount > 0 && count >= maxCount)
                            break;
                    }
                }
            }

            return targetList;
        }
        protected List<T> GetTargetList_Ray(Vector3 center, Vector3 direction, float distance, int layerMask, int maxCount)
        {
            int count = 0;
            List<T> targetList = new List<T>();
            RaycastHit[] hits = Physics.RaycastAll(center, direction, distance, layerMask);
            if (hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    Collider other = hit.collider;
                    Transform tfTarget = other.transform;//.parent;
                    if (tfTarget != null && tfTarget.TryGetComponent(out T controller))
                    {
                        targetList.Add(controller);
                        count++;
                        if (maxCount > 0 && count >= maxCount)
                            break;
                    }
                }
            }

            return targetList;
        }
        #endregion


#if UNITY_EDITOR
        int editor_LastStateHash;
        public float editor_EventTime = 0;
        public Dictionary<int, InGameAniInfo> Editor_StateHashToAnimInfo => m_dicHashToAnimInfo;
        private void Editor_CheckAnimStateChange()
        {

            if (m_Animator == null)
                return;

            int currentStateHash = m_Animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            int nextStateHash = m_Animator.GetNextAnimatorStateInfo(0).shortNameHash;

            if (nextStateHash != 0)
            {
                currentStateHash = nextStateHash;
            }

            if (currentStateHash != editor_LastStateHash)
            {
                if (m_dicHashToAnimInfo.TryGetValue(currentStateHash, out InGameAniInfo currentAnimInfo)
                    && m_dicHashToAnimInfo.TryGetValue(editor_LastStateHash, out InGameAniInfo lastAnimInfo))
                {
                    Debug.Log($"<color=green>�ִϸ��̼� ������Ʈ �����!</color> : {currentAnimInfo.stateName} -> {lastAnimInfo.stateName}");
                }
                else
                {
                    Debug.Log("<color=red>�ִϸ��̼� ������Ʈ �����! ���� ������Ʈ �̸��� ã�� �� �����ϴ�.</color>");
                }
                editor_LastStateHash = currentStateHash;
            }
        }
        IEnumerator DrawArrowRoutine(Vector3 direction, float distance, float duration)
        {
            float time = 0;
            while (time < duration)
            {
                GizmoTool.CustomGizmoUtil.DrawArrow(transform.position, direction, distance, 10, Color.magenta);
                time += Time.deltaTime;
                yield return null;
            }
        }
#endif
    }

}