using System;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Stage;

namespace NabyeolDabyeolDreamPuzzle.Agents
{
    /// <summary>
    /// 반복 실패 시 난이도를 아주 조금 낮춰주는 에이전트.
    /// - 원본 StageData(moveLimit/targetScore/targetBlockCount)는 절대 수정하지 않는다.
    /// - 런타임 보정값만 계산해서 BoardManager가 InitMoves/InitGoal 시 적용한다.
    /// - 실패 횟수는 FailureAssistAgent가 PlayerPrefs에 저장한 값을 읽는다 — 본 에이전트는 별도 저장 없음.
    /// - 클리어 시 FailureAssistAgent.RecordStageClear가 실패 횟수를 0으로 리셋 → 완화도 자동 해제.
    /// - 기본 정책: 3회 실패 → +1 / 4회 → +2 / 5회 이상 → +3 (maxExtraMoves 상한).
    /// - 목표 점수/수집 목표 완화는 기본 비활성 (enableGoalRelief=false).
    /// TODO: Add parent setting to enable/disable difficulty relief.
    /// TODO: Add parent setting for max extra moves.
    /// TODO: Add parent setting for goal relief.
    /// </summary>
    public class DifficultyReliefAgent : MonoBehaviour
    {
        public static DifficultyReliefAgent Instance { get; private set; }

        [Header("Threshold")]
        [SerializeField, Min(1)] private int reliefStartFailCount = 3;

        [Header("Move Relief")]
        [SerializeField] private bool enableMoveRelief = true;
        [SerializeField, Min(0)] private int extraMovePerStep = 1;
        [SerializeField, Min(0)] private int maxExtraMoves = 3;

        [Header("Goal Relief (default off)")]
        [SerializeField] private bool enableGoalRelief = false;
        [SerializeField, Range(0f, 0.5f)] private float targetScoreReductionRate = 0.1f; // step당 감소 비율
        [SerializeField, Range(0f, 0.5f)] private float maxScoreReductionRate = 0.20f;   // 누적 상한
        [SerializeField, Min(0)] private int targetBlockReductionCount = 2;              // step 1 기본 감소량
        [SerializeField, Min(1)] private int minTargetBlockFloor = 1;

        [Header("UI Link (optional)")]
        [SerializeField] private NabyeolDabyeolDreamPuzzle.UI.DifficultyReliefUI reliefUI;

        /// <summary>(stageId, extraMoves, adjustedTargetScore, adjustedTargetBlockCount)</summary>
        public event Action<int, int, int, int> OnReliefApplied;

        public int ReliefStartFailCount => reliefStartFailCount;
        public bool EnableMoveRelief => enableMoveRelief;
        public bool EnableGoalRelief => enableGoalRelief;
        public int MaxExtraMoves => maxExtraMoves;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("DifficultyReliefAgent: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ───────── Step 계산 ─────────

        /// <summary>실패 횟수로부터 완화 단계 계산. 3회=1, 4회=2, 5회=3, …, threshold 미만이면 0.</summary>
        public int GetReliefStep(int stageId)
        {
            if (stageId <= 0) return 0;
            int failCount = FailureAssistAgent.Instance != null
                ? FailureAssistAgent.Instance.GetFailCount(stageId)
                : 0;
            if (failCount < reliefStartFailCount) return 0;
            return failCount - (reliefStartFailCount - 1);
        }

        public bool HasReliefForStage(int stageId)
        {
            return GetReliefStep(stageId) > 0;
        }

        // ───────── 이동 횟수 완화 ─────────

        /// <summary>이번 도전에 추가할 이동 횟수. enableMoveRelief=false이면 항상 0.</summary>
        public int GetExtraMovesForStage(int stageId)
        {
            if (!enableMoveRelief) return 0;
            int step = GetReliefStep(stageId);
            if (step <= 0) return 0;
            int extra = step * extraMovePerStep;
            return Mathf.Clamp(extra, 0, maxExtraMoves);
        }

        // ───────── 목표 점수 완화 ─────────

        /// <summary>목표 점수 보정. enableGoalRelief=false이면 원본 반환.</summary>
        public int GetAdjustedTargetScore(int stageId, int originalTargetScore)
        {
            if (!enableGoalRelief) return originalTargetScore;
            if (originalTargetScore <= 0) return originalTargetScore;
            int step = GetReliefStep(stageId);
            if (step <= 0) return originalTargetScore;
            float rate = Mathf.Min(step * targetScoreReductionRate, maxScoreReductionRate);
            int reduced = Mathf.RoundToInt(originalTargetScore * (1f - rate));
            return Mathf.Max(1, reduced);
        }

        // ───────── 수집 목표 완화 ─────────

        /// <summary>수집 목표 개수 보정. enableGoalRelief=false이면 원본 반환.</summary>
        public int GetAdjustedTargetBlockCount(int stageId, int originalTargetBlockCount)
        {
            if (!enableGoalRelief) return originalTargetBlockCount;
            if (originalTargetBlockCount <= 0) return originalTargetBlockCount;
            int step = GetReliefStep(stageId);
            if (step <= 0) return originalTargetBlockCount;
            // 3회=-2, 4회=-3, 5회=-4 ... (step + targetBlockReductionCount - 1)
            int reduction = (step - 1) + targetBlockReductionCount;
            int reduced = originalTargetBlockCount - reduction;
            return Mathf.Max(minTargetBlockFloor, reduced);
        }

        // ───────── 알림/안내 ─────────

        /// <summary>FailureAssistAgent.OfferAssist에서 호출. 다음 도전 시 적용될 완화값을 로그/이벤트로 안내.</summary>
        public void NotifyRepeatedFailure(int stageId, int failCount)
        {
            if (stageId <= 0) return;
            if (failCount < reliefStartFailCount) return;

            int extraMoves = GetExtraMovesForStage(stageId);
            int adjScore = -1, adjBlock = -1;
            if (StageManager.Instance != null && StageManager.Instance.CurrentStageData != null
                && StageManager.Instance.CurrentStageData.StageId == stageId)
            {
                StageData sd = StageManager.Instance.CurrentStageData;
                adjScore = GetAdjustedTargetScore(stageId, sd.TargetScore);
                adjBlock = GetAdjustedTargetBlockCount(stageId, sd.TargetBlockCount);
            }
            Debug.Log($"DifficultyReliefAgent: Repeated failure stage={stageId}, failCount={failCount} → next attempt extraMoves=+{extraMoves}, adjustedTargetScore={adjScore}, adjustedTargetBlockCount={adjBlock}.");
            OnReliefApplied?.Invoke(stageId, extraMoves, adjScore, adjBlock);
            // UI 안내는 다음 스테이지 시작 시 BoardManager.InitializeBoard에서 TryShowReliefInfo로 표시.
        }

        /// <summary>BoardManager가 스테이지 시작 시 호출. 완화가 적용된 상태이면 안내 UI를 띄운다.</summary>
        public void TryShowReliefInfoForStage(int stageId)
        {
            if (!HasReliefForStage(stageId)) return;
            int extraMoves = GetExtraMovesForStage(stageId);

            int adjScore = -1, adjBlock = -1;
            if (StageManager.Instance != null && StageManager.Instance.CurrentStageData != null
                && StageManager.Instance.CurrentStageData.StageId == stageId)
            {
                StageData sd = StageManager.Instance.CurrentStageData;
                adjScore = GetAdjustedTargetScore(stageId, sd.TargetScore);
                adjBlock = GetAdjustedTargetBlockCount(stageId, sd.TargetBlockCount);
            }

            if (reliefUI != null)
            {
                reliefUI.ShowReliefInfo(stageId, extraMoves, adjScore, adjBlock);
            }
            else
            {
                Debug.Log($"DifficultyReliefAgent: reliefUI not assigned. (stage={stageId}, extra=+{extraMoves})");
            }
        }

        /// <summary>클리어 시 후크. 실패 횟수는 FailureAssistAgent가 리셋하므로 본 에이전트는 별도 저장 없음.</summary>
        public void RecordStageClear(int stageId)
        {
            // 실패 카운트는 FailureAssistAgent가 초기화 → GetReliefStep도 자연스레 0으로 떨어짐.
            Debug.Log($"DifficultyReliefAgent: Stage clear for {stageId}. Relief state reset implicitly.");
        }

        [ContextMenu("Debug Print Current Stage Relief")]
        public void DebugPrintCurrentStageRelief()
        {
            if (StageManager.Instance == null || StageManager.Instance.CurrentStageData == null)
            {
                Debug.Log("DifficultyReliefAgent: No current stage.");
                return;
            }
            int sid = StageManager.Instance.CurrentStageData.StageId;
            int failCount = FailureAssistAgent.Instance != null
                ? FailureAssistAgent.Instance.GetFailCount(sid)
                : 0;
            int step = GetReliefStep(sid);
            int extra = GetExtraMovesForStage(sid);
            StageData sd = StageManager.Instance.CurrentStageData;
            int adjScore = GetAdjustedTargetScore(sid, sd.TargetScore);
            int adjBlock = GetAdjustedTargetBlockCount(sid, sd.TargetBlockCount);
            Debug.Log($"DifficultyReliefAgent: stage={sid}, failCount={failCount}, step={step}, extraMoves=+{extra}, targetScore={sd.TargetScore}→{adjScore}, targetBlockCount={sd.TargetBlockCount}→{adjBlock}");
        }
    }
}
