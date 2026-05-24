using System;
using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Puzzle;
using NabyeolDabyeolDreamPuzzle.Sound;

namespace NabyeolDabyeolDreamPuzzle.Skill
{
    /// <summary>모든 캐릭터 스킬의 식별자. 사용 횟수 제한/UI 갱신/분석 공용 키.</summary>
    public enum SkillType
    {
        NabyeolHint = 0,
        DabyeolMove = 1,
        TwinStarPop = 2,
        CapymongBreath = 3,
        PoporingBubbleHint = 4,
        MochirunNumberSort = 5,
        // None은 enum 끝에 둬서 기존 정수값(0~5)이 유지되도록 한다. 노노처럼 플레이어 스킬이 없는 캐릭터용.
        None = 6
    }

    /// <summary>
    /// UI(스킬 버튼)와 퍼즐 로직(BoardManager) 사이의 얇은 연결 계층.
    /// 실제 알고리즘은 BoardManager·HintAgent가 보유하고, 본 매니저는 진입점 라우팅 +
    /// 스테이지당 스킬 사용 횟수 관리(통합 소유자)를 담당한다.
    /// TODO: Add per-skill cooldown / gauge integration.
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        public static SkillManager Instance { get; private set; }

        [SerializeField] private BoardManager boardManager;

        [Header("Skill Use Limits (per stage)")]
        [SerializeField, Min(0)] private int maxNabyeolHintCount = 3;
        [SerializeField, Min(0)] private int maxDabyeolMoveCount = 2;
        [SerializeField, Min(0)] private int maxTwinStarPopCount = 1;
        [SerializeField, Min(0)] private int maxCapymongBreathCount = 1;
        [SerializeField, Min(0)] private int maxPoporingBubbleHintCount = 2;
        [SerializeField, Min(0)] private int maxMochirunNumberSortCount = 1;

        private Dictionary<SkillType, int> maxUseCounts;
        private Dictionary<SkillType, int> usedCounts;

        /// <summary>스킬 사용 횟수 변동 시 발행. arg2 = 남은 횟수.</summary>
        public event Action<SkillType, int> OnSkillUseCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("SkillManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeUseLimits();
        }

        private void InitializeUseLimits()
        {
            maxUseCounts = new Dictionary<SkillType, int>
            {
                { SkillType.NabyeolHint, maxNabyeolHintCount },
                { SkillType.DabyeolMove, maxDabyeolMoveCount },
                { SkillType.TwinStarPop, maxTwinStarPopCount },
                { SkillType.CapymongBreath, maxCapymongBreathCount },
                { SkillType.PoporingBubbleHint, maxPoporingBubbleHintCount },
                { SkillType.MochirunNumberSort, maxMochirunNumberSortCount }
            };
            usedCounts = new Dictionary<SkillType, int>();
            foreach (SkillType t in Enum.GetValues(typeof(SkillType)))
            {
                usedCounts[t] = 0;
                // SkillType.None은 maxUseCounts에 등록되지 않으므로 안전하게 TryGetValue.
                if (maxUseCounts.TryGetValue(t, out int maxCount))
                {
                    Debug.Log($"SkillManager: Skill limit registered: {t} max {maxCount}");
                }
            }
        }

        /// <summary>해당 스킬을 한 번 더 사용할 수 있는지 (남은 횟수 > 0).</summary>
        public bool HasRemainingSkillUseCount(SkillType skill)
        {
            return GetRemainingSkillUseCount(skill) > 0;
        }

        /// <summary>해당 스킬의 남은 사용 횟수.</summary>
        public int GetRemainingSkillUseCount(SkillType skill)
        {
            if (maxUseCounts == null || usedCounts == null) return 0;
            int max = maxUseCounts.TryGetValue(skill, out int m) ? m : 0;
            int used = usedCounts.TryGetValue(skill, out int u) ? u : 0;
            return Mathf.Max(0, max - used);
        }

        /// <summary>해당 스킬의 최대 사용 횟수.</summary>
        public int GetMaxSkillUseCount(SkillType skill)
        {
            if (maxUseCounts == null) return 0;
            return maxUseCounts.TryGetValue(skill, out int m) ? m : 0;
        }

        /// <summary>
        /// 스킬 효과가 실제로 성공한 시점에만 호출한다. usedCount를 1 증가시키고 이벤트 발행.
        /// 동일 스킬의 사용 시점은 SkillManager.UseXxx 또는 BoardManager 코루틴 내부에서 일관되게 호출한다.
        /// </summary>
        public void NotifySkillConsumed(SkillType skill)
        {
            if (usedCounts == null) InitializeUseLimits();
            if (!usedCounts.ContainsKey(skill)) usedCounts[skill] = 0;
            usedCounts[skill]++;
            int remaining = GetRemainingSkillUseCount(skill);
            Debug.Log($"SkillManager: {skill} consumed. Remaining: {remaining}");
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx(SfxType.Skill);
            OnSkillUseCountChanged?.Invoke(skill, remaining);
        }

        /// <summary>스테이지 시작/재시작 시 모든 스킬 사용 횟수를 0으로 초기화한다.</summary>
        public void ResetSkillUseCountsForStage()
        {
            if (usedCounts == null) InitializeUseLimits();
            foreach (SkillType t in Enum.GetValues(typeof(SkillType)))
            {
                usedCounts[t] = 0;
            }
            Debug.Log("SkillManager: Skill use counts reset for new stage.");

            // 구독자들이 남은 횟수를 다시 갱신하도록 전 스킬에 이벤트 발행.
            foreach (SkillType t in Enum.GetValues(typeof(SkillType)))
            {
                OnSkillUseCountChanged?.Invoke(t, GetRemainingSkillUseCount(t));
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            EnsureBoardManager();
        }

        private void EnsureBoardManager()
        {
            if (boardManager == null)
            {
                boardManager = FindAnyObjectByType<BoardManager>();
            }
            if (boardManager == null)
            {
                Debug.LogWarning("SkillManager: BoardManager reference is missing.");
            }
        }

        /// <summary>
        /// 나별 "별자리 보기" 스킬 사용 요청. 보드 상태에 따라 거부될 수 있다.
        /// true 반환: 힌트 표시 시작 (애니메이션은 BoardManager 코루틴에서 진행).
        /// false 반환: 입력 잠금/클리어/실패/힌트 진행 중이거나 매치 가능한 스왑이 없음.
        /// </summary>
        public bool UseNabyeolHintSkill()
        {
            EnsureBoardManager();
            if (boardManager == null)
            {
                Debug.LogWarning("SkillManager: BoardManager not found. Hint skill cannot run.");
                return false;
            }

            if (!HasRemainingSkillUseCount(SkillType.NabyeolHint))
            {
                Debug.LogWarning("SkillManager: Skill limit reached: NabyeolHint");
                return false;
            }

            if (!boardManager.CanUseHintSkill())
            {
                Debug.LogWarning("SkillManager: Cannot use Nabyeol hint skill right now.");
                return false;
            }

            bool ok = boardManager.ShowHint();
            if (ok) NotifySkillConsumed(SkillType.NabyeolHint);
            return ok;
        }

        /// <summary>
        /// 다별 "꿈결 움직이기" 스킬 사용 요청. BoardManager가 스킬 이동 모드에 진입하면 true.
        /// 진입 후에는 OnBlockClicked가 자동으로 스킬 선택/실행으로 라우팅된다.
        /// </summary>
        public bool UseDabyeolMoveSkill()
        {
            EnsureBoardManager();
            if (boardManager == null)
            {
                Debug.LogWarning("SkillManager: BoardManager not found. Move skill cannot run.");
                return false;
            }

            if (!HasRemainingSkillUseCount(SkillType.DabyeolMove))
            {
                Debug.LogWarning("SkillManager: Skill limit reached: DabyeolMove");
                return false;
            }

            if (!boardManager.CanUseMoveSkill())
            {
                Debug.LogWarning("SkillManager: Cannot use Dabyeol move skill right now.");
                return false;
            }

            // B안: 실제 스왑 실행 시점에 BoardManager.ExecuteMoveSkillSwapRoutine이 NotifySkillConsumed를 호출.
            return boardManager.EnterMoveSkillMode();
        }

        /// <summary>외부 UI에서 다별 스킬 모드를 취소할 때 호출한다.</summary>
        public void CancelDabyeolMoveSkill()
        {
            EnsureBoardManager();
            if (boardManager == null)
            {
                return;
            }
            boardManager.CancelMoveSkillMode();
        }

        /// <summary>
        /// 합동 "트윈스타 팡" 스킬 사용 요청. BoardManager가 색상 선택 모드에 진입하면 true 반환.
        /// 진입 후 OnBlockClicked가 자동으로 색상 선택 → 일괄 제거로 라우팅한다.
        /// </summary>
        public bool UseTwinStarPopSkill()
        {
            EnsureBoardManager();
            if (boardManager == null)
            {
                Debug.LogWarning("SkillManager: BoardManager not found. Twin skill cannot run.");
                return false;
            }

            if (!HasRemainingSkillUseCount(SkillType.TwinStarPop))
            {
                Debug.LogWarning("SkillManager: Skill limit reached: TwinStarPop");
                return false;
            }

            if (!boardManager.CanUseColorClearSkill())
            {
                Debug.LogWarning("SkillManager: Cannot use Twin Star skill right now.");
                return false;
            }

            // B안: 실제 제거 성공 시점에 BoardManager.ExecuteColorClearSkillRoutine이 NotifySkillConsumed를 호출.
            return boardManager.EnterColorClearSkillMode();
        }

        /// <summary>외부 UI에서 합동 스킬 모드를 취소할 때 호출한다.</summary>
        public void CancelTwinStarPopSkill()
        {
            EnsureBoardManager();
            if (boardManager == null)
            {
                return;
            }
            boardManager.CancelColorClearSkillMode();
        }

        /// <summary>
        /// 카피몽 "느긋한 숨결" 스킬. 현재 스테이지의 남은 이동 횟수를 1 증가시킨다.
        /// 스테이지당 1회만 성공하며, 보드/점수/목표에는 영향을 주지 않는다.
        /// </summary>
        public bool UseCapymongBreathSkill()
        {
            EnsureBoardManager();
            if (boardManager == null)
            {
                Debug.LogWarning("SkillManager: BoardManager not found. Capymong skill cannot run.");
                return false;
            }

            if (!HasRemainingSkillUseCount(SkillType.CapymongBreath))
            {
                Debug.LogWarning("SkillManager: Skill limit reached: CapymongBreath");
                return false;
            }

            if (!boardManager.CanUseAddMoveSkill())
            {
                Debug.LogWarning("SkillManager: Cannot use Capymong breath skill right now.");
                return false;
            }

            bool ok = boardManager.AddMoveBySkill(1);
            if (ok) NotifySkillConsumed(SkillType.CapymongBreath);
            return ok;
        }

        /// <summary>
        /// 포포링 "방울 힌트" 스킬. 52번 나별 힌트 탐색 로직을 재사용해 후보 한 쌍을 찾고,
        /// 부드러운 방울 모션으로 두 블록을 강조한다. 스테이지당 1회 제한.
        /// </summary>
        public bool UsePoporingBubbleHintSkill()
        {
            EnsureBoardManager();
            if (boardManager == null)
            {
                Debug.LogWarning("SkillManager: BoardManager not found. Bubble hint skill cannot run.");
                return false;
            }

            if (!HasRemainingSkillUseCount(SkillType.PoporingBubbleHint))
            {
                Debug.LogWarning("SkillManager: Skill limit reached: PoporingBubbleHint");
                return false;
            }

            if (!boardManager.CanUseBubbleHintSkill())
            {
                Debug.LogWarning("SkillManager: Cannot use Poporing bubble hint skill right now.");
                return false;
            }

            bool ok = boardManager.ShowBubbleHint();
            if (ok) NotifySkillConsumed(SkillType.PoporingBubbleHint);
            return ok;
        }

        /// <summary>
        /// 모찌룬 "숫자 블록 정렬" 스킬. 가장 많은 타입을 가로 3칸에 모아 매치를 만든다.
        /// 정렬 성공 시 후속 매치/제거/낙하/연쇄/점수까지 자동으로 이어진다. 스테이지당 1회 제한.
        /// </summary>
        public bool UseMochirunNumberSortSkill()
        {
            EnsureBoardManager();
            if (boardManager == null)
            {
                Debug.LogWarning("SkillManager: BoardManager not found. Number sort skill cannot run.");
                return false;
            }

            if (!HasRemainingSkillUseCount(SkillType.MochirunNumberSort))
            {
                Debug.LogWarning("SkillManager: Skill limit reached: MochirunNumberSort");
                return false;
            }

            if (!boardManager.CanUseNumberSortSkill())
            {
                Debug.LogWarning("SkillManager: Cannot use Mochirun number sort skill right now.");
                return false;
            }

            // 실제 정렬 성공 시점에 BoardManager.ExecuteNumberSortRoutine이 NotifySkillConsumed를 호출.
            return boardManager.SortNumberBlocksBySkill();
        }
    }
}
