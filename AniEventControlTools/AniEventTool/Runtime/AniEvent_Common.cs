using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace AniEventTool
{
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AnimInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string stateName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string clipName;

        public float endTime;
        public float cutFrame;

        public bool useRootMotion;
    }


#if UNITY_EDITOR
    public class AniStateInfo
    {
        public string stateName;
        public AnimationClip clip;
        public bool useRootMotion;

        public AniStateInfo(string stateName, AnimationClip clip)
        {
            this.stateName = stateName;
            this.clip = clip;
            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            useRootMotion = !clipSettings.keepOriginalPositionXZ && !clipSettings.loopBlendPositionXZ;
        }
    }
#endif
    public enum eAniStateExitType : byte
    {
        None,
        Transit,
        Forced,
        Replay
    }

    public enum eGameEventType : byte
    {
        None,
        NextSkill,
        EnableMove,
    }

    public enum eTargetType : byte
    {
        None,
        Player,
        Other,
        Custom
    }
    public enum eRangeType : byte
    {
        Ray,
        Sphere,
        Box,
        Capsule
    }

}