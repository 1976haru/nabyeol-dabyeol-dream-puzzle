// SimpleAnimationManager.cs
// Task 102 — Minimal coroutine-based animations for blocks, characters,
// popups, and region restore fades.
//
// Design:
//   - No external tween library. Everything is a short Coroutine driven by
//     Time.unscaledDeltaTime so animations work even when the game pauses
//     for the clear popup or stage transitions.
//   - One singleton entrypoint. Gameplay code calls
//     SimpleAnimationManager.Instance?.PlayXxx(...) and falls through
//     silently when the manager isn't in the scene.
//   - Every animation restores its target's scale / alpha at the end so
//     overlapping calls don't drift the value. PlayBlockPop is the only
//     exception — the block is destroyed by the caller, so restoring scale
//     after shrinking would be wasted work.
//   - enableAnimations toggles the whole system off for low-end devices
//     or accessibility. When off, every Play* returns immediately.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NabyeolDabyeolDreamPuzzle.Animation
{
    [DisallowMultipleComponent]
    public class SimpleAnimationManager : MonoBehaviour
    {
        public static SimpleAnimationManager Instance { get; private set; }

        [Header("Global toggle")]
        [Tooltip("When false every Play* coroutine returns instantly.")]
        [SerializeField] private bool enableAnimations = true;

        [Header("Block pop")]
        [SerializeField, Range(0.05f, 0.6f)] private float blockPopDuration = 0.2f;
        [SerializeField, Range(1.0f, 1.5f)]  private float blockPopPeakScale = 1.2f;
        [SerializeField, Range(0.0f, 0.5f)]  private float blockPopEndScale  = 0.05f;

        [Header("UI pulse")]
        [SerializeField, Range(0.1f, 0.6f)] private float pulseDuration  = 0.25f;
        [SerializeField, Range(1.0f, 1.4f)] private float pulsePeakScale = 1.1f;

        [Header("Character bounce")]
        [SerializeField, Range(0.1f, 0.6f)] private float bounceDuration  = 0.3f;
        [SerializeField, Range(1.0f, 1.4f)] private float bouncePeakScale = 1.15f;

        [Header("Shake")]
        [SerializeField, Range(0.05f, 0.5f)] private float defaultShakeStrength = 4f;
        [SerializeField, Range(0.05f, 0.5f)] private float defaultShakeDuration = 0.2f;

        [Header("Popup open")]
        [SerializeField, Range(0.1f, 0.6f)] private float popupOpenDuration = 0.18f;
        [SerializeField, Range(0.5f, 1.0f)] private float popupOpenStartScale = 0.85f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("SimpleAnimationManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool AnimationsEnabled => enableAnimations;
        public float BlockPopDuration => blockPopDuration;
        public void SetAnimationsEnabled(bool value) { enableAnimations = value; }

        // ---- Block pop --------------------------------------------------------------------

        public IEnumerator PlayBlockPop(Transform target)
        {
            if (target == null) yield break;
            if (!enableAnimations) yield break;

            Vector3 originalScale = target.localScale;
            Vector3 peakScale     = originalScale * blockPopPeakScale;
            Vector3 endScale      = originalScale * blockPopEndScale;

            float half = Mathf.Max(0.01f, blockPopDuration * 0.5f);

            float t = 0f;
            while (t < half)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(originalScale, peakScale, t / half);
                yield return null;
            }
            if (target == null) yield break;
            target.localScale = peakScale;

            t = 0f;
            while (t < half)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(peakScale, endScale, t / half);
                yield return null;
            }
            if (target == null) yield break;
            target.localScale = endScale;
        }

        // ---- UI pulse ---------------------------------------------------------------------

        public IEnumerator PlayUIPulse(RectTransform target)
        {
            if (target == null) yield break;
            if (!enableAnimations) yield break;

            Vector3 originalScale = target.localScale;
            Vector3 peakScale     = originalScale * pulsePeakScale;
            float half            = Mathf.Max(0.01f, pulseDuration * 0.5f);

            float t = 0f;
            while (t < half)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(originalScale, peakScale, t / half);
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(peakScale, originalScale, t / half);
                yield return null;
            }
            if (target != null) target.localScale = originalScale;
        }

        // ---- Character bounce -------------------------------------------------------------

        public IEnumerator PlayCharacterBounce(RectTransform target)
        {
            if (target == null) yield break;
            if (!enableAnimations) yield break;

            Vector3 originalScale = target.localScale;
            Vector3 peakScale     = originalScale * bouncePeakScale;
            float upTime    = bounceDuration * 0.33f;
            float downTime  = bounceDuration - upTime;

            float t = 0f;
            while (t < upTime)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(originalScale, peakScale, t / upTime);
                yield return null;
            }
            t = 0f;
            while (t < downTime)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(peakScale, originalScale, t / downTime);
                yield return null;
            }
            if (target != null) target.localScale = originalScale;
        }

        // ---- Shake ------------------------------------------------------------------------

        public IEnumerator PlayShake(RectTransform target, float strength = -1f, float duration = -1f)
        {
            if (target == null) yield break;
            if (!enableAnimations) yield break;

            float s = strength <= 0f ? defaultShakeStrength : strength;
            float d = duration <= 0f ? defaultShakeDuration : duration;
            Vector2 originalPos = target.anchoredPosition;

            float t = 0f;
            while (t < d)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                float remaining = Mathf.Clamp01(1f - t / d);
                Vector2 offset = new Vector2(
                    (Random.value - 0.5f) * 2f,
                    (Random.value - 0.5f) * 2f) * s * remaining;
                target.anchoredPosition = originalPos + offset;
                yield return null;
            }
            if (target != null) target.anchoredPosition = originalPos;
        }

        // ---- Image fade -------------------------------------------------------------------

        public IEnumerator FadeImage(Image image, float from, float to, float duration)
        {
            if (image == null) yield break;
            if (!enableAnimations || duration <= 0f)
            {
                Color final = image.color;
                final.a = to;
                image.color = final;
                yield break;
            }

            Color startColor = image.color;
            startColor.a = from;
            image.color = startColor;

            float t = 0f;
            while (t < duration)
            {
                if (image == null) yield break;
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, t / duration);
                Color c = image.color;
                c.a = a;
                image.color = c;
                yield return null;
            }
            if (image != null)
            {
                Color end = image.color;
                end.a = to;
                image.color = end;
            }
        }

        public IEnumerator FadeSwapSprite(Image image, Sprite newSprite, float duration = 0.4f)
        {
            if (image == null) yield break;
            if (!enableAnimations)
            {
                if (newSprite != null) image.sprite = newSprite;
                yield break;
            }
            float half = Mathf.Max(0.05f, duration * 0.5f);
            float startAlpha = image.color.a;
            yield return FadeImage(image, startAlpha, 0f, half);
            if (newSprite != null && image != null) image.sprite = newSprite;
            yield return FadeImage(image, 0f, startAlpha <= 0f ? 1f : startAlpha, half);
        }

        // ---- Popup open -------------------------------------------------------------------

        public IEnumerator PlayPopupOpen(RectTransform target)
        {
            if (target == null) yield break;
            if (!enableAnimations) yield break;

            Vector3 originalScale = target.localScale;
            Vector3 startScale    = originalScale * popupOpenStartScale;

            target.localScale = startScale;
            float t = 0f;
            while (t < popupOpenDuration)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(startScale, originalScale, t / popupOpenDuration);
                yield return null;
            }
            if (target != null) target.localScale = originalScale;
        }
    }
}
