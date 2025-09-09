using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool
{
    public abstract class MoveEventTrackBase : AniEventTrack<MoveEventBase>
    {
        [SerializeField] public Vector3 direction;
        [SerializeField] protected MoveEventBase editedEvent;
        [SerializeField] public Vector3 endPosition = Vector3.zero;
        [SerializeField] public float distance;
        public Transform controllerTr => windowState?.SelectedController.transform;
        public float simpleSpeed => duration == 0 ? 0 : distance / duration;
        public virtual MoveEventBase EditedEvent => editedEvent;
        
    }
}
