using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool
{
    [System.Serializable]
    public abstract class MoveEventBase : AniEventBase
    {

        public bool usePhysics;
        public Vector3 direction;
        /// <summary>
        /// moveType == eMoveType.Trace �� ���� distance�� speed������ ���
        /// </summary>
        public float distance;
        public float simpleSpeed => duration == 0 ? 0 : distance / duration;
    }
}
