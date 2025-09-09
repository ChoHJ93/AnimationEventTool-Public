using UnityEngine;

namespace AniEventTool.ProjectRelated
{
    public class PR_HitEventTrack : HitEventTrack
    {
        public override int Inspector_GetLayerMask(eTargetType targetType)
        {
            switch (targetType)
            {
                default:
                case eTargetType.None:
                case eTargetType.Custom:
                    return 0;

#if PROJECT_RELATED_SAMPLE
                case eTargetType.Player:
                    return LayerManager.GetMask(LayerEnum.Character);
                case eTargetType.Other:
                    return LayerManager.GetMask(LayerEnum.Monster);
#endif // PROJECT_RELATED_SAMPLE
            }
        }
    }
}