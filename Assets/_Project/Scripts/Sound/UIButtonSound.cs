// UIButtonSound.cs
// Task 100 — Drop this on a UI Button to play the ButtonClick SFX on click.
// Keeps SFX wiring out of individual UI scripts; one component per button.

using UnityEngine;
using UnityEngine.UI;

namespace NabyeolDabyeolDreamPuzzle.Sound
{
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public class UIButtonSound : MonoBehaviour
    {
        [Tooltip("Override the click SFX. Defaults to SfxType.ButtonClick.")]
        [SerializeField] private SfxType clickSfx = SfxType.ButtonClick;

        [Tooltip("If true, the click sound is still played even when the button is not interactable.")]
        [SerializeField] private bool playWhenDisabled = false;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button == null) return;
            button.onClick.RemoveListener(PlayClick);
            button.onClick.AddListener(PlayClick);
        }

        private void OnDisable()
        {
            if (button == null) return;
            button.onClick.RemoveListener(PlayClick);
        }

        private void PlayClick()
        {
            if (!playWhenDisabled && button != null && !button.interactable) return;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(clickSfx);
            }
        }
    }
}
