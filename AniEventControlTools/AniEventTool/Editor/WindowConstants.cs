using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool.Editor
{
    public static class WindowConstants
    {
        public const float defaultPadding = 2f;
        public const float kBaseIndent = 15.0f;
        public const float kSubIndent = 30.0f;

        public const float timeAreaYPosition = 19.0f;
        public const float timAreaCursorTextWidth = 17f;
        public const float timAreaCursorTextHeight = 20;
        public const float timeCodeWidth = 50.0f; //should be Enough space to display up to 9999 without clipping
        public const float timeAreaShownRangePadding = 5.0f;
        public const float maxTimeAreaScaling = 90000.0f;

        public const float kDurationGuiThickness = 5.0f;

        //TrackGUI
        public const float eventItemHeight = 30;
        public const float eventLabelHeight = 20;
        public const float eventHeaderButtonSize = 16.0f;
        public const float trackBindingPadding = 5.0f;
        public const float trackContentFieldMin = 20f;
        public const float contentFieldDrawThreshold = 128f;

        public static readonly Vector2 TimeAreaDefaultRange = new Vector2(-timeAreaShownRangePadding, 5.0f); // in seconds. Hack: using negative value to force the UI to have a left margin at 0.
    }
}