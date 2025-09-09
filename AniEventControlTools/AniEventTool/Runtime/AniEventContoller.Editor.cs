#if UNITY_EDITOR
namespace AniEventTool
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    using UnityEditor;
    using UnityEditor.Animations;
    using GizmoTool;

    [RequireComponent(typeof(Animator))]
    public partial class AniEventController<T> where T : MonoBehaviour
    {
        public override Dictionary<AnimInfo, List<AniEventGroup>> Editor_GetAniEventDic => m_dicAniEvent;
        /// <summary>
        /// *Editor Only
        /// </summary>
        [System.Serializable]
        public class MeshInfo
        {
            public SkinnedMeshRenderer skinnedMesh;
            public Mesh bakedMesh;
            public MeshRenderer mesh;
            public MeshFilter meshFilter;
            public Matrix4x4 matTRS;
            public Material[] materials;
            public Vector3 scale;
        }

        private Transform m_transform;
        private Animator m_animator;

        private List<AniStateInfo> m_aniStateInfoList = new List<AniStateInfo>();
        public override List<AniStateInfo> Editor_GetAniStateInfoList => m_aniStateInfoList;

        private List<MeshInfo> m_listDrawMesh = new List<MeshInfo>();
        private List<string> m_listBoneName = new List<string>();
        private Dictionary<string, string> m_dicBonePaths = new Dictionary<string, string>();
        private Vector3 m_stepForwardPos = Vector3.zero;
        private Vector3 m_pushedPos = Vector3.zero;
        private Vector3 lastEventEndPos = Vector3.zero;

        private Transform m_root = null;
        protected MoveEventBase editor_curMoveEvent = null;

        [HideInInspector] public string GroupKey = string.Empty;
        public override Transform Editor_RootBoneTr => m_root;

        private void OnGUI()
        {
            return;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);
            screenPos.y = Screen.height - screenPos.y;

            GUI.Label(new Rect(screenPos.x, screenPos.y, 200, 50), m_curAniTime.ToString());
        }
        public override void Editor_SetDrawMeshInfo()
        {
            m_listDrawMesh.Clear();

            m_stepForwardPos = Vector3.zero;
            m_pushedPos = Vector3.zero;

            SkinnedMeshRenderer[] skinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshes.Length > 0)
            {
                for (int i = 0; i < skinnedMeshes.Length; i++)
                {
                    SkinnedMeshRenderer skinnedMesh = skinnedMeshes[i];
                    if (skinnedMesh == null || skinnedMesh.enabled == false)
                        continue;

                    MeshInfo meshInfo = new MeshInfo();
                    meshInfo.skinnedMesh = skinnedMesh;
                    meshInfo.bakedMesh = new Mesh();
                    meshInfo.materials = skinnedMesh.sharedMaterials;
                    meshInfo.scale = skinnedMesh.transform.localScale;

                    m_listDrawMesh.Add(meshInfo);
                }
            }

            MeshRenderer[] meshes = GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < meshes.Length; i++)
            {
                MeshRenderer mesh = meshes[i];
                if (mesh == null || mesh.enabled == false)
                    continue;

                MeshInfo meshInfo = new MeshInfo();
                meshInfo.mesh = mesh;
                meshInfo.meshFilter = mesh.GetComponent<MeshFilter>();
                meshInfo.materials = mesh.GetComponent<Renderer>().sharedMaterials;
                meshInfo.scale = mesh.transform.localScale;

                m_listDrawMesh.Add(meshInfo);
            }
        }
        public override void Editor_DrawMesh(PreviewRenderUtility previewRenderUtility, int targetLayer)
        {
            if (m_listDrawMesh.IsNullOrEmpty())
            {
                Debug.Log($"No MeshInfo to Draw");
                return;
            }

            foreach (MeshInfo meshInfo in m_listDrawMesh)
            {
                if (meshInfo == null)
                    continue;

                SkinnedMeshRenderer skinnedMesh = meshInfo.skinnedMesh;
                if (skinnedMesh != null)
                {
                    meshInfo.matTRS.SetTRS(skinnedMesh.transform.position + m_stepForwardPos + m_pushedPos, skinnedMesh.transform.rotation, meshInfo.scale);
                    skinnedMesh.BakeMesh(meshInfo.bakedMesh);

                    int subMeshCount = meshInfo.bakedMesh.subMeshCount;
                    for (int i = 0; i < subMeshCount; i++)
                    {
                        //�ӽ�
                        Material newMaterialInstance = new Material(meshInfo.materials[i]);
                        if (skinnedMesh.name.Contains("face", StringComparison.OrdinalIgnoreCase) && newMaterialInstance.HasProperty("_IsEye"))
                            newMaterialInstance.SetInt("_IsEye", 1);
                        //

                        Graphics.DrawMesh(meshInfo.bakedMesh, meshInfo.matTRS, newMaterialInstance, targetLayer, previewRenderUtility.camera);
                    }
                }

                MeshRenderer mesh = meshInfo.mesh;
                if (mesh != null)
                {
                    meshInfo.matTRS.SetTRS(mesh.transform.position + m_stepForwardPos + m_pushedPos, mesh.transform.rotation, Vector3.one);//meshInfo.scale);
                    Graphics.DrawMesh(meshInfo.meshFilter.sharedMesh, meshInfo.matTRS, meshInfo.materials[0], targetLayer, previewRenderUtility.camera);
                }

            }

            foreach (MeshInfo meshInfo in m_listDrawMesh)
            {


            }
        }
        public override List<string> GetBoneNameList => m_listBoneName;
        public override string Editor_GetBonePath(string boneName) { return m_dicBonePaths[boneName]; }
        public override void Editor_SetBoneInfo()
        {
            m_dicBone.Clear();
            m_dicBonePaths.Clear();

            m_listBoneName.Clear();
            m_listBoneName.Add("None");

            if (m_transform == null)
                m_transform = GetComponent<Transform>();
            else
                m_transform = transform;


            m_root = m_transform.FindChildAtDepthWithName(1, "Root");
            if (m_root == null)
                m_root = transform.Find("Bip001");

            if (m_root != null)
            {
                Transform[] bones = m_root.GetComponentsInChildren<Transform>();
                for (int i = 0; i < bones.Length; i++)
                {
                    string boneName = bones[i].name;
                    if (m_dicBone.ContainsKey(bones[i].name))
                    {
                        Debug.Log($"<color=yellow>{m_root.parent.gameObject.name} ���Ͽ� �ߺ��� �̸��� ���� �����մϴ�! -> {boneName}\n�𵨸� ���ҽ��� �� �̸� ������ �ʿ��մϴ�.</color>");
                        continue;
                    }
                    m_dicBone.Add(boneName, bones[i]);
                    m_listBoneName.Add(boneName);

                    string bonePath = GetBonePath(bones[i]);
                    m_dicBonePaths.Add(boneName, bonePath);
                }
            }
        }
        private string GetBonePath(Transform bone)
        {
            string path = bone.name;

            if (bone.Equals(m_root))
                return path;

            while (bone.parent != null)//bone.parent != m_transform)
            {
                bone = bone.parent;
                path = bone.name + "/" + path;
                if (bone.Equals(m_root))
                    break;
            }
            return path;
        }
        public override string Editor_GetBoneName(int index)
        {
            if (index < 0 || index >= m_listBoneName.Count)
                return null;

            string boneName = m_listBoneName[index];
            return boneName;
        }
        public override Transform Editor_GetBone(int index)
        {
            string boneName = Editor_GetBoneName(index);
            if (string.IsNullOrEmpty(boneName) == true)
                return null;

            if (m_dicBone.ContainsKey(boneName) == false)
                return null;

            return m_dicBone[boneName];
        }
        public override Transform Editor_GetBone(string boneName)
        {
            if (m_dicBone.ContainsKey(boneName) == false)
                return null;

            return m_dicBone[boneName];
        }
        public override void Editor_GetAnimations()
        {
            if (m_animator == null)
                m_animator = GetComponent<Animator>();


            m_aniStateInfoList.Clear();
            AnimatorController ac = m_animator.runtimeAnimatorController as AnimatorController;
            foreach (var layer in ac.layers)
            {
                ProcessStateMachine(layer.stateMachine);
            }
            m_aniStateInfoList.Sort((info1, info2) => info1.stateName.CompareTo(info2.stateName));


            if (m_dicAniEvent.Count <= 0)
            {
                foreach (AniStateInfo info in m_aniStateInfoList)
                {
                    List<AniEventGroup> eventList = new List<AniEventGroup>();

                    AnimInfo aniInfo = new AnimInfo();
                    aniInfo.stateName = info.stateName;
                    aniInfo.clipName = info.clip.name;
                    aniInfo.endTime = info.clip.length; //* clip.frameRate;
                    aniInfo.cutFrame = 0f;

                    m_dicAniEvent.Add(aniInfo, eventList);
                }
            }
        }
        public override bool Editor_GetAllEventGroups(out List<AniEventGroup> allEventGroups)
        {
            allEventGroups = new List<AniEventGroup>();

            if (m_dicAniEvent == null || m_dicAniEvent.Count == 0)
                return false;

            foreach (List<AniEventGroup> eventGroups in m_dicAniEvent.Values)
            {
                if (eventGroups.IsNullOrEmpty() == false)
                    eventGroups.Sort((group1, group2) => group1.index.CompareTo(group2.index));
                allEventGroups.AddRange(eventGroups);
            }
            return true;
        }
        void ProcessStateMachine(AnimatorStateMachine stateMachine)
        {
            foreach (var state in stateMachine.states)
            {
                AnimationClip clip = state.state.motion as AnimationClip;
                if (clip != null)
                {
                    m_aniStateInfoList.Add(new AniStateInfo(state.state.name, clip));
                }
            }

            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                ProcessStateMachine(subStateMachine.stateMachine);
            }
        }
        public override int Editor_GetAniStateCount => m_aniStateInfoList.Count;
        public override string[] Editor_GetAniStateNames()
        {
            int count = Editor_GetAniStateCount;
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
            {
                names[i] = m_aniStateInfoList[i].stateName;
            }
            return names;
        }
        public override string[] Editor_GetAnimationNames()
        {
            int count = m_aniStateInfoList.Count;
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
            {
                names[i] = m_aniStateInfoList[i].clip.name;
            }
            return names;
        }
        public override bool Editor_TryGetAnimationClip(int selection, out AnimationClip animationClip)
        {
            animationClip = null;
            if (selection < 0 || selection >= m_aniStateInfoList.Count)
                return false;

            animationClip = m_aniStateInfoList[selection].clip;
            return true;
        }
        private bool Editor_TryGetAniInfo(int selection, out AnimInfo animInfo)
        {
            animInfo = null;
            foreach (KeyValuePair<AnimInfo, List<AniEventGroup>> keyValue in m_dicAniEvent)
            {
                if (keyValue.Key.stateName.Equals(m_aniStateInfoList[selection].stateName))
                {
                    animInfo = keyValue.Key;
                    return true;
                }
            }
            return false;
        }
        public override List<AniEventGroup> Editor_GetEventList(int selection)
        {
            foreach (KeyValuePair<AnimInfo, List<AniEventGroup>> keyValue in m_dicAniEvent)
            {
                if (keyValue.Key.stateName.Equals(m_aniStateInfoList[selection].stateName))
                {
                    return m_dicAniEvent[keyValue.Key];
                }
            }
            return null;
        }
        public override AniEventGroup Editor_AddEventGroup(int selection)
        {
            AnimInfo animInfo;
            if (Editor_TryGetAniInfo(selection, out animInfo) == false)
            {
                Debug.LogError($"ĳ���� �̺�Ʈ ������ : �ִϸ��̼� Ŭ���� �ش��ϴ� �̺�Ʈ�� �о�� �� �����ϴ�.");
                return null;
            }

            AnimationClip clip;
            if (Editor_TryGetAnimationClip(selection, out clip) == false)
            {
                Debug.LogError($"ĳ���� �̺�Ʈ ������ : �ִϸ��̼� Ŭ���� �о�� �� �����ϴ�.");
                return null;
            }

            AniEventGroup animEventGroup = new AniEventGroup();
            m_dicAniEvent[animInfo].Add(animEventGroup);

            return animEventGroup;
        }
        public override bool Editor_GetValidStateInfo(int selection, out AnimInfo animInfo, out AniStateInfo stateInfo)
        {
            stateInfo = null;
            if (Editor_TryGetAniInfo(selection, out animInfo) == false)
            {
                Debug.LogError($"ĳ���� �̺�Ʈ ������ : �ִϸ��̼� Ŭ���� �ش��ϴ� �̺�Ʈ�� �о�� �� �����ϴ�.");
                return false;
            }

            AnimationClip clip;
            if (Editor_TryGetAnimationClip(selection, out clip) == false)
            {
                Debug.LogError($"ĳ���� �̺�Ʈ ������ : �ִϸ��̼� Ŭ���� �о�� �� �����ϴ�.");
                return false;
            }

            stateInfo = m_aniStateInfoList[selection];
            return true;
        }
        public override void Editor_SetAllEventsData(AnimInfo key, List<AniEventGroup> value)
        {
            if (key == null)
            {
                Debug.LogError($"<color=orange>Animation State ������ Null �Դϴ�.</color>");
                return;
            }

            var searchResult = m_dicAniEvent.FirstOrDefault(kvp => kvp.Key.stateName.Equals(key.stateName));
            AnimInfo matchedInfo = searchResult.Key;
            if (matchedInfo == null || matchedInfo == default)
            {
                Debug.LogError($"<color=yellow>Animation State ������ ��ġ���� �ʽ��ϴ�. {key.stateName} Animation State ������ ã�� �� �����ϴ�.</color>");
                return;
            }
            else if (matchedInfo.clipName.Equals(key.clipName) == false)
            {
                Debug.LogError($"<color=yellow>Animation State ������ ��ġ���� �ʽ��ϴ�. {key.stateName} State�� Clip ������ �ٸ��ϴ�.</color>");
                return;
            }

            if (searchResult.Equals(default(KeyValuePair<AnimInfo, List<AniEventGroup>>)))
            {
                m_dicAniEvent.Add(matchedInfo, value);
            }
            else
            {
                m_dicAniEvent[matchedInfo] = value;
            }
        }

        public override void Editor_Release()
        {
            m_dicAniEvent.Clear();
            m_animator = null;
            m_aniStateInfoList.Clear();
        }

        #region Movement

        public override void Editor_SetMoveEvent(MoveEventBase moveEvent, Vector3 lastEndPos)
        {
            if (moveEvent == null)
            {
                transform.position = lastEndPos;
                transform.rotation = Quaternion.identity;
                editor_curMoveEvent = null;
                return;
            }

            if (editor_curMoveEvent == null)
                editor_curMoveEvent = Activator.CreateInstance(moveEvent.GetType()) as MoveEventBase;
            //copy
            editor_curMoveEvent.startTime = moveEvent.startTime;
            editor_curMoveEvent.endTime = moveEvent.endTime;
            editor_curMoveEvent.usePhysics = moveEvent.usePhysics;
            editor_curMoveEvent.direction = moveEvent.direction;
            editor_curMoveEvent.distance = moveEvent.distance;

            lastEventEndPos = lastEndPos;
        }
        public override void Editor_UpdateTransform(float currentTime)
        {
            if (editor_curMoveEvent != null)
            {
                float eventTime = Mathf.Min(currentTime - editor_curMoveEvent.startTime, editor_curMoveEvent.endTime);
                float time = Mathf.Max(0, eventTime);
                Vector3 dir = editor_curMoveEvent.direction.normalized;
                float speed = editor_curMoveEvent.simpleSpeed;
                float duration = editor_curMoveEvent.duration;
                Editor_UpdateTransform_Constant(time, dir, speed, duration);
            }
            //else if (�� ���ӵ� ��� ��)
            //{
            //    float moveSpeed = ;
            //    float reduceDelta = ;
            //    float duration = ;

            //    Editor_UpdateTransform_UniAccele(currentTime, transform.forward, moveSpeed, reduceDelta, duration);
            //}
        }

        public override void Editor_ResetTransform()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// ��ӿ
        /// </summary>
        /// <param name="time"></param>
        /// <param name="moveSpeed">���� �ӵ�</param>
        /// <param name="duration">�̵��ð� (* �ð��� �� �Ǳ� �� �ӵ��� 0�� �����ϸ� ����)</param>
        protected void Editor_UpdateTransform_Constant(float time, Vector3 direction, float moveSpeed, float duration)
        {
            float t = Mathf.Min(time, duration);
            float movedDistance = CustomMathUtils.GetDistanceAtTime(moveSpeed, t);
            transform.position = lastEventEndPos + transform.TransformDirection(direction.normalized) * movedDistance;

        }

        /// <summary>
        /// ��ӵ� �̵�
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="moveSpeed">���� �ӵ�</param>
        /// <param name="reduceDelta">fixed update �� ƽ�� �ӵ� ��ȭ��</param>
        /// <param name="duration">�̵� �ð� (* �ð��� �� �Ǳ� �� �ӵ��� 0�� �����ϸ� ����)</param>
        protected void Editor_UpdateTransform_UniAccele(float time, Vector3 direction, float moveSpeed, float duration, float reduceDelta)
        {
            float t = Mathf.Min(time, duration);
            float movedDistance = CustomMathUtils.GetDistanceAtTime(moveSpeed, reduceDelta, t);
            transform.position = lastEventEndPos + transform.TransformDirection(direction.normalized) * movedDistance;
        }
        #endregion

        #region Gizmo
        protected IEnumerator DrawRangeGizmoRoutine(HitEvent evt, float duration)
        {
            bool attach = evt.attach;
            bool followParentRot = evt.followParentRot;
            float time = 0;
            List<RangeInfo> rangeInfos = new List<RangeInfo>(evt.ranges);

            Transform controllerTr = ControllerTr;
            Vector3 playerPos = controllerTr.position;
            Quaternion playerRot = controllerTr.rotation;
            Vector3 forward = controllerTr.forward;
            while (time < duration)
            {
                foreach (RangeInfo info in rangeInfos)
                {
                    DrawGizmo(playerPos, playerRot, forward, attach, followParentRot, info);
                }
                time += Time.deltaTime;
                yield return null;
            }
            yield return null;
        }

        private void DrawGizmo(Vector3 playerPos, Quaternion playerRot, Vector3 PlayerForward, bool attach, bool followParentRot, RangeInfo info)
        {
            Color gizmoColor = new Color(1, 0, 0, 1);
            CustomGizmoUtil.Style gizmoStyle = CustomGizmoUtil.Style.Wireframe;

            playerPos = attach ? playerPos : m_aniStartPos;
            playerRot = attach ? playerRot : Quaternion.Euler(m_aniStartRot);
            Vector3 center;
            if (attach)
                center = followParentRot ? LocalToWorld(info.center, playerPos, playerRot) : playerPos + info.center;
            else
                center = playerPos + (followParentRot ? playerRot * info.center : info.center);
            Quaternion infoRot = Quaternion.Euler(info.rotation);
            Quaternion gizmoRot = followParentRot ? playerRot * infoRot : infoRot;

            switch (info.rangeType)
            {
                case eRangeType.Sphere:
                    {
                        CustomGizmoUtil.DrawSphere(center, info.radius, gizmoColor, 16, gizmoStyle, true);
                    }
                    break;
                case eRangeType.Box:
                    {
                        CustomGizmoUtil.DrawBox(center, gizmoRot, info.size, gizmoColor, gizmoStyle, true);
                    }
                    break;
                case eRangeType.Capsule:
                    {
                        CustomGizmoUtil.DrawCapsule(center, gizmoRot, info.size.z, info.radius, 16, 24, gizmoColor, gizmoStyle, true);
                    }
                    break;
                case eRangeType.Ray:
                    {
                        Vector3 dir = followParentRot ? PlayerForward : Vector3.forward;
                        dir = infoRot * dir;
                        float dist = info.radius == 0 ? 1000 : info.radius; // Vector3.Distance(from, to);

                        Vector3 to = center + dir * dist;
                        float coneRadius = 0.5f * 0.15f;
                        float coneHeight = 0.5f * 0.4f;
                        int numSegments = 8;
                        float stemThickness = 0.5f * 0.1f;
                        CustomGizmoUtil.DrawArrow(center, to, coneRadius, coneHeight, numSegments, stemThickness, gizmoColor, gizmoStyle, true);
                    }
                    break;
            }
        }

        //private void DrawDetectRangeGizmo(MoveEvent moveEvent) { }
        protected IEnumerator DrawDestPos(Transform targetTr, Vector3 addedPosition, float duration)
        {
            if (targetTr == null)
                yield break;

            Color gizmoColor = new Color(0, 1, 1, 1);
            CustomGizmoUtil.Style gizmoStyle = CustomGizmoUtil.Style.SmoothShaded;
            float time = 0;
            duration = Mathf.Max(1f, duration);
            while (time < duration)
            {
                Vector3 dest = targetTr.position + addedPosition;
                CustomGizmoUtil.DrawSphere(dest, 0.2f, gizmoColor, 16, gizmoStyle, true);
                time += Time.deltaTime;
                yield return null;
            }
        }
        #endregion

        #region PlayMode_On_Editor
        protected void Editor_InitOnPlayMode()
        {
        }
        protected void Editor_CacheAniStateNames()
        {
            m_dicAniStateNames.Clear();
            if (m_animator == null)
                m_animator = GetComponent<Animator>();

            AnimatorController ac = m_animator.runtimeAnimatorController as AnimatorController;
            foreach (var layer in ac.layers)
            {
                Editor_RegisterAllStateNames(layer.stateMachine);
            }
            m_animator = null;
        }
        private void Editor_RegisterAllStateNames(AnimatorStateMachine stateMachine)
        {
            foreach (var state in stateMachine.states)
            {
                m_dicAniStateNames.Add(state.state.nameHash, state.state.name);
            }

            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                Editor_RegisterAllStateNames(subStateMachine.stateMachine);
            }

        }
        protected void OnAniStateChanged_Editor(AnimatorStateInfo nextStateInfo)
        {
            if (Application.isPlaying == false)
                return;

            if (m_dicAniStateNames.ContainsKey(nextStateInfo.shortNameHash) == false)
                return;

            string stateName = m_dicAniStateNames[nextStateInfo.shortNameHash];
            //Debug.Log("<color=yellow>�ִϸ��̼� ������Ʈ ����� �Ǵ� ��� �ð� �ʱ�ȭ��!</color> ���� ������Ʈ: " + stateName);
        }
        #endregion
    }
}
#endif