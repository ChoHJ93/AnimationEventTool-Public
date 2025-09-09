#if UNITY_EDITOR
namespace AniEventTool.Editor.Dev
{
    using System.Collections;
    using System.Collections.Generic;
    using System;
    using System.Reflection;

    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Animations;

    using AniEventTool.Editor;

    using Object = UnityEngine.Object;

    public static class PatchFunctions
    {
        private class AniStateInfo
        {
            public string stateName;
            public AnimationClip clip;

            public AniStateInfo(string stateName, AnimationClip clip)
            {
                this.stateName = stateName;
                this.clip = clip;
            }
        }



        //public static void Dev_ReplaceHitTypeGameEventToHitEvent()
        //{
        //    // Get all prefabs in the Resources folder
        //    var allPrefabs = Resources.LoadAll<GameObject>("");

        //    foreach (var prefab in allPrefabs)
        //    {
        //        // Check if the prefab has an AniEventController component
        //        var aniEventController = prefab.GetComponent<AniEventController>();
        //        if (aniEventController != null)
        //        {
        //            string path = AniEventToolPreferences.JSONFilePath + aniEventController.name + ".json";
        //            aniEventController.Editor_GetAnimations();
        //            aniEventController.Editor_LoadEventFile(path);
        //            //copy hitType game event's value to hit event. and add hit event to hit event list
        //            foreach (var aniEventGroupList in aniEventController.Editor_GetAniEventDic.Values)
        //            {
        //                foreach (var evtGroup in aniEventGroupList)
        //                {
        //                    if (evtGroup.gameEvents.IsNullOrEmpty())
        //                        continue;
        //                    if (evtGroup.hitEvents.IsNullOrEmpty())
        //                        evtGroup.hitEvents = new List<HitEvent>();
        //                    foreach (var gameEvent in evtGroup.gameEvents)
        //                    {
        //                        if (gameEvent.eventType == eGameEventType.Hit)
        //                        {
        //                            var hitEvent = new HitEvent();
        //                            hitEvent.groupId = gameEvent.groupId;
        //                            hitEvent.index = gameEvent.index;
        //                            hitEvent.name = gameEvent.name;
        //                            hitEvent.startTime = gameEvent.startTime;
        //                            hitEvent.endTime = gameEvent.endTime;
        //                            hitEvent.layerMask = gameEvent.layerMask;
        //                            hitEvent.targetType = gameEvent.targetType;
        //                            hitEvent.targetCount = gameEvent.targetCount;
        //                            hitEvent.hitCount = gameEvent.hitCount;
        //                            hitEvent.durationPerHit = gameEvent.durationPerHit;
        //                            hitEvent.lifeTime = gameEvent.lifeTime;
        //                            hitEvent.attach = gameEvent.attach;
        //                            hitEvent.followParentRot = gameEvent.followParentRot;
        //                            hitEvent.ranges = gameEvent.ranges;

        //                            evtGroup.hitEvents.Add(hitEvent);
        //                        }
        //                    }
        //                }
        //            }
        //            CommonUtil.SaveEventFileToJSON(aniEventController, path);
        //        }
        //    }
        //}

        //public static void Dev_MinusOneValueOfGameEventType()
        //{
        //    // Get all prefabs in the Resources folder
        //    var allPrefabs = Resources.LoadAll<GameObject>("");

        //    foreach (var prefab in allPrefabs)
        //    {
        //        // Check if the prefab has an AniEventController component
        //        var aniEventController = prefab.GetComponent<AniEventController>();
        //        if (aniEventController != null)
        //        {
        //            string path = AniEventToolPreferences.JSONFilePath + aniEventController.name + ".json";
        //            aniEventController.Editor_GetAnimations();
        //            aniEventController.Editor_LoadEventFile(path);
        //            //copy hitType game event's value to hit event. and add hit event to hit event list
        //            foreach (var aniEventGroupList in aniEventController.Editor_GetAniEventDic.Values)
        //            {
        //                foreach (var evtGroup in aniEventGroupList)
        //                {
        //                    if (evtGroup.gameEvents.IsNullOrEmpty())
        //                        continue;
        //                    foreach (var gameEvent in evtGroup.gameEvents)
        //                    {
        //                        if (gameEvent.eventType != eGameEventType.LookPlayer)
        //                            gameEvent.eventType -= 1;
        //                    }
        //                }
        //            }
        //            CommonUtil.SaveEventFileToJSON(aniEventController, path);
        //        }
        //    }
        //}

        private static void GetAllStateInfos(AnimatorController animatorController, out List<AniStateInfo> stateInfos)
        {
            stateInfos = new List<AniStateInfo>();

            foreach (var layer in animatorController.layers)
            {
                var stateMachine = layer.stateMachine;
                GetStateInfoRecursively(stateMachine, stateInfos);
            }

            stateInfos.Sort((x, y) => x.stateName.CompareTo(y.stateName));
        }

        private static void GetStateInfoRecursively(AnimatorStateMachine stateMachine, List<AniStateInfo> stateInfos)
        {
            foreach (var state in stateMachine.states)
            {
                if (state.state.motion == null)
                    continue;

                if (state.state.motion is AnimationClip clip)
                {
                    stateInfos.Add(new AniStateInfo(state.state.name, clip));
                }
            }

            foreach (var childStateMachine in stateMachine.stateMachines)
            {
                GetStateInfoRecursively(childStateMachine.stateMachine, stateInfos);
            }
        }
        public static void ChangeEventsAddedTransformValue_RootMotionAni()
        {
            WindowState windowState = AniEventToolWindow.Instance.State;
            AniEventControllerBase controller = windowState.SelectedController;
            if (windowState.SelectedClip == null || windowState.SelectedController == null)
                return;

            float time = (float)windowState.time;
            bool useRootMotion = windowState.useRootMotion;
            AnimationClip clip = windowState.SelectedClip;
            if (!useRootMotion || clip == null)
                return;

            List<AniEventTrackBase> currentEventTracks = new List<AniEventTrackBase>();
            List<AniEventGroupTrack> curGroupTracks = AniEventToolWindow.Instance.EventGroupTrackList;

            foreach (var groupTrack in curGroupTracks)
            {
                foreach (var eventTrack in groupTrack.ChildEventTracks)
                {
                    currentEventTracks.Add(eventTrack);
                }
            }
            controller.Editor_UpdateTransform(time);
            clip.SampleAnimation(controller.gameObject, time);

            Transform controllerTr = windowState.GetControllerTr(true);

            foreach (var eventTrack in currentEventTracks)
            {
                if (eventTrack is HitEventTrack hitEventTrack)
                {
                    List<RangeInfo> ranges = hitEventTrack.ranges;
                    foreach (var range in ranges)
                    {

                        if (range.rangeType == eRangeType.Ray)
                            continue;

                        range.center -= CustomMathUtils.GetRoundedVector(controllerTr.position);
                        //range.rotation -= GetRoundedVector(controllerTr.rotation.eulerAngles);
                    }

                }
                else if (eventTrack is EffectEventTrack effectEventTrack)
                {
                    Transform particleInstanceTr = effectEventTrack.particleInstance.transform;

                    Vector3 addedposition = particleInstanceTr.position - controllerTr.position;
                    Quaternion relatedRotation = particleInstanceTr.rotation.normalized * Quaternion.Inverse(controllerTr.rotation.normalized);
                    Vector3 addedRotation = relatedRotation.eulerAngles;
                    Type type = typeof(EffectEventTrack);

                    FieldInfo addedPosInfo = type.GetField("addedPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo addedRotInfo = type.GetField("addedRotation", BindingFlags.NonPublic | BindingFlags.Instance);
                    addedposition = CustomMathUtils.GetRoundedVector(addedposition);
                    addedRotation = CustomMathUtils.GetRoundedVector(addedRotation);
                    addedPosInfo.SetValue(effectEventTrack, addedposition);
                    //addedRotInfo.SetValue(effectEventTrack, addedRotation);
                }
            }
        }
    }
}
#endif