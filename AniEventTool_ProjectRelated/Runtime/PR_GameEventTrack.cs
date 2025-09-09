using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.ProjectRelated
{
    public class PR_GameEventTrack : AniEventTrack<PR_GameEvent>
    {
        [SerializeField] private List<(Renderer, Material[])> originalMaterials;
        [SerializeField] private List<Material> instancedMaterials;
        public List<Material> InstancedMaterials
        {
            get
            {
                if (instancedMaterials == null)
                {
                    instancedMaterials = new List<Material>();
                    originalMaterials = new List<(Renderer, Material[])>();
                    if (windowState.SelectedController != null)
                    {
                        Renderer[] renderers = windowState.SelectedController.GetComponentsInChildren<Renderer>();
                        foreach (Renderer renderer in renderers)
                        {
                            (Renderer, Material[]) originalMat = (renderer, renderer.sharedMaterials);
                            originalMaterials.Add(originalMat);

                            if (renderer.sharedMaterial.HasProperty("_AttackNotice"))
                            {
                                Material instancedMat = new Material(renderer.sharedMaterial);
                                instancedMat.SetFloat("_AttackNotice", 0);
                                instancedMat.name = renderer.sharedMaterial.name + "(Instanced)";
                                instancedMaterials.Add(instancedMat);

                                renderer.sharedMaterial = instancedMat;
                            }
                        }
                    }
                }
                return instancedMaterials;
            }
        }

        protected virtual void OnDisable()
        {
            if (originalMaterials != null)
            {
                for (int i = 0; i < originalMaterials.Count; i++)
                {
                    (Renderer, Material[]) originalMat = originalMaterials[i];
                    if (originalMat.Item1 != null)
                        originalMat.Item1.sharedMaterials = originalMat.Item2;
                }
            }
            if (instancedMaterials.IsNullOrEmpty() == false)
                instancedMaterials.Clear();
        }

        [SerializeField] public eGameEventType gameEventType;
        [SerializeField] public bool cancelSkill;

        protected override void Init(WindowState windowState, PR_GameEvent aniEvent, AniEventGroupTrack parentTrackAsset = null)
        {
            base.Init(windowState, aniEvent, parentTrackAsset);

            //Init properties
            gameEventType = aniEvent.eventType;
            cancelSkill = aniEvent.cancelSkill;
        }

        protected override void ClearData()
        {
            base.ClearData();
            data.eventType = eGameEventType.None;
            data.cancelSkill = false;
        }
        public override void ApplyToEventData()
        {
            base.ApplyToEventData();
            data.eventType = gameEventType;
            data.cancelSkill = cancelSkill;
        }
        public override void PlayEvent(float currentTime)
        {
            base.PlayEvent(currentTime);
        }

        public override void MoveTime(float movedTime)
        {
            base.MoveTime(movedTime);
        }

#if UNITY_EDITOR
        public void Inspector_OnPropertiesModified()
        {
            // Clamp properties
        }
#endif

    }
}