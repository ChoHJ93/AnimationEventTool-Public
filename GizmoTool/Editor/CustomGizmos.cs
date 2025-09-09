using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Style = GizmoTool.CustomGizmoUtil.Style;
#endif

namespace GizmoTool
{

    public class CustomGizmos : MonoBehaviour
    {
#if UNITY_EDITOR
        public enum ArrowDirection
        {
            None,
            Forward,
            Backward,
            Up,
            Down,
            Left,
            Right
        }

        [Serializable]
        public class ArrowGizmoInfo
        {
            private enum eRootTrType 
            {
                Default,
                MeshFilter,
                SkinnedMesh
            }
            #region Values for Custom
            public bool bIsShowTargetObjecField = false;
            public GameObject targetObject;

            public Color color = Color.white;

            public bool m_bUseCustomDirection = false;
            public bool m_bLocalDirection = false;
            public ArrowDirection m_vSimpleDirection = ArrowDirection.None;
            public Vector3 m_vCustomDir = Vector3.forward;
            //private Vector3 m_vDirection = Vector3.forward; // default value

            public float m_fLength = 1f;
            public float m_size = 1f;
            public Style m_style = Style.SmoothShaded;
            #endregion

            private Transform m_rootTr = null;

            #region Values for Init
            private bool rootTrInitialized = false;
            private eRootTrType rootTrType = eRootTrType.Default;
            private MeshFilter m_rootTr_Meshfilter = null;
            private SkinnedMeshRenderer m_rootTr_SkinnedMesh = null;
            #endregion
            public void InitRootTrType(Transform rootTr) 
            {
                SkinnedMeshRenderer mySkinnedMesh = rootTr.GetComponent<SkinnedMeshRenderer>();
                MeshFilter myMeshFilter = rootTr.GetComponent<MeshFilter>();
                
                rootTrType              = mySkinnedMesh ? eRootTrType.SkinnedMesh : myMeshFilter? eRootTrType.MeshFilter : eRootTrType.Default;
                m_rootTr                = rootTr;
                m_rootTr_Meshfilter     = rootTrType == eRootTrType.MeshFilter ? myMeshFilter : null;
                m_rootTr_SkinnedMesh    = rootTrType == eRootTrType.SkinnedMesh ? mySkinnedMesh : null;

                rootTrInitialized = true;
            }

            public Vector3 GetStartPosition(Transform rootTr)
            {
                if (rootTrInitialized == false)
                    InitRootTrType(rootTr);

                switch (rootTrType)
                {
                    case eRootTrType.MeshFilter:
                        return m_rootTr.TransformPoint(m_rootTr_Meshfilter.sharedMesh.bounds.center);

                    case eRootTrType.SkinnedMesh:
                        return m_rootTr.TransformPoint(m_rootTr_SkinnedMesh.bounds.center);

                    default:
                    case eRootTrType.Default:
                        return m_rootTr.transform.position;
                }
            }

            public Vector3 GetDirection(Transform rootTr)
            {
                Transform objectTr = bIsShowTargetObjecField && targetObject ? targetObject.transform : rootTr;
                Vector3 retrunDir = Vector3.forward;
                if (rootTr == null)
                    return Vector3.forward;

                if (m_bUseCustomDirection)
                {
                    retrunDir = m_bLocalDirection ? objectTr.transform.TransformDirection(m_vCustomDir) : m_vCustomDir;
                }
                else
                {
                    switch (m_vSimpleDirection)
                    {
                        case ArrowDirection.Forward:
                            retrunDir = m_bLocalDirection ? objectTr.forward : Vector3.forward;
                            break;
                        case ArrowDirection.Backward:
                            retrunDir = m_bLocalDirection ? -objectTr.forward : Vector3.back;
                            break;
                        case ArrowDirection.Up:
                            retrunDir = m_bLocalDirection ? objectTr.up : Vector3.up;
                            break;
                        case ArrowDirection.Down:
                            retrunDir = m_bLocalDirection ? -objectTr.up : Vector3.down;
                            break;
                        case ArrowDirection.Right:
                            retrunDir = m_bLocalDirection ? objectTr.right : Vector3.right;
                            break;
                        case ArrowDirection.Left:
                            retrunDir = m_bLocalDirection ? -objectTr.right : Vector3.left;
                            break;


                        case ArrowDirection.None:
                        default:
                            retrunDir = Vector3.forward; break;
                    }
                }

                return retrunDir;
            }

        }

        //[SerializeField] bool isLocalTr = false;

        private bool[] bShowDirArray = new bool[] { false, false, false };

        //[SerializeField]
        public List<ArrowGizmoInfo> m_ArrowGizmoInfoList = new List<ArrowGizmoInfo>();

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
#if UNITY_EDITOR
            SceneView.duringSceneGui += DuringSceneGUI;
#endif
        }

        private static void DuringSceneGUI(SceneView sceneView)
        {
#if UNITY_EDITOR
            CustomGizmos[] targetArray = FindObjectsOfType<CustomGizmos>(false);
            if (targetArray.Length > 0)
            {
                foreach (CustomGizmos target in targetArray)
                {
                    if (target.enabled)
                        target.DrawwArrows_Custom();//DrawArrow_Mesh();
                }
            }
#endif
        }

        void OnEnable()
        {
        }

        void DrawwArrows_Custom()
        {
#if UNITY_EDITOR
            if (m_ArrowGizmoInfoList != null && m_ArrowGizmoInfoList.Count > 0)
            {
                foreach (ArrowGizmoInfo gizmoInfo in m_ArrowGizmoInfoList)
                {
                    Vector3 startPos = gizmoInfo.GetStartPosition(transform);
                    Vector3 dir = gizmoInfo.GetDirection(transform).normalized;
                    Vector3 endPos = startPos + dir * gizmoInfo.m_fLength;

                    CustomGizmoUtil.DrawArrow(startPos, endPos, gizmoInfo.m_size * 0.15f, gizmoInfo.m_size * 0.4f, 14, gizmoInfo.m_size * 0.1f, gizmoInfo.color, gizmoInfo.m_style);
                }
            }
#endif
        }
#endif
    }
}

