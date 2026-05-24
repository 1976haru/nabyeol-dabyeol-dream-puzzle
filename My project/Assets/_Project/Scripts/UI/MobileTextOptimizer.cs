// MobileTextOptimizer.cs
// Task 99 — Apply mobile-friendly text styles to TMP texts on a panel.
//
// Drop one on each major UI panel (MainMenu / StageSelect / GameHUD /
// ClearPopup / FailPopup / Album / ParentModeHub / Story UIs), assign the
// shared MobileTextStyleData, and register each TMP text with its semantic
// role (Title, Body, Button, …). On Awake the optimizer applies font sizes,
// enables word-wrapping for long text, and warns about buttons that are too
// short or texts smaller than the readability threshold. No layout edits,
// no button event tampering — pure visual tuning.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>One TMP text and its semantic role for mobile sizing.</summary>
    [Serializable]
    public struct MobileTextItem
    {
        public TextMeshProUGUI text;
        public MobileTextRole  role;
        [Tooltip("If true, enableWordWrapping is forced on. Useful for body / dialogue / description text.")]
        public bool wrapLongText;
    }

    /// <summary>
    /// Applies <see cref="MobileTextStyleData"/> font sizes to the registered TMP
    /// texts and audits button heights. Safe to leave disabled or partially wired —
    /// missing fields are skipped, never thrown.
    /// </summary>
    [DisallowMultipleComponent]
    public class MobileTextOptimizer : MonoBehaviour
    {
        [Header("Style table")]
        [SerializeField] private MobileTextStyleData styleData;

        [Header("Texts to optimize")]
        [SerializeField] private MobileTextItem[] textItems;

        [Header("Buttons to audit (height check)")]
        [SerializeField] private Button[] buttons;

        [Header("Behavior")]
        [Tooltip("Reapply styles on every OnEnable in case the panel is re-shown after a font swap.")]
        [SerializeField] private bool applyOnEnable = true;

        [Tooltip("Log warnings when readability thresholds are violated.")]
        [SerializeField] private bool logWarnings = true;

        // Fallback values used when styleData is not assigned — keep them sane so
        // a missing asset only triggers a warning, not a crash.
        private const int   FallbackBodyFontSize    = 36;
        private const int   FallbackMinReadable     = 20;
        private const float FallbackMinButtonHeight = 72f;

        private void Awake()
        {
            ApplyTextStyles();
            CheckButtonHeights();
        }

        private void OnEnable()
        {
            if (!applyOnEnable) return;
            ApplyTextStyles();
            CheckButtonHeights();
        }

        // ---- Public API -------------------------------------------------------------------

        /// <summary>
        /// Apply the configured font size for each item's role. Items with a null
        /// <see cref="MobileTextItem.text"/> are skipped. Triggers a warning when a
        /// resulting size is below the readability threshold.
        /// </summary>
        public void ApplyTextStyles()
        {
            if (textItems == null || textItems.Length == 0) return;
            if (styleData == null)
            {
                if (logWarnings)
                    Debug.LogWarning($"MobileTextOptimizer ({name}): styleData not assigned — using fallback body size {FallbackBodyFontSize}.");
            }

            int minReadable = styleData != null ? styleData.minimumReadableFontSize : FallbackMinReadable;

            for (int i = 0; i < textItems.Length; i++)
            {
                MobileTextItem item = textItems[i];
                TextMeshProUGUI text = item.text;
                if (text == null) continue;

                int size = styleData != null ? styleData.GetFontSize(item.role) : FallbackBodyFontSize;
                if (size > 0) text.fontSize = size;

                if (item.wrapLongText)
                {
                    text.enableWordWrapping = true;
                }

                if (logWarnings && text.fontSize > 0 && text.fontSize < minReadable)
                {
                    Debug.LogWarning(
                        $"MobileTextOptimizer ({name}): '{text.name}' fontSize={text.fontSize} is below readable minimum {minReadable}.",
                        text);
                }
            }
        }

        /// <summary>
        /// Audit registered buttons; warn for each one shorter than the configured
        /// minimum height. Does not mutate the buttons — sizing remains an Editor
        /// choice.
        /// </summary>
        public void CheckButtonHeights()
        {
            if (!logWarnings || buttons == null || buttons.Length == 0) return;
            float minHeight = styleData != null ? styleData.minimumButtonHeight : FallbackMinButtonHeight;

            for (int i = 0; i < buttons.Length; i++)
            {
                Button btn = buttons[i];
                if (btn == null) continue;
                RectTransform rect = btn.transform as RectTransform;
                if (rect == null) continue;
                float h = rect.rect.height;
                if (h < minHeight)
                {
                    Debug.LogWarning(
                        $"MobileTextOptimizer ({name}): button '{btn.name}' height={h:F1} is below recommended {minHeight}.",
                        btn);
                }
            }
        }
    }
}
