using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Puzzle;

namespace NabyeolDabyeolDreamPuzzle.Stage
{
    /// <summary>
    /// 스테이지 클리어 목표의 종류. 이번 단계에서는 데이터만 정의하고
    /// 실제 판정은 후속 단계(GoalManager 확장)에서 추가한다.
    /// </summary>
    public enum StageGoalType
    {
        Score = 0,
        CollectBlock = 1,
        ClearObstacle = 2
    }

    /// <summary>
    /// 보스형 퍼즐 스테이지 종류. None은 일반 스테이지.
    /// 보스 전투/HP/애니메이션은 이번 단계 범위가 아니며, 데이터 분류와 UI 표시 목적이다.
    /// </summary>
    public enum BossStageType
    {
        None = 0,
        MemoryTree = 1,
        ReverseClockTower = 2
    }

    /// <summary>
    /// 한 스테이지의 정적 데이터(목표·이동 횟수·등장 블록·보상 카드 등)를 Inspector에서 편집 가능한
    /// ScriptableObject로 보관한다. 런타임 상태(현재 점수·남은 이동수)는 여기서 다루지 않는다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StageData",
        menuName = "NabyeolDabyeol/Stage Data",
        order = 100)]
    public class StageData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Min(1)] private int stageId = 1;
        [SerializeField] private string stageName;
        [TextArea(2, 5)]
        [SerializeField] private string description;

        [Header("Goal")]
        [SerializeField] private StageGoalType goalType = StageGoalType.Score;
        [SerializeField, Min(0)] private int targetScore = 500;
        // goalType == CollectBlock일 때만 의미를 갖는다.
        [SerializeField] private BlockType targetBlockType = BlockType.DreamBubble;
        [SerializeField, Min(0)] private int targetBlockCount;

        [Header("Limits")]
        [SerializeField, Min(1)] private int moveLimit = 25;

        [Header("Board")]
        [SerializeField, Min(3)] private int boardWidth = 6;
        [SerializeField, Min(3)] private int boardHeight = 6;
        // TODO: BoardManager.spawnableTypes / activeSpawnTypeCount와 후속 단계에서 연결한다.
        // (StageManager.CurrentStage.AvailableBlockTypes를 읽어 BoardManager에 주입할 예정)
        [SerializeField]
        private List<BlockType> availableBlockTypes = new List<BlockType>
        {
            BlockType.DreamBubble,
            BlockType.MoonRiceCake,
            BlockType.InkStar,
            BlockType.WaveCloud,
            BlockType.HeartLight
        };

        [Header("Boss")]
        [SerializeField] private bool isBossStage;
        [SerializeField] private BossStageType bossStageType = BossStageType.None;
        [TextArea(2, 4)]
        [SerializeField] private string specialGoalDescription;

        [Header("Reward")]
        // TODO: 클리어 시 SaveManager.UnlockLearningCardId(rewardCardId)와 연결할 예정.
        [SerializeField] private string rewardCardId;
        [SerializeField, Min(0)] private int rewardCardAmount = 1;
        [SerializeField, Min(0)] private int rewardSparklePieces;

        public int StageId => stageId;
        public string StageName => stageName;
        public string Description => description;

        public StageGoalType GoalType => goalType;
        public int TargetScore => targetScore;
        public BlockType TargetBlockType => targetBlockType;
        public int TargetBlockCount => targetBlockCount;

        public int MoveLimit => moveLimit;

        public int BoardWidth => boardWidth;
        public int BoardHeight => boardHeight;
        public IReadOnlyList<BlockType> AvailableBlockTypes => availableBlockTypes;

        public string RewardCardId => rewardCardId;
        public int RewardCardAmount => rewardCardAmount;
        public int RewardSparklePieces => rewardSparklePieces;

        public bool IsBossStage => isBossStage;
        public BossStageType BossStageType => bossStageType;
        public string SpecialGoalDescription => specialGoalDescription;

        /// <summary>이전 stub과 호환되는 별칭. rewardCardId와 동일한 값을 반환한다.</summary>
        public string LearningCardId => rewardCardId;

        /// <summary>등장 블록 종류 수(3~5로 클램프). BoardManager.activeSpawnTypeCount와 호환된다.</summary>
        public int SpawnTypeCount
        {
            get
            {
                int count = availableBlockTypes == null ? 0 : availableBlockTypes.Count;
                return Mathf.Clamp(count, 3, 5);
            }
        }

        /// <summary>
        /// 기본 데이터 유효성 검사. Editor와 런타임 양쪽에서 호출 가능하다.
        /// 음수 또는 빈 목록처럼 명백히 잘못된 값을 거른다.
        /// </summary>
        public bool IsValid()
        {
            if (stageId <= 0)
            {
                return false;
            }
            if (moveLimit <= 0)
            {
                return false;
            }
            if (boardWidth <= 0 || boardHeight <= 0)
            {
                return false;
            }
            if (availableBlockTypes == null || availableBlockTypes.Count == 0)
            {
                return false;
            }
            if (targetScore < 0)
            {
                return false;
            }
            if (targetBlockCount < 0)
            {
                return false;
            }
            if (rewardCardAmount < 0)
            {
                return false;
            }
            if (isBossStage && bossStageType == BossStageType.None)
            {
                return false;
            }
            return true;
        }
    }
}
