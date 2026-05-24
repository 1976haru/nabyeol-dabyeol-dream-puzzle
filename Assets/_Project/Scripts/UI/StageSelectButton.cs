// StageSelectButton.cs
// Task 93 — Individual stage button used inside StageSelectUI.
//
// Owns a single stage tile: id label, optional name label, and three state
// icons (locked / cleared / current). Click is forwarded to a callback
// supplied by StageSelectUI so the button itself stays controller-free.
//
// Null-safe: every UI reference is optional, every state mutation is guarded.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    public class StageSelectButton : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI stageIdText;
        [SerializeField] private TextMeshProUGUI stageNameText;

        [Header("State icons")]
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject clearIcon;
        [SerializeField] private GameObject currentIcon;

        [Header("Button")]
        [SerializeField] private Button button;

        public int StageId { get; private set; }
        public bool IsLocked { get; private set; }
        public bool IsCleared { get; private set; }
        public bool IsCurrent { get; private set; }

        private Action<int> onClickCallback;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>
        /// Configure this button for a stage. Safe to call repeatedly.
        /// </summary>
        public void Setup(int stageId,
                          string stageName,
                          bool locked,
                          bool cleared,
                          bool current,
                          Action<int> onClick)
        {
            StageId   = stageId;
            IsLocked  = locked;
            IsCleared = cleared;
            IsCurrent = current;
            onClickCallback = onClick;

            if (stageIdText != null)
                stageIdText.text = stageId > 0 ? stageId.ToString() : "?";

            if (stageNameText != null)
                stageNameText.text = string.IsNullOrEmpty(stageName) ? string.Empty : stageName;

            if (lockIcon    != null) lockIcon.SetActive(locked);
            if (clearIcon   != null) clearIcon.SetActive(cleared && !locked);
            if (currentIcon != null) currentIcon.SetActive(current && !locked);

            if (button != null)
            {
                // Locked stages are non-interactable. PlayButton in StageSelectUI
                // re-checks lock state in case any UI lets the click through.
                button.interactable = !locked;
            }
        }

        private void HandleClick()
        {
            if (IsLocked)
            {
                Debug.Log($"[StageSelectButton] locked stage {StageId} click ignored.");
                onClickCallback?.Invoke(StageId);
                return;
            }
            Debug.Log($"[StageSelectButton] clicked stageId={StageId}.");
            onClickCallback?.Invoke(StageId);
        }
    }
}
