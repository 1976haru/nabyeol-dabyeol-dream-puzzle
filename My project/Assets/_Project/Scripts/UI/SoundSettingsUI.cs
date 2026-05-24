// SoundSettingsUI.cs
// Task 100 — Optional UI to expose SoundManager volume / mute to the player.
// Bind a mute Toggle and two volume Sliders in the Inspector. All fields are
// optional — leave any of them unassigned and that control is simply skipped.

using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Sound;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    public class SoundSettingsUI : MonoBehaviour
    {
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;

        private void OnEnable()
        {
            SoundManager mgr = SoundManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("SoundSettingsUI: SoundManager.Instance not found.");
                return;
            }

            if (muteToggle != null)
            {
                muteToggle.SetIsOnWithoutNotify(mgr.IsMuted());
                muteToggle.onValueChanged.RemoveListener(OnMuteChanged);
                muteToggle.onValueChanged.AddListener(OnMuteChanged);
            }
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(mgr.GetMasterVolume());
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(mgr.GetSfxVolume());
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(mgr.GetBgmVolume());
                bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
                bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            }
        }

        private void OnDisable()
        {
            if (muteToggle != null)         muteToggle.onValueChanged.RemoveListener(OnMuteChanged);
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (sfxVolumeSlider != null)    sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            if (bgmVolumeSlider != null)    bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        }

        private void OnMuteChanged(bool value)
        {
            SoundManager.Instance?.SetMuted(value);
        }

        private void OnMasterVolumeChanged(float value)
        {
            SoundManager.Instance?.SetMasterVolume(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            SoundManager.Instance?.SetSfxVolume(value);
        }

        private void OnBgmVolumeChanged(float value)
        {
            SoundManager.Instance?.SetBgmVolume(value);
        }
    }
}
