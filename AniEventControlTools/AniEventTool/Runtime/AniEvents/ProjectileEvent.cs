using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool
{

    [System.Serializable]
    public class ProjectileEvent : AniEventBase
    {
        [SerializeField] public GameObject prefab;

        [SerializeField] public string boneName;
        [SerializeField] public Vector3 addedPosition;
        [SerializeField] public Vector3 addedRotation;
        [SerializeField] public bool attach;
        [SerializeField] public bool startOnBone;
        [SerializeField] public bool followParentRot;
        [SerializeField] public bool keep;

        override public bool IsValidEventData => prefab != null;


#if UNITY_EDITOR
#endif
    }

}
