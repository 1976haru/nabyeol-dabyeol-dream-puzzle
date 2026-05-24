// CollectionItemButton.cs
// Task 97 — Reusable card button used inside CollectionAlbumUI for a single
// character / knowledge card / album page entry.
//
// Responsibilities:
//   - Own a single tile: icon, label, lock overlay, selection highlight.
//   - Forward clicks to a callback supplied by CollectionAlbumUI so this
//     button itself stays controller-free.
//   - Both locked and unlocked items remain clickable so the parent UI can
//     show the unlock hint in the detail pane.
//
// Null-safe: every UI reference is optional. Setup is idempotent.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    public class CollectionItemButton : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI subText;

        [Header("Visuals")]
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject selectedHighlight;

        [Header("Button")]
        [SerializeField] private Button button;

        [Header("Locked tint")]
        [Tooltip("Applied to iconImage when the item is locked.")]
        [SerializeField] private Color lockedTint = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color unlockedTint = Color.white;

        public string ItemId { get; private set; }
        public bool IsLocked { get; private set; }

        private Action<string> onClickCallback;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (selectedHighlight != null) selectedHighlight.SetActive(false);
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
        /// Configure this button. Pass null for icon to keep the prefab's default
        /// placeholder. Locked items still receive clicks so the parent UI can
        /// surface "Stage X 클리어 시 열림" hints.
        /// </summary>
        public void Setup(string itemId,
                          string label,
                          string sub,
                          Sprite icon,
                          bool locked,
                          Action<string> onClick)
        {
            ItemId = itemId;
            IsLocked = locked;
            onClickCallback = onClick;

            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(label) ? (string.IsNullOrEmpty(itemId) ? "?" : itemId) : label;

            if (subText != null)
                subText.text = string.IsNullOrEmpty(sub) ? string.Empty : sub;

            if (iconImage != null)
            {
                if (icon != null) iconImage.sprite = icon;
                iconImage.color = locked ? lockedTint : unlockedTint;
            }

            if (lockOverlay != null) lockOverlay.SetActive(locked);
            if (selectedHighlight != null) selectedHighlight.SetActive(false);

            if (button != null)
            {
                // Both locked and unlocked items stay interactable — the parent
                // pane decides whether to show the detail or the lock hint.
                button.interactable = true;
            }
        }

        /// <summary>Visual-only selection toggle. Does not raise the click callback.</summary>
        public void SetSelected(bool selected)
        {
            if (selectedHighlight != null) selectedHighlight.SetActive(selected);
        }

        private void HandleClick()
        {
            Debug.Log($"[CollectionItemButton] clicked itemId='{ItemId}' locked={IsLocked}.");
            onClickCallback?.Invoke(ItemId);
        }
    }
}
