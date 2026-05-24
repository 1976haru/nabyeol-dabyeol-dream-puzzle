using System;
using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Cards;

namespace NabyeolDabyeolDreamPuzzle.Agents
{
    /// <summary>
    /// 학습 코치 v1. 오늘 얻은 지식카드 중 최대 3장을 골라 짧게 요약해 보여주는 에이전트.
    /// - 실제 AI API 호출 없음. KnowledgeCardData.shortText를 그대로 활용한다.
    /// - 오늘 얻은 카드 목록은 PlayerPrefs에 `|` 구분 문자열로 저장 + 중복 제거.
    /// - 앱 실행 또는 카드 기록 시점에 날짜를 확인해 자정 넘으면 자동 리셋.
    /// - 진행도/카드 보상 시스템과 별개로 동작 (rewardCardId만 읽어 기록).
    /// TODO: Use LearningPackManager.GetFirstCardByStageId as fallback when StageData.rewardCardId is empty.
    /// TODO: Switch from sequential to rarity-weighted selection when more than 3 cards.
    /// TODO: Add real AI summarization in v2 (요약을 더 짧고 자연스럽게).
    /// </summary>
    public class LearningCoachAgent : MonoBehaviour
    {
        public static LearningCoachAgent Instance { get; private set; }

        private const string LearningCoachDateKey = "LearningCoach_Date";
        private const string LearningCoachTodayCardsKey = "LearningCoach_TodayCards";
        private const char Delimiter = '|';
        private const string DateFormat = "yyyy-MM-dd";

        [Header("Database")]
        [SerializeField] private KnowledgeCardDatabase knowledgeCardDatabase;

        [Header("Summary")]
        [SerializeField, Min(1)] private int maxSummaryCount = 3;

        public event Action OnTodayCardsChanged;

        public int MaxSummaryCount => maxSummaryCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("LearningCoachAgent: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            RefreshTodayIfNeeded();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ───────── 날짜 ─────────

        private static string TodayString()
        {
            return DateTime.Now.ToString(DateFormat);
        }

        /// <summary>저장된 날짜가 오늘과 다르면 오늘 카드 목록을 리셋한다.</summary>
        public void RefreshTodayIfNeeded()
        {
            string today = TodayString();
            string saved = PlayerPrefs.GetString(LearningCoachDateKey, string.Empty);
            if (saved == today) return;

            PlayerPrefs.SetString(LearningCoachDateKey, today);
            PlayerPrefs.DeleteKey(LearningCoachTodayCardsKey);
            PlayerPrefs.Save();
            Debug.Log($"LearningCoachAgent: Day rolled over. Previous='{saved}', new='{today}'. Today card list reset.");
            OnTodayCardsChanged?.Invoke();
        }

        // ───────── 카드 기록 ─────────

        /// <summary>스테이지 클리어 시 보상 카드 ID를 오늘 카드 목록에 추가. 중복은 무시.</summary>
        public void RecordCardEarnedToday(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId)) return;
            RefreshTodayIfNeeded();

            List<string> ids = GetTodayCardIds();
            if (ids.Contains(cardId))
            {
                Debug.Log($"LearningCoachAgent: Duplicate card ignored ({cardId}).");
                return;
            }
            ids.Add(cardId);
            SaveTodayCardIds(ids);
            Debug.Log($"LearningCoachAgent: Card recorded today: {cardId} (total {ids.Count}).");
            OnTodayCardsChanged?.Invoke();
        }

        private void SaveTodayCardIds(List<string> ids)
        {
            PlayerPrefs.SetString(LearningCoachTodayCardsKey, string.Join(Delimiter.ToString(), ids));
            PlayerPrefs.Save();
        }

        public List<string> GetTodayCardIds()
        {
            RefreshTodayIfNeeded();
            string raw = PlayerPrefs.GetString(LearningCoachTodayCardsKey, string.Empty);
            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(raw)) return result;
            string[] parts = raw.Split(Delimiter);
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                if (string.IsNullOrWhiteSpace(p)) continue;
                if (seen.Add(p)) result.Add(p);
            }
            return result;
        }

        /// <summary>오늘 얻은 모든 KnowledgeCardData. Database에 없는 cardId는 경고 후 skip.</summary>
        public List<KnowledgeCardData> GetTodayCards()
        {
            List<KnowledgeCardData> result = new List<KnowledgeCardData>();
            if (knowledgeCardDatabase == null)
            {
                Debug.LogWarning("LearningCoachAgent: knowledgeCardDatabase is not assigned.");
                return result;
            }
            List<string> ids = GetTodayCardIds();
            for (int i = 0; i < ids.Count; i++)
            {
                KnowledgeCardData c = knowledgeCardDatabase.FindByCardId(ids[i]);
                if (c == null)
                {
                    Debug.LogWarning($"LearningCoachAgent: cardId '{ids[i]}' not found in database. Skipping.");
                    continue;
                }
                result.Add(c);
            }
            return result;
        }

        /// <summary>요약에 사용할 카드 N장. 4장 이상이면 앞에서부터 maxSummaryCount만큼 자른다.</summary>
        public List<KnowledgeCardData> GetTodaySummaryCards(int maxCount = -1)
        {
            int limit = maxCount > 0 ? maxCount : maxSummaryCount;
            List<KnowledgeCardData> all = GetTodayCards();
            if (all.Count <= limit) return all;
            return all.GetRange(0, limit);
        }

        /// <summary>요약 한 줄 문자열 리스트. "카드명: shortText" 형태.</summary>
        public List<string> BuildTodaySummaryLines()
        {
            List<KnowledgeCardData> cards = GetTodaySummaryCards();
            List<string> lines = new List<string>(cards.Count);
            for (int i = 0; i < cards.Count; i++)
            {
                KnowledgeCardData c = cards[i];
                if (c == null) continue;
                string title = !string.IsNullOrWhiteSpace(c.CardName) ? c.CardName : c.CardId;
                string body = !string.IsNullOrWhiteSpace(c.ShortText) ? c.ShortText : string.Empty;
                lines.Add($"{title}: {body}");
            }
            return lines;
        }

        // ───────── 디버그 ─────────

        [ContextMenu("Reset Today Learning Coach Cards")]
        public void ResetTodayCardsForDebug()
        {
            PlayerPrefs.DeleteKey(LearningCoachTodayCardsKey);
            PlayerPrefs.SetString(LearningCoachDateKey, TodayString());
            PlayerPrefs.Save();
            Debug.Log("LearningCoachAgent: Today card list cleared (debug).");
            OnTodayCardsChanged?.Invoke();
        }

        [ContextMenu("Debug Add Sample Today Cards")]
        public void DebugAddSampleTodayCards()
        {
            RecordCardEarnedToday("card_rabbit_001");
            RecordCardEarnedToday("card_squirrel_001");
            RecordCardEarnedToday("card_number_star_001");
            Debug.Log("LearningCoachAgent: Sample cards added (debug).");
        }
    }
}
