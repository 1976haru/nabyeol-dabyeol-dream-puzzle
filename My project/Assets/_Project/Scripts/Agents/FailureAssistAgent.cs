using System;
using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Agents
{
    /// <summary>
    /// 스테이지별 실패 횟수를 추적하고, 같은 스테이지 3회 연속 실패 시 부드러운 도움을 제공하는 에이전트.
    /// - 도움은 자동 클리어가 아니라 힌트/격려/예약 힌트 표시로 한정.
    /// - 실패 횟수는 PlayerPrefs에 stageId별로 저장되어 앱 재시작 후에도 유지.
    /// - 클리어 시 해당 스테이지 실패 횟수 초기화 (resetFailCountOnClear=true 정책).
    /// - FailPopup 상태에서는 보드 입력이 막혀 즉시 힌트가 불가능 → 다음 스테이지 시작 시 자동 표시 예약.
    /// TODO: Offer extra move after repeated failures.
    /// TODO: Reduce target score temporarily for accessibility mode.
    /// TODO: Connect to CharacterUIManager dialogue (Capymong/Nabyeol soothing lines after 3+ fails).
    /// </summary>
    public class FailureAssistAgent : MonoBehaviour
    {
        public static FailureAssistAgent Instance { get; private set; }

        private const string FailCountKeyPrefix = "StageFailCount_";
        private const string PendingHintStageIdKey = "FailureAssist_PendingHintStageId";
        private const string KeyListPref = "FailureAssist_KeyList";
        private const char KeyListDelimiter = ';';

        [Header("Policy")]
        [SerializeField, Min(1)] private int assistThreshold = 3;
        [SerializeField] private bool resetFailCountOnClear = true;
        [SerializeField] private bool showHintOnAssist = true;
        [SerializeField] private bool offerExtraMoveOnAssist = false;

        [Header("Links (optional)")]
        [SerializeField] private HintAgent hintAgent;
        [SerializeField] private NabyeolDabyeolDreamPuzzle.UI.FailureAssistUI assistUI;

        public int AssistThreshold => assistThreshold;
        public bool ShowHintOnAssist => showHintOnAssist;
        public bool OfferExtraMoveOnAssist => offerExtraMoveOnAssist;

        /// <summary>(stageId, failCount)</summary>
        public event Action<int, int> OnFailCountChanged;
        /// <summary>(stageId, failCount) — 도움 제공이 트리거되는 순간.</summary>
        public event Action<int, int> OnAssistOffered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("FailureAssistAgent: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static string KeyOf(int stageId) => FailCountKeyPrefix + stageId;

        // ───────── KeyList 관리 (전체 초기화용) ─────────

        private HashSet<int> LoadKeyList()
        {
            HashSet<int> set = new HashSet<int>();
            string raw = PlayerPrefs.GetString(KeyListPref, string.Empty);
            if (string.IsNullOrEmpty(raw)) return set;
            string[] parts = raw.Split(KeyListDelimiter);
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int sid) && sid > 0) set.Add(sid);
            }
            return set;
        }

        private void SaveKeyList(HashSet<int> set)
        {
            PlayerPrefs.SetString(KeyListPref, string.Join(KeyListDelimiter.ToString(), set));
        }

        private void RegisterKey(int stageId)
        {
            HashSet<int> set = LoadKeyList();
            if (set.Add(stageId)) SaveKeyList(set);
        }

        private void UnregisterKey(int stageId)
        {
            HashSet<int> set = LoadKeyList();
            if (set.Remove(stageId)) SaveKeyList(set);
        }

        // ───────── 실패/클리어 기록 ─────────

        /// <summary>실패 확정 시 BoardManager.CheckStageFail의 isStageFailed false→true 트랜지션에서 1회 호출.</summary>
        public void RecordStageFail(int stageId)
        {
            if (stageId <= 0)
            {
                Debug.LogWarning($"FailureAssistAgent: RecordStageFail invalid stageId {stageId}.");
                return;
            }
            int next = GetFailCount(stageId) + 1;
            PlayerPrefs.SetInt(KeyOf(stageId), next);
            RegisterKey(stageId);
            PlayerPrefs.Save();
            Debug.Log($"FailureAssistAgent: Stage fail recorded: stageId={stageId}, count={next}.");
            OnFailCountChanged?.Invoke(stageId, next);
            if (next >= assistThreshold)
            {
                TryOfferAssist(stageId, next);
            }
        }

        /// <summary>클리어 확정 시 BoardManager.CheckStageClear에서 호출. 정책에 따라 실패 횟수 초기화.</summary>
        public void RecordStageClear(int stageId)
        {
            if (stageId <= 0) return;
            if (!resetFailCountOnClear) return;
            int prev = GetFailCount(stageId);
            ResetFailCount(stageId);
            // 클리어 시 예약된 힌트도 의미 없음 → 정리
            if (PlayerPrefs.GetInt(PendingHintStageIdKey, -1) == stageId)
            {
                ClearPendingHint();
            }
            if (prev > 0)
            {
                Debug.Log($"FailureAssistAgent: Stage cleared → fail count reset for stageId={stageId} (was {prev}).");
            }
        }

        public int GetFailCount(int stageId)
        {
            if (stageId <= 0) return 0;
            return PlayerPrefs.GetInt(KeyOf(stageId), 0);
        }

        public void ResetFailCount(int stageId)
        {
            if (stageId <= 0) return;
            if (!PlayerPrefs.HasKey(KeyOf(stageId))) return;
            PlayerPrefs.DeleteKey(KeyOf(stageId));
            UnregisterKey(stageId);
            PlayerPrefs.Save();
            OnFailCountChanged?.Invoke(stageId, 0);
        }

        public string GetFailCountMessage(int stageId)
        {
            return $"이번 스테이지 도전 실패 {GetFailCount(stageId)}회";
        }

        [ContextMenu("Reset All Failure Counts")]
        public void ResetAllFailureCounts()
        {
            HashSet<int> set = LoadKeyList();
            int cleared = 0;
            foreach (int sid in set)
            {
                if (PlayerPrefs.HasKey(KeyOf(sid)))
                {
                    PlayerPrefs.DeleteKey(KeyOf(sid));
                    cleared++;
                }
            }
            PlayerPrefs.DeleteKey(KeyListPref);
            PlayerPrefs.DeleteKey(PendingHintStageIdKey);
            PlayerPrefs.Save();
            Debug.Log($"FailureAssistAgent: Reset {cleared} failure counts and pending hint state.");
        }

        // ───────── 도움 제공 ─────────

        /// <summary>외부에서도 강제 트리거 가능. 보통은 RecordStageFail 내부에서 자동 호출.</summary>
        public void TryOfferAssist(int stageId)
        {
            int count = GetFailCount(stageId);
            if (count >= assistThreshold)
            {
                OfferAssist(stageId, count);
            }
        }

        private void TryOfferAssist(int stageId, int failCount)
        {
            OfferAssist(stageId, failCount);
        }

        private void OfferAssist(int stageId, int failCount)
        {
            Debug.Log($"FailureAssistAgent: Assist offered for stageId={stageId} (count={failCount}).");
            OnAssistOffered?.Invoke(stageId, failCount);

            // 1) 도움 UI 표시 (assigned이면)
            if (assistUI != null)
            {
                assistUI.ShowAssist(stageId, failCount);
            }
            else
            {
                Debug.Log("FailureAssistAgent: assistUI not assigned. Assist will rely on hint reservation only.");
            }

            // 2) 다음 도전 시 힌트 자동 표시 예약 (FailPopup 상태에서는 즉시 힌트 어려움)
            if (showHintOnAssist)
            {
                RequestHintOnNextStart(stageId);
            }

            // 3) 84번 — 난이도 완화 에이전트에 반복 실패 알림. 다음 도전에서 이동 횟수 보너스 자동 적용.
            if (DifficultyReliefAgent.Instance != null)
            {
                DifficultyReliefAgent.Instance.NotifyRepeatedFailure(stageId, failCount);
            }
        }

        // ───────── 예약 힌트 ─────────

        public void RequestHintOnNextStart(int stageId)
        {
            if (stageId <= 0) return;
            PlayerPrefs.SetInt(PendingHintStageIdKey, stageId);
            PlayerPrefs.Save();
            Debug.Log($"FailureAssistAgent: Hint reserved for next start of stage {stageId}.");
        }

        public bool ShouldShowHintOnStageStart(int stageId)
        {
            if (stageId <= 0) return false;
            return PlayerPrefs.GetInt(PendingHintStageIdKey, -1) == stageId;
        }

        public void ClearPendingHint()
        {
            if (PlayerPrefs.HasKey(PendingHintStageIdKey))
            {
                PlayerPrefs.DeleteKey(PendingHintStageIdKey);
                PlayerPrefs.Save();
                Debug.Log("FailureAssistAgent: Pending hint cleared.");
            }
        }

        /// <summary>
        /// 지금 즉시 힌트 표시 시도. 보드 상태가 허용하면 HintAgent.ShowHint, 아니면 다음 시작 시 예약.
        /// FailureAssistUI의 "힌트 보기" 버튼이 호출.
        /// </summary>
        public bool TryShowHintNow(int stageId)
        {
            if (hintAgent != null)
            {
                bool ok = hintAgent.ShowHint();
                if (ok)
                {
                    Debug.Log($"FailureAssistAgent: Hint shown immediately for stage {stageId}.");
                    return true;
                }
            }
            else
            {
                Debug.LogWarning("FailureAssistAgent: hintAgent not assigned. Reserving for next start.");
            }
            RequestHintOnNextStart(stageId);
            return false;
        }
    }
}
