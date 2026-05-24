// SoundManager.cs
// Task 100 — Central SFX hub for the puzzle game.
//
// Design:
//   - Singleton. One AudioSource is used for SFX. Every external call goes
//     through PlaySfx(SfxType) so individual gameplay scripts never spawn
//     their own AudioSources.
//   - SFX are bound by enum (SfxType) so callers don't pass strings. Clips
//     are configured in the Inspector as a list of SoundClipData entries —
//     one per role. Missing clips degrade gracefully (warning, no throw).
//   - Volume and mute are persisted to PlayerPrefs and reloaded on Awake.
//   - Final volume = masterVolume × sfxVolume × clipData.volume. Mute zeroes
//     all of them at the AudioSource level.
//
// TODO:
//   - Add per-sfx cooldown to avoid sound spam when designers tune game feel.
//   - Add BGM channel and ducking.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Sound
{
    /// <summary>Logical SFX roles. New entries can be appended at the end safely.</summary>
    public enum SfxType
    {
        ButtonClick   = 0,
        BlockSelect   = 1,
        BlockSwap     = 2,
        Match         = 3,
        BlockRemove   = 4,
        Drop          = 5,
        Cascade       = 6,
        Clear         = 7,
        Fail          = 8,
        Reward        = 9,
        Skill         = 10,
        PopupOpen     = 11,
        PopupClose    = 12,
    }

    /// <summary>Logical BGM roles. None = no music (StopBgm equivalent).</summary>
    public enum BgmType
    {
        None        = 0,
        MainMenu    = 1,
        StageSelect = 2,
        Puzzle      = 3,
        Boss        = 4,
        Calm        = 5,
        Clear       = 6,
    }

    /// <summary>Per-clip data assigned in the Inspector.</summary>
    [System.Serializable]
    public struct SoundClipData
    {
        public SfxType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    /// <summary>Per-BGM clip data assigned in the Inspector.</summary>
    [System.Serializable]
    public struct BgmClipData
    {
        public BgmType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    /// <summary>
    /// Central manager for all gameplay SFX. Persisted as a singleton across
    /// scenes (DontDestroyOnLoad) so puzzle-side calls survive scene reloads
    /// from Stage retry/select transitions.
    /// </summary>
    [DisallowMultipleComponent]
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("SFX Source")]
        [SerializeField] private AudioSource sfxSource;

        [Header("BGM Source")]
        [SerializeField] private AudioSource bgmSource;

        [Header("SFX Clips")]
        [SerializeField] private SoundClipData[] clips;

        [Header("BGM Clips")]
        [SerializeField] private BgmClipData[] bgmClips;

        [Header("Volume (0..1)")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume    = 1f;
        [Tooltip("BGM channel volume. Defaults lower than SFX so background music does not overpower the game feel.")]
        [SerializeField, Range(0f, 1f)] private float bgmVolume    = 0.6f;
        [SerializeField] private bool muted = false;

        // PlayerPrefs keys.
        private const string PrefMasterVolume = "Sound_MasterVolume";
        private const string PrefSfxVolume    = "Sound_SfxVolume";
        private const string PrefBgmVolume    = "Sound_BgmVolume";
        private const string PrefMuted        = "Sound_Muted";

        // Indexed lookup built on Awake from the inspector list.
        private Dictionary<SfxType, SoundClipData> clipLookup;
        private Dictionary<BgmType, BgmClipData>   bgmClipLookup;

        // To keep the warning log readable, suppress repeated misses per type.
        private HashSet<SfxType> warnedMissingClips;
        private HashSet<BgmType> warnedMissingBgm;

        // Runtime BGM state.
        private BgmType  currentBgmType = BgmType.None;
        private Coroutine bgmFadeRoutine;

        // ---- Lifecycle ---------------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("SoundManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSource();
            EnsureBgmSource();
            BuildClipLookup();
            BuildBgmClipLookup();
            warnedMissingClips = new HashSet<SfxType>();
            warnedMissingBgm   = new HashSet<BgmType>();
            LoadSettings();
            ApplyVolumeToSource();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnsureSource()
        {
            if (sfxSource == null)
            {
                // Avoid colliding with bgmSource: only adopt an existing AudioSource
                // when it is not already the bgmSource reference.
                AudioSource existing = GetComponent<AudioSource>();
                if (existing != null && existing != bgmSource) sfxSource = existing;
            }
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        private void EnsureBgmSource()
        {
            if (bgmSource == null)
            {
                // Find any existing AudioSource other than sfxSource on this object,
                // otherwise add a new one dedicated to BGM.
                AudioSource[] all = GetComponents<AudioSource>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != sfxSource) { bgmSource = all[i]; break; }
                }
            }
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }

        private void BuildClipLookup()
        {
            clipLookup = new Dictionary<SfxType, SoundClipData>();
            if (clips == null) return;
            for (int i = 0; i < clips.Length; i++)
            {
                SoundClipData data = clips[i];
                // Last write wins so designers can override duplicates by reordering.
                clipLookup[data.type] = data;
            }
        }

        private void BuildBgmClipLookup()
        {
            bgmClipLookup = new Dictionary<BgmType, BgmClipData>();
            if (bgmClips == null) return;
            for (int i = 0; i < bgmClips.Length; i++)
            {
                BgmClipData data = bgmClips[i];
                bgmClipLookup[data.type] = data;
            }
        }

        // ---- Public API --------------------------------------------------------------------

        /// <summary>
        /// Play the SFX clip registered for <paramref name="type"/>. No-op when muted,
        /// when the clip is missing, or when the source is gone. Safe to call every frame
        /// from gameplay code — callers should still throttle to one play per event.
        /// </summary>
        public void PlaySfx(SfxType type)
        {
            if (muted) return;
            if (sfxSource == null)
            {
                Debug.LogWarning("SoundManager: sfxSource missing; cannot play.");
                return;
            }
            if (clipLookup == null) BuildClipLookup();

            if (!clipLookup.TryGetValue(type, out SoundClipData data) || data.clip == null)
            {
                if (warnedMissingClips == null) warnedMissingClips = new HashSet<SfxType>();
                if (warnedMissingClips.Add(type))
                {
                    Debug.LogWarning($"SoundManager: AudioClip missing for {type}. Further warnings for {type} suppressed.");
                }
                return;
            }

            float perClipVolume = Mathf.Clamp01(data.volume <= 0f ? 1f : data.volume);
            float finalVolume   = Mathf.Clamp01(masterVolume * sfxVolume * perClipVolume);
            sfxSource.PlayOneShot(data.clip, finalVolume);
        }

        /// <summary>Mute or unmute all SFX. Persists to PlayerPrefs.</summary>
        public void SetMuted(bool value)
        {
            muted = value;
            ApplyVolumeToSource();
            SaveSettings();
        }

        public bool IsMuted() => muted;

        /// <summary>Set the master volume (0..1). Persists to PlayerPrefs.</summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeToSource();
            SaveSettings();
        }

        public float GetMasterVolume() => masterVolume;

        /// <summary>Set the SFX channel volume (0..1). Persists to PlayerPrefs.</summary>
        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumeToSource();
            SaveSettings();
        }

        public float GetSfxVolume() => sfxVolume;

        // ---- Settings persistence ----------------------------------------------------------

        private void LoadSettings()
        {
            if (PlayerPrefs.HasKey(PrefMasterVolume))
                masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMasterVolume, masterVolume));
            if (PlayerPrefs.HasKey(PrefSfxVolume))
                sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfxVolume, sfxVolume));
            if (PlayerPrefs.HasKey(PrefBgmVolume))
                bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefBgmVolume, bgmVolume));
            if (PlayerPrefs.HasKey(PrefMuted))
                muted = PlayerPrefs.GetInt(PrefMuted, muted ? 1 : 0) != 0;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(PrefMasterVolume, masterVolume);
            PlayerPrefs.SetFloat(PrefSfxVolume,    sfxVolume);
            PlayerPrefs.SetFloat(PrefBgmVolume,    bgmVolume);
            PlayerPrefs.SetInt(PrefMuted,          muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyVolumeToSource()
        {
            if (sfxSource != null)
            {
                sfxSource.mute = muted;
                sfxSource.volume = Mathf.Clamp01(masterVolume * sfxVolume);
            }
            if (bgmSource != null)
            {
                bgmSource.mute = muted;
                bgmSource.volume = ComputeFinalBgmVolume();
            }
        }

        private float ComputeFinalBgmVolume()
        {
            float clipVolume = 1f;
            if (bgmClipLookup != null
                && currentBgmType != BgmType.None
                && bgmClipLookup.TryGetValue(currentBgmType, out BgmClipData data))
            {
                clipVolume = data.volume <= 0f ? 1f : Mathf.Clamp01(data.volume);
            }
            return Mathf.Clamp01(masterVolume * bgmVolume * clipVolume);
        }

        // ---- BGM public API ----------------------------------------------------------------

        /// <summary>
        /// Play the BGM registered for <paramref name="type"/> in loop. Restarts only when
        /// switching to a different type — repeated calls with the same type are no-ops so
        /// scene-enter triggers don't cause restarts on every UI refresh.
        /// </summary>
        public void PlayBgm(BgmType type)
        {
            if (type == BgmType.None)
            {
                StopBgm();
                return;
            }
            if (bgmSource == null)
            {
                Debug.LogWarning("SoundManager: bgmSource missing; cannot play BGM.");
                return;
            }
            if (bgmClipLookup == null) BuildBgmClipLookup();

            if (currentBgmType == type && bgmSource.isPlaying) return;

            if (!bgmClipLookup.TryGetValue(type, out BgmClipData data) || data.clip == null)
            {
                if (warnedMissingBgm == null) warnedMissingBgm = new HashSet<BgmType>();
                if (warnedMissingBgm.Add(type))
                {
                    Debug.LogWarning($"SoundManager: BGM clip missing for {type}. Further warnings for {type} suppressed.");
                }
                return;
            }

            StopFadeRoutine();
            currentBgmType   = type;
            bgmSource.clip   = data.clip;
            bgmSource.loop   = true;
            bgmSource.volume = ComputeFinalBgmVolume();
            bgmSource.Play();
            Debug.Log($"SoundManager: BGM playing: {type} (clip='{data.clip.name}').");
        }

        /// <summary>Stop the BGM immediately and clear current type.</summary>
        public void StopBgm()
        {
            StopFadeRoutine();
            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }
            currentBgmType = BgmType.None;
        }

        /// <summary>
        /// Fade out the current clip, swap to the new BGM and fade in. Same type +
        /// already playing is a no-op. fadeSeconds is split half/half.
        /// </summary>
        public void PlayBgmWithFade(BgmType type, float fadeSeconds = 0.5f)
        {
            if (type == BgmType.None)
            {
                if (gameObject.activeInHierarchy)
                    bgmFadeRoutine = StartCoroutine(FadeOutAndStop(Mathf.Max(0f, fadeSeconds)));
                else
                    StopBgm();
                return;
            }
            if (bgmSource == null)
            {
                Debug.LogWarning("SoundManager: bgmSource missing; cannot fade BGM.");
                return;
            }
            if (currentBgmType == type && bgmSource.isPlaying) return;
            if (!gameObject.activeInHierarchy || fadeSeconds <= 0f)
            {
                PlayBgm(type);
                return;
            }
            StopFadeRoutine();
            bgmFadeRoutine = StartCoroutine(FadeRoutine(type, fadeSeconds));
        }

        private IEnumerator FadeRoutine(BgmType nextType, float fadeSeconds)
        {
            float half = Mathf.Max(0.01f, fadeSeconds * 0.5f);
            float startVolume = bgmSource != null ? bgmSource.volume : 0f;

            // Fade out.
            float t = 0f;
            while (t < half && bgmSource != null && bgmSource.isPlaying)
            {
                t += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / half);
                yield return null;
            }

            // Swap clip without restarting if it's the same.
            currentBgmType = nextType;
            if (bgmClipLookup != null
                && bgmClipLookup.TryGetValue(nextType, out BgmClipData data)
                && data.clip != null
                && bgmSource != null)
            {
                bgmSource.clip   = data.clip;
                bgmSource.loop   = true;
                bgmSource.volume = 0f;
                bgmSource.Play();
            }
            else
            {
                if (warnedMissingBgm == null) warnedMissingBgm = new HashSet<BgmType>();
                if (warnedMissingBgm.Add(nextType))
                    Debug.LogWarning($"SoundManager: BGM clip missing for {nextType} during fade. Suppressing further warnings.");
                bgmFadeRoutine = null;
                yield break;
            }

            // Fade in.
            float target = ComputeFinalBgmVolume();
            t = 0f;
            while (t < half && bgmSource != null)
            {
                t += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, target, t / half);
                yield return null;
            }
            if (bgmSource != null) bgmSource.volume = target;
            bgmFadeRoutine = null;
        }

        private IEnumerator FadeOutAndStop(float fadeSeconds)
        {
            if (bgmSource == null) yield break;
            float startVolume = bgmSource.volume;
            float t = 0f;
            while (t < fadeSeconds && bgmSource != null && bgmSource.isPlaying)
            {
                t += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeSeconds);
                yield return null;
            }
            StopBgm();
        }

        private void StopFadeRoutine()
        {
            if (bgmFadeRoutine != null)
            {
                StopCoroutine(bgmFadeRoutine);
                bgmFadeRoutine = null;
            }
        }

        /// <summary>Set BGM channel volume (0..1) and persist.</summary>
        public void SetBgmVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            ApplyVolumeToSource();
            SaveSettings();
        }

        public float GetBgmVolume() => bgmVolume;

        /// <summary>Currently active BGM, or BgmType.None when stopped.</summary>
        public BgmType GetCurrentBgmType() => currentBgmType;
    }
}
