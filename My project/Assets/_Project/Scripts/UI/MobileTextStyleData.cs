// MobileTextStyleData.cs
// Task 99 — Mobile-friendly text style table.
//
// Stores font-size presets per text role so screens can be reskinned for
// child-friendly mobile readability without touching individual UI scripts.
// Create the asset via: Assets > Create > MallangTwins > UI > Mobile Text Style.

using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// Semantic role of a TMP text in mobile UI. Matched to a font size by
    /// <see cref="MobileTextStyleData"/>.
    /// </summary>
    public enum MobileTextRole
    {
        Title,
        Subtitle,
        Body,
        Button,
        Small,
        PopupTitle,
        PopupBody,
    }

    [CreateAssetMenu(
        fileName = "MobileTextStyleData",
        menuName = "MallangTwins/UI/Mobile Text Style",
        order = 200)]
    public class MobileTextStyleData : ScriptableObject
    {
        [Header("Font sizes (pt — TMP units, 1080x1920 reference)")]
        [Min(8)] public int titleFontSize       = 64;
        [Min(8)] public int subtitleFontSize    = 48;
        [Min(8)] public int bodyFontSize        = 36;
        [Min(8)] public int buttonFontSize      = 40;
        [Min(8)] public int smallFontSize       = 28;
        [Min(8)] public int popupTitleFontSize  = 56;
        [Min(8)] public int popupBodyFontSize   = 36;

        [Header("Readability thresholds")]
        [Tooltip("Warning is logged when any TMP font size is below this value.")]
        [Min(1)] public int minimumReadableFontSize = 20;

        [Tooltip("Warning is logged when any button's RectTransform height is below this value.")]
        [Min(1)] public float minimumButtonHeight = 72f;

        /// <summary>Returns the configured font size for <paramref name="role"/>.</summary>
        public int GetFontSize(MobileTextRole role)
        {
            switch (role)
            {
                case MobileTextRole.Title:      return titleFontSize;
                case MobileTextRole.Subtitle:   return subtitleFontSize;
                case MobileTextRole.Body:       return bodyFontSize;
                case MobileTextRole.Button:     return buttonFontSize;
                case MobileTextRole.Small:      return smallFontSize;
                case MobileTextRole.PopupTitle: return popupTitleFontSize;
                case MobileTextRole.PopupBody:  return popupBodyFontSize;
                default:                        return bodyFontSize;
            }
        }
    }
}
