using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AniEventTool
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine.VFX;

    [System.Serializable]
    public class ParticleEvent : AniEventBase
    {
        //���� pooling �� ����
        protected const int effectInitCount = 2;
        protected const int effectMaxCount = 5;

        [SerializeField] public GameObject prefab;

        [SerializeField] public string boneName;
        [SerializeField] public Vector3 addedPosition;
        [SerializeField] public Vector3 addedRotation;
        [SerializeField] public float lifeTime;
        [SerializeField] public bool attach;
        [SerializeField] public bool startOnBone;
        [SerializeField] public bool followParentRot;
        [SerializeField] public bool ignoreY;

        [SerializeField] public bool keep;
        [SerializeField] public bool deactiveLoop; // false : active value / true: 1 : loop
        [SerializeField] public bool detachOnEnd;
        [SerializeField] public float detachTime;

        #region Parameters-for-Ingame-Only
        private Transform ControllerTr => eventController.ControllerTr;
        private Vector3 ControllerPos => eventController.ControllerPos;
        private Quaternion ControllerRot => eventController.ControllerRot;

        private EffectEventComponent _effectObj = null;
        internal EffectEventComponent EffectComponent { get { return _effectObj; } set { _effectObj = value; } }
        internal GameObject effectObj => _effectObj.gameObject;
        internal string prefabName = string.Empty;
        internal ParticleSystem[] psArray = null;
        internal float loopLifeTime = 0f;

        private CancellationTokenSource ctUpdatePos;
        #endregion
        private void InitOnRuntime()
        {
            if (deactiveLoop)
            {
                psArray = effectObj.GetComponentsInChildren<ParticleSystem>();
                loopLifeTime = 0;
                foreach (ParticleSystem ps in psArray)
                {
                    if (loopLifeTime < ps.main.duration)
                        loopLifeTime = ps.main.duration;
                }
            }
        }

        public void Clone(ParticleEvent pe)
        {
            //prefab = pe.prefab;
            //particle = pe.particle;
            boneName = pe.boneName;
            addedPosition = pe.addedPosition;
            addedRotation = pe.addedRotation;
            lifeTime = pe.lifeTime;
            attach = pe.attach;
            startOnBone = pe.startOnBone;
            followParentRot = pe.followParentRot;
            keep = pe.keep;
            ignoreY = pe.ignoreY;
            deactiveLoop = pe.deactiveLoop;
            detachOnEnd = pe.detachOnEnd;
            detachTime = pe.detachTime;

            prefabName = pe.prefabName;
            InitOnRuntime();
        }

        public override bool IsValidEventData => prefab != null;

        public override bool ReadPropertiesFromJson(ref JsonObject jsonData)
        {
            if (!base.ReadPropertiesFromJson(ref jsonData))
                return false;

            jsonData.GetField(out string prefabFullPath, "PrefabPath", string.Empty);

            if (!TryGetEffectPrefab(prefabFullPath, out GameObject effectObj))
                return false;

            Vector3 originalScale = effectObj.transform.localScale;
            jsonData.GetField(out boneName, "Bone", string.Empty);

            string[] strAdded = jsonData.GetField("AddedPosition").str.Split('|');
            addedPosition = new Vector3(float.Parse(strAdded[0]), float.Parse(strAdded[1]), float.Parse(strAdded[2]));

            strAdded = jsonData.GetField("AddedRotation").str.Split('|');
            addedRotation = new Vector3(float.Parse(strAdded[0]), float.Parse(strAdded[1]), float.Parse(strAdded[2]));

            jsonData.GetField(out lifeTime, "LifeTime", 0f);
            jsonData.GetField(out attach, "Attach", false);
            jsonData.GetField(out startOnBone, "StartOnBone", false);
            jsonData.GetField(out followParentRot, "FollowParentRot", false);
            jsonData.GetField(out keep, "Keep", false);
            jsonData.GetField(out deactiveLoop, "DeactiveLoop", false);
            jsonData.GetField(out ignoreY, "IgnoreY", false);
            jsonData.GetField(out detachOnEnd, "DetachOnEnd", false);
            jsonData.GetField(out detachTime, "DetachTime", 0f);

            prefabName = effectObj.gameObject.name;
#if UNITY_EDITOR

            if (Application.isPlaying)
            {
                effectObj.gameObject.name = JsonUtil.GetPrefabNameFromPath(prefabFullPath) + "_Origin";
                effectObj.transform.SetParent(CommonUtil.Editor_ObjectPoolTr);
            }
#endif
            if (effectObj.TryGetComponent(out EffectEventComponent evtComponent) == false)
                evtComponent = effectObj.AddComponent<EffectEventComponent>();
            evtComponent.Init();
            //effectObj = effectObj;
            prefab = effectObj;
            EffectComponent = evtComponent;
            effectObj.SetActive(false);

            return true;
        }
        private bool TryGetEffectPrefab(string path, out GameObject prefab)
        {
            string effPath = JsonUtil.GetPrefabPath(path);
            string effName = JsonUtil.GetPrefabNameFromPath(path);

            prefab = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets" + path);
                if (prefab == null)
                {
                    Debug.LogError("��ƼŬ ������ �ε��� �� �����ϴ�. " + effPath + effName);
                    return false;
                }
                if (!HasEffectComponent(prefab))
                {
                    Debug.LogError("ParticleSystem �Ǵ� VisualEffect ������Ʈ�� ã�� �� �����ϴ�. " + effPath + effName);
                    return false;
                }
                return true;
            }
            else
            {
#endif
                GameObject effectPrefab = Resources.Load<GameObject>(effPath + effName);
                if (effectPrefab == null)
                {
                    Debug.LogError("��ƼŬ ������ �ε��� �� �����ϴ�. " + effPath + effName);
                    return false;
                }
                if (!HasEffectComponent(effectPrefab))
                {
                    Debug.LogError("ParticleSystem �Ǵ� VisualEffect ������Ʈ�� ã�� �� �����ϴ�. " + effPath + effName);
                    return false;
                }

                GameObject effectObj = GameObject.Instantiate(effectPrefab);
                prefab = effectObj;
                effectObj.name = effName;
                return true;
#if UNITY_EDITOR
            }
#endif
        }

        public virtual bool HasEffectComponent(GameObject go)
        {
            return go.GetComponent<ParticleSystem>()
                   || go.GetComponentAtDepth<ParticleSystem>(1)
                   || go.GetComponent<VisualEffect>()
                   || go.GetComponentAtDepth<VisualEffect>(1);
        }

        #region Ingame-Only
        public override void InitOnRuntime(AniEventControllerBase ownerController)
        {
            base.InitOnRuntime(ownerController);
            RegisterEffect();
            ctUpdatePos = new CancellationTokenSource();

            eventController.OnAniSpeedChange += OnAniSpeedChange;
        }
        public override void PlayEvent()
        {
            base.PlayEvent();

            EffectComponent = SpawnEffect();

            Transform boneTr = eventController.DicBone.TryGetValue(boneName, out boneTr) ? boneTr : null;
            if (attach)
            {
                SetEffectToAttachPoint(effectObj.transform, boneTr);
                if (boneTr != null)
                    UpdateParticlePos(boneTr, ctUpdatePos.Token).Forget();
                else
                    UpdateParticlePos(ctUpdatePos.Token).Forget();
            }
            else
            {
                Vector3 bonePos = boneTr?.position ?? ControllerPos;
                Quaternion boneRot = boneTr?.rotation ?? Quaternion.identity;
                Vector3 parentPos = startOnBone ? bonePos : ControllerPos;
                if (ignoreY)
                    parentPos.y = ControllerTr.position.y + 0.01f;
                Vector3 v = addedPosition;
                Quaternion parentRot = Quaternion.identity;
                if (followParentRot)
                {
                    parentRot = ControllerRot * boneRot;
                    v = parentRot * boneRot * addedPosition;
                }
                effectObj.transform.position = parentPos + v;
                effectObj.transform.rotation = parentRot * Quaternion.Euler(addedRotation);
            }
        }
        public override void StopEvent()
        {
            base.StopEvent();
            if (keep)
            {
                if (detachOnEnd || detachTime > 0)
                {
                    ctUpdatePos.Cancel();
                    ctUpdatePos.Dispose();
                    ctUpdatePos = new CancellationTokenSource();
                }
                DelayDeactiveParticle(ctUpdatePos.Token).Forget();
            }
            else
                DespawnEffect(EffectComponent);
        }
        public override void OnControllerDestroy()
        {
            base.OnControllerDestroy();

            ctUpdatePos.Cancel();

            if (EffectComponent != null)
                GameObject.DestroyImmediate(effectObj);
            if (prefab != null)
                GameObject.DestroyImmediate(prefab);
        }

        private void OnAniSpeedChange(float speed)
        {
            EffectComponent.SetSimulateSpeed(speed);
        }

        private void RegisterEffect()
        {
            EffectEventComponent effectObj = EffectComponent;
            if (eventController.EffectPool.ContainsKey(prefabName))
                return;

            Stack<EffectEventComponent> pool = new Stack<EffectEventComponent>(effectMaxCount);
            for (int i = 0; i < effectInitCount; i++)
            {
                EffectEventComponent effectClone = GameObject.Instantiate(prefab).GetComponent<EffectEventComponent>();
                effectClone.gameObject.name = prefabName + $"({i + 1:D2})";
                //new GameObject(effectObj.gameObject.name + $"{i + 1:D2}").AddComponent<EffectEventComponent>();
                effectClone.Init();
                effectClone.gameObject.SetActive(false);
                pool.Push(effectClone);

#if UNITY_EDITOR
                effectClone.transform.SetParent(CommonUtil.Editor_ObjectPoolTr);
#endif
            }
            eventController.EffectPool.Add(prefabName, pool);

        }
        private EffectEventComponent SpawnEffect()
        {
            if (eventController == null)
                return null;

            if (effectObj.activeSelf == false)
            {
                effectObj.SetActive(true);
                return EffectComponent;
            }
            //Debug.Log($"{effectObj.name} is Active, pop another from pool!");
            if (!eventController.EffectPool.TryGetValue(prefabName, out Stack<EffectEventComponent> pool))
            {
                Debug.Log($"SpawnEffect -> Not Exist Key ==> SpawnKey : {prefabName}");
                return null;
            }
            EffectEventComponent effect;
            if (pool.Count > 0)
            {
                effect = pool.Pop();
            }
            else
            {
                effect = GameObject.Instantiate(prefab).GetComponent<EffectEventComponent>();
                effect.gameObject.name = prefabName + "_Clone";
                //new GameObject(prefabName + "_Clone").AddComponent<EffectEventComponent>();
                Debug.Log($"{effect.gameObject.name} Cloned");
                effect.Init();
            }
            effect.gameObject.SetActive(true);
            //Debug.Log($"{effect.gameObject.name} poped");
            return effect;
        }
        private void SetEffectToAttachPoint(Transform effectTr, Transform boneTr)
        {
            Quaternion parentRot = Quaternion.identity;
            if (followParentRot == true)
                parentRot = ControllerTr.rotation;

            Vector3 v = ControllerTr.rotation * addedPosition;
            //Quaternion boneRot = Quaternion.identity;
            if (boneTr != null)
            {
                v = boneTr.rotation * v;
                Vector3 bonePos = boneTr.position;
                if (ignoreY == true)
                    bonePos.y = ControllerTr.position.y + 0.01f;//posOnGround.y;

                effectTr.position = boneTr.position + v;

                parentRot = boneTr.rotation;
            }
            else
            {
                effectTr.position = ControllerTr.position + v;
            }

            effectTr.rotation = parentRot * Quaternion.Euler(addedRotation.x, addedRotation.y, addedRotation.z);
        }
        async UniTask UpdateParticlePos(Transform boneTr, CancellationToken ct)
        {
            float time = 0;
            while (effectObj.activeSelf && boneTr != null)
            {
                SetEffectToAttachPoint(effectObj.transform, boneTr);
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
                time += Time.fixedDeltaTime;
                if (detachTime > 0 && time > detachTime)
                    break;
            }
        }
        async UniTask UpdateParticlePos(CancellationToken ct)
        {
            Transform effectTr = effectObj.transform;
            Vector3 relatedPos = ControllerTr.InverseTransformPoint(ControllerPos, ControllerRot, Vector3.one, effectTr.position);
            Quaternion relatedRot = Quaternion.Inverse(ControllerRot) * effectTr.rotation;

            float time = 0;
            while (effectTr.gameObject.activeSelf)
            {
                effectTr.position = ControllerPos + ControllerRot * relatedPos;
                effectTr.rotation = ControllerRot * relatedRot;
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
                time += Time.fixedDeltaTime;
                if (detachTime > 0 && time > detachTime)
                    break;
            }
        }

        private void DespawnEffect(EffectEventComponent targetObj)
        {
            if (targetObj == null) return;
            if (!targetObj.gameObject.activeSelf) return;

            targetObj.gameObject.SetActive(false);

            if (effectObj.Equals(targetObj) == false)
            {
                if (!eventController.EffectPool.TryGetValue(prefabName, out Stack<EffectEventComponent> pool))
                {
                    Debug.Log($"DespawnEffect -> Not Exist Key ==> SpawnKey : {prefabName}");
                    return;
                }

                pool.Push(targetObj);
            }
        }
        async UniTask DelayDeactiveParticle(CancellationToken ct)
        {
            await UniTask.Delay((int)(lifeTime * 1000), cancellationToken: ct);

            if (effectObj == null || effectObj.activeSelf == false)
                return;

            if (deactiveLoop)
            {
                EffectComponent.SetParticleLoopValue(false);
                await UniTask.Delay((int)(loopLifeTime * 1000), cancellationToken: ct);
                EffectComponent.SetParticleLoopValue(true);
            }

            DespawnEffect(EffectComponent);
        }

        #endregion

#if UNITY_EDITOR
        public override void WritePropertiesToJson(ref JsonObject jsonData)
        {
            base.WritePropertiesToJson(ref jsonData);

            string fxPrefabPath = AssetDatabase.GetAssetPath(prefab);
            int n = fxPrefabPath.IndexOf("/");
            fxPrefabPath = fxPrefabPath.Substring(n, fxPrefabPath.Length - n);

            jsonData.AddField("PrefabPath", fxPrefabPath);
            jsonData.AddField("Bone", boneName);
            jsonData.AddField("AddedPosition", string.Format("{0}|{1}|{2}", addedPosition.x, addedPosition.y, addedPosition.z));
            jsonData.AddField("AddedRotation", string.Format("{0}|{1}|{2}", addedRotation.x, addedRotation.y, addedRotation.z));
            jsonData.AddField("LifeTime", lifeTime);
            jsonData.AddField("Attach", attach);
            jsonData.AddField("StartOnBone", startOnBone);
            jsonData.AddField("FollowParentRot", followParentRot);
            jsonData.AddField("Keep", keep);
            jsonData.AddField("DeactiveLoop", deactiveLoop);
            jsonData.AddField("IgnoreY", ignoreY);
            jsonData.AddField("DetachOnEnd", detachOnEnd);
            jsonData.AddField("DetachTime", detachTime);
        }
#endif
    }
}
