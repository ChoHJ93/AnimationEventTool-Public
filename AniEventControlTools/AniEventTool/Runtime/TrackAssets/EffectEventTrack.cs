#if UNITY_EDITOR
namespace AniEventTool
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
    using Random = UnityEngine.Random;
#endif
    [Serializable]
    public class ParticleSimulateInfo
    {
        public ParticleSystem particleSystem { get; private set; }
        public bool loop { get; private set; }
        public float simulateTime { get; set; }
        public float beforeSimulateTime { get; set; }
        public bool isPlaying { get; set; }
        public List<ParticleSimulateInfo> subEmitters { get; private set; }


        public ParticleSimulateInfo(ParticleSystem ps)
        {
            particleSystem = ps;
            loop = ps.main.loop;
            simulateTime = 0;
            beforeSimulateTime = 0;
            isPlaying = false;
            subEmitters = new List<ParticleSimulateInfo>();

            // Get sub-emitters
            for (int i = 0; i < ps.subEmitters.subEmittersCount; i++)
            {
                ParticleSystem subEmitter = ps.subEmitters.GetSubEmitterSystem(i);
                if (subEmitter != null)
                {
                    subEmitters.Add(new ParticleSimulateInfo(subEmitter));
                }
            }
        }

    }
    public class EffectEventTrack : AniEventTrack<ParticleEvent>
    {
        public override AniEventBase GetAniEvent => data;
        public ParticleEvent aniEvent => data;

        [SerializeField] string boneName;
        [SerializeField] Vector3 addedPosition;
        [SerializeField] Vector3 addedRotation;
        [SerializeField] float lifeTime;
        [SerializeField] bool attach;
        [SerializeField] bool startOnBone;
        private Transform targetBone { get; set; }
        [SerializeField] bool followParentRot;
        [SerializeField] public bool keep;
        [SerializeField] bool deactiveLoop;
        [SerializeField] bool ignoreY;

        [SerializeField] bool detachOnEnd;
        [SerializeField] bool manualDetach; //for inspector only
        [SerializeField] float detachTime;

        private bool attachForSimulate = true;
        private bool IsAttach => attachForSimulate;
        private bool useBone => attachToBone || startOnBone;
        #region properties forEditor
        public GameObject particlePrefab { get; set; }
        [SerializeField] bool prevActiveState = false;
        [SerializeField] bool attachToBone;
        public Vector3 startPosition { get; private set; }
        public Quaternion startRotation { get; private set; }

        [HideInInspector] public string[] boneNames;
        [SerializeField] int selectIndex;
        public GameObject particleInstance { get; set; }
        public List<ParticleSimulateInfo> simulateInfoList = new List<ParticleSimulateInfo>();

        private bool isTransformChanged = false;
        private Vector3 effectPrevPos = Vector3.zero;
        private Quaternion effectPrevRot = Quaternion.identity;
        private Vector3 effectPrevScale = Vector3.one;
        #endregion

        private void OnEnable()
        {
            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;
        }
        private void OnDisable()
        {
            PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdated;
        }

        protected override void Init(WindowState windowState, ParticleEvent particleEvent, AniEventGroupTrack parentTrackAsset = null)
        {
            base.Init(windowState, particleEvent, parentTrackAsset);

            #region Init properties
            boneName = string.IsNullOrWhiteSpace(particleEvent.boneName) ? "None" : particleEvent.boneName;
            addedPosition = particleEvent.addedPosition;
            addedRotation = particleEvent.addedRotation;
            lifeTime = particleEvent.lifeTime;
            attach = particleEvent.attach;
            startOnBone = particleEvent.startOnBone;
            targetBone = boneName.Equals("None") == false ? windowState.SelectedController.Editor_GetBone(boneName) : null;
            attachToBone = !startOnBone && targetBone != null;
            followParentRot = particleEvent.followParentRot;
            deactiveLoop = particleEvent.deactiveLoop;
            ignoreY = particleEvent.ignoreY;
            particlePrefab = particleEvent.prefab;

            var boneNameList = windowState.SelectedController.GetBoneNameList;
            boneNames = boneNameList.ToArray();
            selectIndex = boneNameList.FindIndex(item => item.Equals(boneName));
            selectIndex = selectIndex < 0 ? 0 : selectIndex;
            #endregion

            if (particlePrefab != null)
            {
                SetPrefabObjectData(particlePrefab, out GameObject prefabInstance);
            }

            keep = particleEvent.keep; //InitEffectObjectInstance ���� �����ϴ� �� ���� ������ �� �켱
            detachOnEnd = particleEvent.detachOnEnd;
            detachTime = particleEvent.detachTime;
            manualDetach = detachTime > 0;
            //if (windowState.SelectedClip != null && simulateInfoList.IsNullOrEmpty() == false)
            //endTime = keep ? windowState.SelectedClip.length : startTime + simulateInfoList.Max(info => info.particleSystem.main.duration);
            endTime = particleEvent.endTime;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            particlePrefab = null;
        }

        public override void SetPrefabObjectData(GameObject originalPrefab, out GameObject prefabInstance, bool setEndTime = false)
        {
            if(!aniEvent.HasEffectComponent(originalPrefab))
            {
                Debug.Log($"<color=yellow>{originalPrefab.name} ������ ���Ͽ��� {typeof(ParticleSystem).Name} ������Ʈ�� ã�� �� �����ϴ�. \n������ Ÿ�ӿ��� �ùķ��̼� ���� �ʽ��ϴ�. (��Ÿ�ӿ����� ������ ��)</color>");
                base.SetPrefabObjectData(null, out prefabInstance, setEndTime);
                return;
            }

            base.SetPrefabObjectData(originalPrefab, out prefabInstance, setEndTime);

            particlePrefab = originalPrefab;
            this.particleInstance = prefabInstance;
            simulateInfoList.Clear();
            GameObject effectInstance = cachedObject.ObjectInstance;
            if (effectInstance == null)
                return;

            uint randomSeed = (uint)Random.Range(1, 100);
            SetRandomSeedRecursive(effectInstance, randomSeed);
            if (effectInstance.TryGetComponent(out ParticleSystem ps))
            {
                ParticleSimulateInfo simulateInfo = new ParticleSimulateInfo(ps);
                simulateInfoList.Add(simulateInfo);
            }
            else
            {
                List<ParticleSystem> particles = effectInstance?.GetComponentsAtDepth<ParticleSystem>(1) ?? new List<ParticleSystem>();
                foreach (ParticleSystem childPS in particles)
                {
                    ParticleSimulateInfo simulateInfo = new ParticleSimulateInfo(childPS);
                    simulateInfoList.Add(simulateInfo);
                }
            }

            InitEffectObjectInstance(setEndTime);
        }
        void SetRandomSeedRecursive(GameObject rootObj, uint seed)
        {
            if (rootObj == null)
                return;

            foreach (ParticleSystem ps in rootObj.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.randomSeed = seed;
            }
        }

        private void InitEffectObjectInstance(bool setEndTime = false)
        {
            if (this.particleInstance == null || simulateInfoList.IsNullOrEmpty())
                return;
            bool isLoopEffect = simulateInfoList.Exists(info => info.loop);
            keep = isLoopEffect;
            //SetOriginalTransform();
            InitEffectTr();
            if (setEndTime)
                SetEndTime();

            Transform boneTr = targetBone;

            SetStartTr();
            SetEffectTransform();
            SetAddedTr();

#if UNITY_EDITOR
            Init_for_Inspector();
#endif
        }
        private void InitEffectTr()
        {
            Transform effectTr = particleInstance.transform;
            effectTr.localPosition = Vector3.zero;
            effectTr.localRotation = Quaternion.identity;
            effectTr.localScale = Vector3.one;
        }
        private void SetOriginalTransform()
        {
            //Transform effectPrefabTr = particlePrefab.transform;
            //originalPos = effectPrefabTr.localPosition;
            //originalRot = effectPrefabTr.localRotation.normalized;
            //originalScale = effectPrefabTr.localScale;
        }

        public override void PlayEvent(float currentTime)
        {
            base.PlayEvent(currentTime);

            if (windowState?.SelectedController == null || cachedObject?.ObjectInstance == null)
                return;
            //bool isInTime = keep ? !(startTime > currentTime || windowState.duration <= currentTime) : !(startTime > currentTime || endTime < currentTime);
            bool isInTime = currentTime >= startTime && currentTime <= (keep ? windowState.duration : endTime);
            bool showEvent = isInTime && isEnable;
            GameObject effectInstance = cachedObject.ObjectInstance;
            if (prevActiveState != showEvent)
            {
                prevActiveState = showEvent;
                OnEffectActive(showEvent);
            }
            effectInstance.SetActive(showEvent);
            Animator ani = effectInstance.GetComponent<Animator>();

            if (showEvent && windowState.IsTimeChanged == false)
            {
                CheckEffectModified();
            }

            if (windowState.IsTimeChanged && effectInstance.activeInHierarchy)
            {
                attachForSimulate = attach ? manualDetach ? currentTime - startTime < detachTime : true : false;
                SetEffectTransform();
            }

            SimulateParticles(simulateInfoList, currentTime);
        }
        public override void StopEvent()
        {
            base.StopEvent();
        }
        public override void OnResourceModified()
        {
            base.OnResourceModified();
        }
        public override void MoveTime(float movedTime)
        {
            base.MoveTime(movedTime);
            SetStartTr();
            SetEffectTransform();
            SceneView.RepaintAll();
        }

        protected override void ClearData()
        {
            base.ClearData();
            data.prefab = default;
            data.boneName = boneName = default;
            data.addedPosition = addedPosition = default;
            data.addedRotation = addedRotation = default;
            data.lifeTime = lifeTime = default;
            data.attach = attach = default;
            data.startOnBone = attachToBone = default;
            data.followParentRot = followParentRot = default;
            data.keep = keep = default;
            data.deactiveLoop = deactiveLoop = default;
            data.ignoreY = ignoreY = default;
            data.detachOnEnd = detachOnEnd = default;
            data.detachTime = detachTime = default;
        }
        public override void ApplyToEventData()
        {
            base.ApplyToEventData();
            SetEndTime();

            data.prefab = particlePrefab;
            data.boneName = boneName;
            data.addedPosition = addedPosition;
            data.addedRotation = addedRotation;
            data.lifeTime = lifeTime;
            data.attach = attach;
            data.startOnBone = startOnBone;
            data.followParentRot = followParentRot;
            data.keep = keep;
            data.deactiveLoop = deactiveLoop;
            data.ignoreY = ignoreY;
            data.detachOnEnd = detachOnEnd;
            data.detachTime = manualDetach ? detachTime : 0;
        }

        private void OnEffectActive(bool activeSelf)
        {
            if (activeSelf)
            {
                SetStartTr();
            }
        }

        private void SimulateParticles(List<ParticleSimulateInfo> infos, float time)
        {
            foreach (ParticleSimulateInfo psi in infos)
            {
                psi.simulateTime = time - startTime;
                if (psi.simulateTime != psi.beforeSimulateTime)
                {
                    psi.particleSystem.Simulate(psi.simulateTime, true);
                    SimulateSubEmitters(psi.subEmitters, psi.simulateTime);
                    SceneView.RepaintAll();
                }

                psi.beforeSimulateTime = psi.simulateTime;
            }
        }
        private void SimulateSubEmitters(List<ParticleSimulateInfo> subEmitters, float time)
        {
            foreach (ParticleSimulateInfo psi in subEmitters)
            {
                psi.simulateTime = time - startTime;
                if (psi.simulateTime != psi.beforeSimulateTime)
                {
                    psi.particleSystem.Simulate(psi.simulateTime, false, false, false);
                    SimulateSubEmitters(psi.subEmitters, psi.simulateTime);
                    SceneView.RepaintAll();
                }

                psi.beforeSimulateTime = psi.simulateTime;
            }
        }

        private void SetEndTime()
        {
            if (simulateInfoList.IsNullOrEmpty())
                return;

            endTime = keep ? windowState.duration : startTime + simulateInfoList.Max(info => info.particleSystem.main.duration);
        }

        #region Calc-Transform
        private void SetStartTr()
        {
            if (windowState.SelectedController == null || windowState.SelectedClip == null)
                return;

            windowState.SelectedController.Editor_UpdateTransform(startTime);

            Transform controllerTr = windowState.SelectedController.transform;
            windowState.SelectedClip.SampleAnimation(controllerTr.gameObject, startTime);

            if (useBone && targetBone != null)
            {
                startPosition = targetBone.position;
                startRotation = targetBone.transform.rotation;
                if (followParentRot)
                    startRotation *= Quaternion.Inverse(controllerTr.rotation);
            }
            else
            {
                startPosition = windowState.GetControllerTr(true).position;
                startRotation = followParentRot ? controllerTr.rotation : Quaternion.identity;
            }
        }

        private void SetAddedTr()
        {
            Transform effectTr = particleInstance.transform;
            Transform controllerTr = windowState.SelectedController.transform;

            if (useBone && targetBone != null)
            {
                addedPosition = effectTr.position - targetBone.position;//InverseTransformPoint();
                Quaternion addedRot = Quaternion.Inverse(targetBone.rotation.normalized) * effectTr.rotation;// * Quaternion.Inverse(startRotation);// * Quaternion.Inverse(originalRot);
                addedRotation = addedRot.eulerAngles;
            }
            else
            {
                addedPosition = effectTr.position - startPosition;
                Quaternion relatedRot = effectTr.rotation.normalized * Quaternion.Inverse(startRotation.normalized); ;// * Quaternion.Inverse(originalRot);
                Vector3 addedRot = effectTr.rotation.normalized.eulerAngles - controllerTr.rotation.normalized.eulerAngles;
                addedRotation = relatedRot.eulerAngles;
            }

            addedPosition = CustomMathUtils.GetRoundedVector(addedPosition);
            addedRotation = CustomMathUtils.GetRoundedVector(addedRotation);
        }

        private void SetEffectTransform(bool resimulate = false)
        {
            bool isDetached = attach && attachForSimulate == false;
            if (particleInstance == null || isDetached)
                return;

            Transform effectTr = particleInstance.transform;
            Vector3 controllerPos = windowState.GetControllerTr(true)?.position ?? Vector3.zero;
            Quaternion controllerRot = windowState.SelectedController?.transform.rotation.normalized ?? Quaternion.identity;

            if (attach)
            {
                Vector3 parentRot = followParentRot ? controllerRot.eulerAngles : Vector3.zero;
                if (attachToBone && targetBone != null)
                {
                    parentRot = targetBone.rotation * parentRot;
                    effectTr.position = targetBone.position + Quaternion.Euler(parentRot) * addedPosition;
                    effectTr.rotation = targetBone.rotation * Quaternion.Euler(addedRotation + parentRot);
                }
                else
                {
                    effectTr.position = addedPosition + controllerPos;
                    effectTr.rotation = Quaternion.Euler(addedRotation + parentRot) * startRotation;
                }
            }
            else
            {
                effectTr.position = startPosition + addedPosition;
                effectTr.rotation = startRotation * Quaternion.Euler(addedRotation);
            }
            if (ignoreY)
                effectTr.position = new Vector3(effectTr.position.x, 0, effectTr.position.z);
            if (!resimulate)
            {
                CheckTransformChanged();
            }
        }
        private bool CheckTransformChanged()
        {
            if (particleInstance != null)
            {
                Transform effectInstanceTr = particleInstance.transform;

                if (effectPrevPos.Equals(effectInstanceTr.position) == false
                    || effectPrevRot.Equals(effectInstanceTr.rotation) == false
                    || effectPrevScale.Equals(effectInstanceTr.localScale) == false)
                {
                    effectPrevPos = effectInstanceTr.position;
                    effectPrevRot = effectInstanceTr.rotation;
                    effectPrevScale = effectInstanceTr.localScale;

                    isTransformChanged = true;
                    return true;
                }
            }
            isTransformChanged = false;
            return false;
        }
        #endregion

        private void UpdateEffectTr()
        {

        }
        private void CheckEffectModified()
        {
            if (windowState.playing)
                return;
            if (isTransformChanged)
            {
                //SetStartTr();
                //SetAddedTr();
                SetEffectTransform();
                foreach (ParticleSimulateInfo psi in simulateInfoList)
                {
                    psi.particleSystem.Simulate(psi.simulateTime, true);
                }
            }
            isTransformChanged = false;
        }

        public override void Refresh()
        {
            base.Refresh();
            SetStartTr();
            SetEffectTransform();
        }

        void OnPrefabInstanceUpdated(GameObject instance)
        {
            if (particlePrefab != null && instance.Equals(particlePrefab))
            {
                //foreach (ParticleSimulateInfo psi in simulateInfoList)
                //{
                //    psi.particleSystem.Simulate(psi.simulateTime, true);
                //}
                //SceneView.RepaintAll();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Only for Inspector
        /// </summary>
        private ParticleSystem[] psArray = null;
        /// <summary>
        /// Only for Inspector
        /// </summary>
        public bool hasLoopPS { get; private set; }
        /// <summary>
        /// Only for Inspector
        /// </summary>
        public float currentTime => Mathf.Max(0, (float)windowState.time - startTime);
        private void Init_for_Inspector()
        {
            psArray = particleInstance.GetComponentsInChildren<ParticleSystem>();
            if (psArray != null && psArray.Length > 0)
            {
                foreach (ParticleSystem ps in psArray)
                {
                    hasLoopPS = ps.main.loop;
                    if (hasLoopPS)
                        break;
                }
            }
        }

        public void Inspector_TransformOptionsEditted()
        {
            if (particlePrefab == null || particleInstance == null)
                return;
            if (useBone)
            {
                boneName = windowState.SelectedController.Editor_GetBoneName(selectIndex);
                targetBone = boneName.Equals("None") == false ? windowState.SelectedController.Editor_GetBone(boneName) : null;
            }
            else
            {
                selectIndex = 0;
                boneName = "None";
                targetBone = null;
            }

            float originStartTime = startTime;
            if (attach)
                startTime = (float)windowState.time;

            SetStartTr();
            SetAddedTr();
            startTime = originStartTime;

            SetEndTime();
        }

        public void Inspector_TransformValuesEditted()
        {
            SetEffectTransform(true);
        }
#endif
    }
}
#endif