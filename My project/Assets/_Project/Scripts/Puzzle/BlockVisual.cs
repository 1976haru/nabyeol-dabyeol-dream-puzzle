using System.Collections;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Puzzle
{
    /// <summary>
    /// Block의 시각 표현(스프라이트 색, 선택 시 확대, 제거 효과)을 담당하는 컴포넌트.
    /// 제거 효과는 외부 플러그인 없이 scale 확대 + alpha 페이드만으로 구현한다.
    /// </summary>
    [RequireComponent(typeof(Block))]
    public class BlockVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float selectedScaleMultiplier = 1.12f;

        [Header("Remove Effect")]
        [SerializeField, Min(0.01f)] private float removeEffectDuration = 0.18f;
        [SerializeField, Min(1f)] private float removeScaleMultiplier = 1.35f;

        private Block block;
        private Vector3 defaultScale;
        private Coroutine removeRoutine;

        private void Awake()
        {
            block = GetComponent<Block>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            defaultScale = transform.localScale;
            Refresh();
        }

        /// <summary>현재 Block 상태(Type, IsSelected)에 맞춰 색·스케일을 다시 적용한다.</summary>
        public void Refresh()
        {
            if (block == null)
            {
                return;
            }
            ApplyType(block.Type);
            SetSelected(block.IsSelected);
        }

        /// <summary>타입에 맞춰 스프라이트 색을 적용한다. ColorFor가 alpha를 회복하므로 제거 효과 후 재사용해도 안전하다.</summary>
        public void ApplyType(BlockType type)
        {
            if (spriteRenderer == null)
            {
                return;
            }
            spriteRenderer.color = ColorFor(type);
        }

        /// <summary>선택 상태를 시각적으로 반영한다.</summary>
        public void SetSelected(bool selected)
        {
            transform.localScale = selected ? defaultScale * selectedScaleMultiplier : defaultScale;
        }

        /// <summary>
        /// 제거 효과 코루틴. 현재 localScale을 기준으로 removeScaleMultiplier 배까지 확대하면서
        /// alpha를 0으로 페이드한다. spriteRenderer가 없으면 즉시 종료한다.
        /// </summary>
        public IEnumerator PlayRemoveEffectRoutine()
        {
            if (spriteRenderer == null)
            {
                yield break;
            }

            Vector3 startScale = transform.localScale;
            Vector3 endScale = startScale * removeScaleMultiplier;
            Color startColor = spriteRenderer.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            float duration = Mathf.Max(0.0001f, removeEffectDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                transform.localScale = Vector3.LerpUnclamped(startScale, endScale, t);

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(startColor, endColor, t);
                }

                yield return null;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = endColor;
            }
        }

        /// <summary>
        /// 제거 효과를 재생한 뒤 GameObject를 Destroy한다.
        /// 진행 중인 제거 코루틴이 있으면 중지하고 새로 시작한다.
        /// </summary>
        public void PlayRemoveEffectThenDestroy()
        {
            if (removeRoutine != null)
            {
                StopCoroutine(removeRoutine);
                removeRoutine = null;
            }
            removeRoutine = StartCoroutine(RemoveAndDestroyRoutine());
        }

        private IEnumerator RemoveAndDestroyRoutine()
        {
            yield return StartCoroutine(PlayRemoveEffectRoutine());
            removeRoutine = null;
            Object.Destroy(gameObject);
        }

        private Color ColorFor(BlockType t)
        {
            switch (t)
            {
                case BlockType.DreamBubble: return new Color(.45f, .85f, 1f, 1f);
                case BlockType.MoonRiceCake: return new Color(1f, .92f, .45f, 1f);
                case BlockType.InkStar: return new Color(.65f, .45f, 1f, 1f);
                case BlockType.WaveCloud: return new Color(.55f, .75f, 1f, 1f);
                case BlockType.HeartLight: return new Color(1f, .55f, .75f, 1f);
                case BlockType.Noise: return new Color(.18f, .18f, .22f, 1f);
                default: return new Color(.6f, .6f, .6f, .25f);
            }
        }
    }
}
