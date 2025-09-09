
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GizmoTool
{
    [CreateAssetMenu(menuName = "Custom Gizmo Setting")]
    public class CustomGizmoOption : ScriptableObject
    {
#if UNITY_EDITOR
        public bool showPlayerDirectionArrow = false;
        public float playerArrowLength = 1f;
        public float playerArrowWidth = 0.5f;
        public Color arrowColor = Color.cyan;
        public CustomGizmoUtil.Style arrowStyle = CustomGizmoUtil.Style.SmoothShaded;

        public bool showEnemyHitbox = false;
        public Color enemyHitboxColor = Color.green;
        public CustomGizmoUtil.Style enemyHitboxStyle = CustomGizmoUtil.Style.Wireframe;

        public bool showEnemyClickArea = false;
        public Color enemyClickAreaColor = Color.red;
        public CustomGizmoUtil.Style clickAreaStyle = CustomGizmoUtil.Style.Wireframe;

        public bool showMouseWorldPos = false;

        public bool showMouseCurserImg = false;
        public Color curserColor_Normal = Color.green;
        public Color curserColor_OnEnemy = Color.red;
        public Sprite curserSprite;

        #region Cheat
        [HideInInspector] public bool cheat_player_Invincible;
        [HideInInspector] public bool cheat_player_Zombie;
        [HideInInspector] public bool cheat_player_NoCoolTime;
        [HideInInspector] public bool cheat_player_Heal;
        [HideInInspector] public bool cheat_player_ResetCoin;
        [HideInInspector] public bool cheat_enemy_Stop;
        [HideInInspector] public bool cheat_enemy_KillNearby;
        [HideInInspector] public bool cheat_enemy_NoDamage;
        [HideInInspector] public Color cheat_enemy_KillRangeColor = Color.red;
        [HideInInspector] public float cheat_enemy_KillRange;
        //[HideInInspector] public bool cheat_
        #endregion

        public void SetActiveAll(bool value)
        {
            showPlayerDirectionArrow = value;
            showEnemyHitbox = value;
            showEnemyClickArea = value;
            showMouseWorldPos = value;
            showMouseCurserImg = value;
        }
#endif

    }

#if UNITY_EDITOR
    [System.Serializable]
    public class CustomGizmoSetting
    {
        public static string SHOW_GIZMO_KEY = "GizmoSetting";

        [MenuItem("Custom/Show Custom Gizmo")]
        static void ToggleShowGizmo()
        {
            bool showGizmo = EditorPrefs.GetBool(SHOW_GIZMO_KEY, false);
            showGizmo = !showGizmo;
            EditorPrefs.SetBool(SHOW_GIZMO_KEY, showGizmo);
        }

        [MenuItem("Custom/Show Custom Gizmo", true)]
        static bool ShowGizmoOptionValidate()
        {
            bool showGizmo = EditorPrefs.GetBool(SHOW_GIZMO_KEY, false);
            Menu.SetChecked("Custom/Show Custom Gizmo", showGizmo);
            return true;
        }
    }
#endif
}
