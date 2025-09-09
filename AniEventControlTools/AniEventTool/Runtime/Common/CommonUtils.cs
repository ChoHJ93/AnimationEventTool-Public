using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;

    public static class CommonUtil
    {
        [Obsolete]
        /// <summary>
        /// [Editor에서만 사용] baseType의 모든 파생 타입을 가져옵니다. (다단계 상속의 말단만, 추상 제외)
        /// <para>** 상대적으로 가벼움</para>
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="extraExcludes"></param>
        /// <returns></returns>
        public static Type[] GetLeafDerivedTypesInEditor(Type baseType, IEnumerable<Type> extraExcludes = null)
        {
            var excluded = new HashSet<Type>(extraExcludes ?? Array.Empty<Type>())
        {
            baseType,
            typeof(AniEventGroup)
        };

            var all = TypeCache.GetTypesDerivedFrom(baseType);
            var candidates = new HashSet<Type>(all.Where(t => !t.IsAbstract && !excluded.Contains(t)));

            var leafOnly = candidates.Where(t =>
                !candidates.Any(o => !ReferenceEquals(o, t) && t.IsAssignableFrom(o))
            ).ToArray();

            return leafOnly.Length > 0 ? leafOnly : Array.Empty<Type>();
        }

        /// <summary>
        ///  baseType의 모든 파생 타입을 가져옵니다. (다단계 상속의 말단만, 추상 제외) 
        /// <para>** 무거움</para>
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="fromEntireSolution"></param>
        /// <param name="extraExcludes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Type[] GetLeafDerivedTypes(Type baseType, bool fromEntireSolution = true, IEnumerable<Type> extraExcludes = null)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));

            Assembly[] targetAssemblies = fromEntireSolution ? AppDomain.CurrentDomain.GetAssemblies() : new[] { baseType.Assembly };

            var excluded = new HashSet<Type>(extraExcludes ?? Array.Empty<Type>())
        {
            baseType,              // 자기 자신 제외
            typeof(AniEventGroup)
        };

            var candidates = new HashSet<Type>();

            foreach (var asm in targetAssemblies)
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types?.Where(t => t != null).ToArray() ?? Array.Empty<Type>();
                    Debug.LogWarning($"[TypeUtil] Partial type load: {asm.GetName().Name} - {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TypeUtil] Skip assembly: {asm.GetName().Name} - {ex.Message}");
                    continue;
                }

                foreach (Type t in types)
                {
                    if (t.IsAbstract || excluded.Contains(t) || baseType.IsAssignableFrom(t))
                        continue;

                    candidates.Add(t);
                }
            }

            if (candidates.Count == 0)
                return Array.Empty<Type>();

            var leafOnly = candidates.Where(t => !candidates.Any(o => !ReferenceEquals(o, t) && t.IsAssignableFrom(o))).ToArray();

            return leafOnly.Length > 0 ? leafOnly : Array.Empty<Type>();
        }

        /// <summary>
        /// baseType의 모든 파생 타입을 가져옵니다. (다단계 상속 포함, 추상 제외)
        /// <para>** 무거움</para>
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="fromEntireSolution"></param>
        /// <returns></returns>
        public static Type[] GetDerivedTypesFor(Type baseType, bool fromEntireSolution = true)
        {
            Assembly[] targetAssemblies;
            if (fromEntireSolution)
                targetAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            else
                targetAssemblies = new Assembly[] { Assembly.GetAssembly(baseType) };

            Type[] excludedTypes = new[] { baseType, typeof(AniEventGroup) };

            List<Type> derivedTypes = new List<Type>();
            foreach (Assembly assembly in targetAssemblies)
            {
                // GetTypes can throw an exception if an assembly cannot be loaded.
                Type[] types = new Type[] { };
                try
                {
                    types = assembly.GetTypes().Where(baseType.IsAssignableFrom).ToArray();
                }
                catch (ReflectionTypeLoadException e)
                {
                    Debug.LogError(e.Message);
                    types = e.Types; // Get the types that could be loaded (can contain null values).
                }

                foreach (Type type in types)
                {
                    if (type != null && !type.IsAbstract && type.IsSubclassOf(baseType) && !excludedTypes.Contains(type))
                    {
                        //check if type is subclass of one of the derived types


                        if (!derivedTypes.Contains(type))
                            derivedTypes.Add(type);
                    }
                }
            }

            if (derivedTypes.Count == 0)
                return null;

            return derivedTypes.ToArray();
        }

        public static Type GetMatchEventType(Type eventTrackType)
        {
            if (eventTrackType == null)
                return null;

            if (eventTrackType.IsGenericType
                    && eventTrackType.GenericTypeArguments != null
                        && eventTrackType.GenericTypeArguments.Length > 0)
                return eventTrackType.GenericTypeArguments[0];
            else
                return GetMatchEventType(eventTrackType.BaseType);
        }

        public static Type FindTypeByName(string typeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
            return null; // Return null if no type is found
        }
        private static Transform objectPool;
        public static Transform Editor_ObjectPoolTr
        {
            get
            {
                if (objectPool == null)
                    objectPool = GameObject.Find("AEC_ObjectPool")?.transform;
                if (objectPool == null)
                    objectPool = new GameObject("AEC_ObjectPool").transform;

                return objectPool;
            }
        }

        public static bool IsValidEventData<T>(T data) where T : AniEventBase
        {
            if (data == null) return false;

            return data.IsValidEventData;
        }

#if UNITY_EDITOR

        /// <summary>
        /// for Dev Only
        /// </summary>
        /// <param name="eventContoller"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool SaveEventFileToJSON(AniEventControllerBase eventContoller, string path)
        {
            List<(AnimInfo, List<AniEventGroup>)> aniEventList = new List<(AnimInfo, List<AniEventGroup>)>();
            foreach (KeyValuePair<AnimInfo, List<AniEventGroup>> kv in eventContoller.Editor_GetAniEventDic)
            {
                aniEventList.Add((kv.Key, kv.Value));
            }
            return SaveEventFileToJSON(eventContoller.Editor_GetAniStateInfoList, aniEventList, path);
        }

        public static bool SaveEventFileToJSON(List<AniStateInfo> stateInfos, List<(AnimInfo, List<AniEventGroup>)> dicAniEvent, string path)
        {
            JsonObject root = new JsonObject(JsonObject.Type.OBJECT);

            JsonObject eventTypesObj = new JsonObject(JsonObject.Type.ARRAY);
            Type[] eventTypes = GetDerivedTypesFor(typeof(AniEventBase));
            foreach (Type eventType in eventTypes)
            {
                eventTypesObj.Add(eventType.Name);
            }
            root.AddField("EventTypes", eventTypesObj);

            int count = 0;
            foreach (var kv in dicAniEvent)
            {
                if (count >= stateInfos.Count)
                    continue;
                JsonObject aniEventObject = new JsonObject(JsonObject.Type.ARRAY);
                root.AddField(stateInfos[count].stateName, aniEventObject);

                string stateName = stateInfos[count].stateName;
                string clipName = stateInfos[count].clip.name;
                float endTime = stateInfos.Find(info => info.clip.name.Equals(clipName))?.clip.length ?? 0f;
                bool useRootMotion = stateInfos.Find(info => info.clip.name.Equals(clipName))?.useRootMotion ?? false;

                aniEventObject.AddField("StateName", stateName);
                aniEventObject.AddField("ClipName", clipName);
                aniEventObject.AddField("EndTime", endTime);
                aniEventObject.AddField("Use_RootMotion", useRootMotion);

                JsonObject evtListObject = new JsonObject(JsonObject.Type.ARRAY);
                aniEventObject.AddField("Events", evtListObject);

                foreach (AniEventGroup evtGroup in kv.Item2)
                {
                    if (evtGroup.Editor_GetValidEventCount == 0)
                        continue;

                    JsonObject evtObject = new JsonObject(JsonObject.Type.ARRAY);
                    evtListObject.Add(evtObject);

                    evtObject.AddField("EventGroup", evtGroup.eventName); // only for Editor
                    evtObject.AddField("StartTime", evtGroup.startTime);
                    if (evtGroup.endTime > 0f) evtObject.AddField("EndTime", evtGroup.endTime);
                    evtObject.AddField("Index", evtGroup.index);

                    List<AniEventBase> aniEvents = evtGroup.aniEvents;

                    foreach (Type eventType in eventTypes)
                    {
                        if (eventType == null)
                            continue;
                        List<AniEventBase> eventList = evtGroup.GetValidEventList(eventType);
                        evtObject.AddField($"{eventType.Name}Count", eventList.Count);
                        JsonObject jObj = new JsonObject(JsonObject.Type.ARRAY);
                        evtObject.AddField($"{eventType.Name}Datas", jObj);

                        foreach (AniEventBase evt in eventList)
                        {
                            JsonObject evtJObj = new JsonObject(JsonObject.Type.OBJECT);
                            evt.WritePropertiesToJson(ref evtJObj);
                            jObj.Add(evtJObj);
                        }
                    }
                }
                count++;
            }

            string json = root.ToString(true);
            File.WriteAllText(path, json);
            return true;
        }

#endif // UNITY_EDITOR
        public static bool LoadEventDataFromJSON(JsonObject root, out List<(AnimInfo, List<AniEventGroup>)> dicAniEvent)
        {
            dicAniEvent = new List<(AnimInfo, List<AniEventGroup>)>();

            if (root == null || root.Count == 0)
                return false;

            List<string> eventTypeNames = new List<string>();
            foreach (JsonObject eventTypeObj in root.GetField("EventTypes"))
            {
                eventTypeNames.Add(eventTypeObj.str);
            }
            root.RemoveField("EventTypes");
            foreach (JsonObject aniEventObj in root)
            {
                if (aniEventObj.Count == 0)
                    continue;

                AnimInfo animInfo = new AnimInfo();
                List<AniEventGroup> aniEventGroups = new List<AniEventGroup>();

                aniEventObj.GetField(out animInfo.stateName, "StateName", string.Empty);
                aniEventObj.GetField(out animInfo.clipName, "ClipName", string.Empty);
                aniEventObj.GetField(out animInfo.endTime, "EndTime", 0f);
                aniEventObj.GetField(out animInfo.useRootMotion, "Use_RootMotion", false);

                foreach (JsonObject evtGroupObj in aniEventObj.GetField("Events"))
                {
                    if (evtGroupObj.Count == 0)
                        continue;

                    AniEventGroup evtGroup = new AniEventGroup();
                    evtGroup.aniEvents = new List<AniEventBase>();
                    evtGroupObj.GetField(out evtGroup.eventName, "EventGroup", string.Empty);
                    evtGroupObj.GetField(out evtGroup.startTime, "StartTime", 0f);
                    evtGroupObj.GetField(out evtGroup.endTime, "EndTime", 0f);
                    evtGroupObj.GetField(out evtGroup.index, "Index", -1);

                    foreach (string eventType in eventTypeNames)
                    {
                        evtGroupObj.GetField(out int count, $"{eventType}Count", 0);
                        if (count == 0)
                            continue;
                        //get type by name from all loaded assemblies
                        Type type = FindTypeByName($"AniEventTool.{eventType}");
                        if (type == null || type.IsSubclassOf(typeof(AniEventBase)) == false)
                            continue;


                        evtGroupObj.GetField(out int evtCount, $"{eventType}Count", 0);
                        if (evtCount == 0)
                            continue;

                        JsonObject eventObj = evtGroupObj.GetField($"{eventType}Datas");
                        for (int i = 0; i < evtCount; i++)
                        {
                            AniEventBase aniEvent = Activator.CreateInstance(type) as AniEventBase;
                            if (aniEvent == null)
                                continue;
                            JsonObject eventDataJson = eventObj.list[i];
                            if (aniEvent.ReadPropertiesFromJson(ref eventDataJson) == false)
                                continue;

                            evtGroup.aniEvents.Add(aniEvent);
                        }
                    }
                    aniEventGroups.Add(evtGroup);
                }

                dicAniEvent.Add((animInfo, aniEventGroups));
            }

            return true;
        }
    }
}