using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
namespace AniEventTool
{
    using UnityEditor;
    using GizmoTool;

    public class HitEventTrack : AniEventTrack<HitEvent>
    {
        [System.Serializable]
        private class RangeGizmo
        {
            public RangeInfo rangeInfo { get; private set; }
            public GameObject obj { get; private set; }
            Collider collider;

            public RangeGizmo(RangeInfo info, string name)
            {
                rangeInfo = info;
                if (obj == null)
                    obj = new GameObject($"Range_{name}");
                obj.hideFlags = HideFlags.HideAndDontSave;
                SetCollider(info);
            }
            private void SetCollider(RangeInfo info)
            {
                switch (info.rangeType)
                {
                    case eRangeType.Sphere:
                        {
                            SphereCollider newCollider = obj.GetComponent<SphereCollider>(true);
                            collider = newCollider;
                            obj.transform.position = info.center;
                            newCollider.center = Vector3.zero;
                            newCollider.radius = info.radius;
                        }
                        break;
                    case eRangeType.Capsule:
                        {
                            CapsuleCollider newCollider = obj.GetComponent<CapsuleCollider>(true);
                            collider = newCollider;
                            obj.transform.position = info.center;
                            obj.transform.rotation = Quaternion.Euler(info.rotation);
                            newCollider.center = Vector3.zero;
                            newCollider.height = info.size.y;
                            newCollider.radius = info.radius;
                        }
                        break;
                    case eRangeType.Box:
                        {
                            BoxCollider newCollider = obj.GetComponent<BoxCollider>(true);
                            collider = newCollider;
                            obj.transform.position = info.center;
                            obj.transform.rotation = Quaternion.Euler(info.rotation);
                            newCollider.center = Vector3.zero;
                            newCollider.size = info.size;
                        }
                        break;
                }
                if (collider)
                    collider.isTrigger = true;
            }
            public void Refresh()
            {
                if (rangeInfo == null)
                    DestroyImmediate(obj);

                if (obj.TryGetComponent(out Collider attachedCollider))
                {
                    bool checkSphereType = rangeInfo.rangeType == eRangeType.Sphere && attachedCollider is not SphereCollider;
                    bool checkCapsuleType = rangeInfo.rangeType == eRangeType.Capsule && attachedCollider is not CapsuleCollider;
                    bool checkBoxType = rangeInfo.rangeType == eRangeType.Box && attachedCollider is not BoxCollider;
                    bool checkRayType = rangeInfo.rangeType == eRangeType.Ray && attachedCollider != null;
                    if (checkSphereType || checkCapsuleType || checkBoxType || checkRayType)
                    {
                        DestroyImmediate(attachedCollider);
                    }
                }
                SetCollider(rangeInfo);
            }
            public void Override(RangeInfo info, string name)
            {
                rangeInfo = info;
                if (obj == null)
                    obj = new GameObject($"Range_{name}");
                else
                    obj.name = $"Range_{name}";
                obj.hideFlags = HideFlags.HideAndDontSave;

                Refresh();
            }
        }

        [SerializeField] public LayerMask layerMask;
        [SerializeField] public eTargetType targetType;
        [SerializeField] public int targetCount;
        [SerializeField] public int hitCount;
        [SerializeField] public float durationPerHit;
        [SerializeField] public float lifeTime;
        [SerializeField] public bool stiffEffect;
        [SerializeField] public bool attach;
        [SerializeField] public bool followParentRot;
        [SerializeField] public List<RangeInfo> ranges;

        #region properties forEditor
        [SerializeField] public bool limitTargetCount;
        [SerializeField] public bool invokeMultiple;
        [SerializeField] public CustomGizmoUtil.Style gizmoStyle;
        [SerializeField] public float gizmoAlpha;
        [SerializeField] private List<RangeGizmo> rangeGizmos;
        #endregion

        protected override void Init(WindowState windowState, HitEvent aniEvent, AniEventGroupTrack parentTrackAsset = null)
        {
            base.Init(windowState, aniEvent, parentTrackAsset);

            targetType = aniEvent.targetType;
            layerMask = aniEvent.layerMask;
            targetCount = aniEvent.targetCount;
            ranges = aniEvent.ranges ?? new List<RangeInfo>();
            attach = aniEvent.attach;
            followParentRot = aniEvent.followParentRot;
            lifeTime = aniEvent.lifeTime;
            stiffEffect = aniEvent.stiffEffect;
            hitCount = aniEvent.hitCount;
            durationPerHit = aniEvent.durationPerHit;
            endTime = lifeTime > 0 ? aniEvent.startTime + lifeTime : aniEvent.endTime;
            limitTargetCount = targetCount > 0;
            invokeMultiple = hitCount > 1;

            gizmoStyle = CustomGizmoUtil.Style.Wireframe;
            gizmoAlpha = 0.5f;
        }

        protected override void ClearData()
        {
            base.ClearData();

            data.layerMask = default;
            data.targetType = default;
            data.targetCount = default;
            data.ranges = new List<RangeInfo>();
            data.attach = default;
            data.followParentRot = default;
            data.lifeTime = default;
            data.stiffEffect = default;
            data.hitCount = default;
            data.durationPerHit = default;
        }

        public override void ApplyToEventData()
        {
            base.ApplyToEventData();

            data.layerMask = layerMask;
            data.targetType = targetType;
            data.targetCount = targetCount;
            data.ranges = ranges;
            data.attach = attach;
            data.followParentRot = followParentRot;
            data.lifeTime = lifeTime;
            data.stiffEffect = stiffEffect;
            data.hitCount = hitCount;
            data.durationPerHit = durationPerHit;
        }

        public override void PlayEvent(float currentTime)
        {
            base.PlayEvent(currentTime);

            bool isInTime = currentTime >= startTime && currentTime <= (lifeTime > 0 ? endTime : startTime + windowState.inverseFrameRate);
            bool activeEvent = isInTime && isEnable;

            foreach (RangeInfo info in ranges)
            {
                if (activeEvent)
                    DrawGizmo(info);
            }
        }
        private void DrawGizmo(RangeInfo info)
        {
            Color gizmoColor;
            if (targetType == eTargetType.Player)
                gizmoColor = new Color(0, 1, 0, gizmoAlpha);
            else if (targetType == eTargetType.Other)
                gizmoColor = new Color(1, 0, 0, gizmoAlpha);
            else if (targetType == eTargetType.Custom)
                gizmoColor = new Color(0, 0, 1, gizmoAlpha);
            else
                gizmoColor = new Color(0.5f, 0.5f, 0.5f, gizmoAlpha);

            int sceneViewCount = SceneView.sceneViews.Count;


            Vector3 controllerPos = windowState.GetControllerTr(attach).position;
            Quaternion controllerRot = windowState.SelectedController.transform.rotation;

            Vector3 center;
            if (attach)
                center = controllerPos + info.center;
            else
                center = followParentRot ? controllerRot * info.center : info.center;

            Quaternion infoRot = Quaternion.Euler(info.rotation);
            Quaternion gizmoRot = followParentRot ? controllerRot * infoRot : infoRot;
            for (int i = 0; i <= sceneViewCount; i++)
            {
                Camera sceneCam;
                if (i < sceneViewCount)
                    sceneCam = (SceneView.sceneViews[i] as SceneView).camera;
                else
                    sceneCam = windowState.gameCamera;
                switch (info.rangeType)
                {
                    case eRangeType.Sphere:
                        {
                            CustomGizmoUtil.DrawSphere(center, info.radius, gizmoColor, 16, gizmoStyle, true, 0, sceneCam);
                        }
                        break;
                    case eRangeType.Box:
                        {
                            CustomGizmoUtil.DrawBox(center, gizmoRot, info.size, gizmoColor, gizmoStyle, true, 0, sceneCam);
                        }
                        break;
                    case eRangeType.Capsule:
                        {
                            CustomGizmoUtil.DrawCapsule(center, gizmoRot, info.size.z, info.radius, 16, 24, gizmoColor, gizmoStyle, true, 0, sceneCam);
                        }
                        break;
                    case eRangeType.Ray:
                        {
                            Vector3 dir = followParentRot ? windowState.SelectedController.transform.forward : Vector3.forward;
                            dir = infoRot * dir;
                            float dist = info.radius == 0 ? 1000 : info.radius; // Vector3.Distance(from, to);

                            CustomGizmoUtil.DrawArrow(center, dir, dist, 0.5f, gizmoColor, gizmoStyle, true, 0, sceneCam);
                        }
                        break;
                }
            }
        }


#if UNITY_EDITOR
        public void Inspector_OnPropertiesModified()
        {
            endTime = lifeTime > 0 ? startTime + lifeTime : endTime;
        }

        public virtual int Inspector_GetLayerMask(eTargetType targetType) { return 0; }
#endif
    }
}
#endif