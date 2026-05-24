using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Agents;
using NabyeolDabyeolDreamPuzzle.Stage;
using NabyeolDabyeolDreamPuzzle.Skill;
using NabyeolDabyeolDreamPuzzle.Album;
using NabyeolDabyeolDreamPuzzle.Region;
using NabyeolDabyeolDreamPuzzle.Sound;
using NabyeolDabyeolDreamPuzzle.Animation;

namespace NabyeolDabyeolDreamPuzzle.Puzzle
{
    /// <summary>
    /// 6x6 매치-3 퍼즐 보드를 생성하고 전체 보드 상태(Block[,])를 관리한다.
    /// 이번 단계는 보드 생성/배열 관리/좌표 계산까지만 담당하며,
    /// 매치 판정, 블록 교환, 낙하, 입력, 점수 계산은 후속 단계에서 추가한다.
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField, Min(3)] private int rows = 6;
        [SerializeField, Min(3)] private int columns = 6;
        [SerializeField] private Block blockPrefab;
        [SerializeField] private Transform boardRoot;
        [SerializeField, Min(0.1f)] private float cellSize = 1.1f;
        [SerializeField] private bool generateOnStart = true;

        [Header("Spawn Types")]
        [SerializeField] private BlockType[] spawnableTypes =
        {
            BlockType.DreamBubble,
            BlockType.MoonRiceCake,
            BlockType.InkStar,
            BlockType.WaveCloud,
            BlockType.HeartLight
        };

        [SerializeField, Min(3)] private int activeSpawnTypeCount = 5;

        [Header("Hint / Solvability")]
        [SerializeField] private HintAgent hintAgent;
        [SerializeField, Min(1)] private int maxBoardRegenerationAttempts = 20;

        [Header("Swap")]
        [SerializeField] private BlockSwapper blockSwapper;

        [Header("Match")]
        [SerializeField] private MatchFinder matchFinder;

        [Header("Match Remove")]
        [SerializeField, Min(0.01f)] private float matchRemoveWaitTime = 0.2f;

        [Header("Drop")]
        [SerializeField] private BlockDropper blockDropper;

        [Header("New Block Spawn")]
        [SerializeField, Min(0.01f)] private float newBlockSpawnOffsetY = 1.5f;
        [SerializeField, Min(0.01f)] private float newBlockDropDuration = 0.2f;

        [Header("Cascade")]
        [SerializeField, Min(0f)] private float cascadeStabilizeDelay = 0.1f;

        private const int MaxCascadeCount = 20;

        [Header("Score")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField, Min(0)] private int blockBaseScore = 10;
        [SerializeField] private int score;

        [Header("Moves")]
        [SerializeField] private MoveManager moveManager;

        [Header("Goal")]
        [SerializeField] private TextMeshProUGUI goalText;
        [SerializeField, Min(0)] private int defaultTargetScore = 500;

        private StageGoalType currentGoalType = StageGoalType.Score;
        private int targetScore;
        private BlockType targetBlockType = BlockType.DreamBubble;
        private int targetBlockCount;
        private int currentCollectedBlockCount;
        private bool goalAchievedLogged;

        private bool isCurrentBossStage;
        private BossStageType currentBossStageType = BossStageType.None;
        private string currentStageName;

        [Header("Clear")]
        [SerializeField] private GameObject clearPopup;
        [SerializeField, Min(0f)] private float clearPopupDelay = 0.2f;

        private bool isStageCleared;
        private Coroutine clearPopupCoroutine;

        [Header("Fail")]
        [SerializeField] private GameObject failPopup;
        [SerializeField, Min(0f)] private float failPopupDelay = 0.2f;

        private bool isStageFailed;
        private Coroutine failPopupCoroutine;

        [Header("Skill Hint")]
        [SerializeField, Min(1)] private int hintPulseCount = 3;
        [SerializeField, Min(0.01f)] private float hintPulseDuration = 0.15f;
        [SerializeField, Min(1f)] private float hintScaleMultiplier = 1.2f;

        private bool isShowingHint;
        private Coroutine hintCoroutine;

        // 82번 — HintAgent v1 진입점용. 기존 isShowingHint(나별 스킬)와 별도로 두어
        // 에이전트 힌트 표시 중에는 다른 입력/스킬을 차단하고, 다른 스킬 중에는 에이전트 힌트를 막는다.
        private bool isAgentHintRunning;
        private Coroutine agentHintCoroutine;

        [Header("Skill Move (Dabyeol)")]
        // (Inspector-tunable 항목은 향후 cooldown/usage 추가 시 여기에 둔다.)

        private bool isMoveSkillMode;
        private Block moveSkillSelectedBlock;

        [Header("Skill Twin Star Pop")]
        // 0 이하 = 전체 제거. 양수면 최대 N개까지 제거.
        [SerializeField] private int twinSkillMaxRemoveCount = 12;

        private bool isColorClearSkillMode;
        private bool isColorClearSkillRunning;

        [Header("Skill Add Move (Capymong)")]
        [SerializeField, Min(1)] private int maxRemainingMoves = 99;

        [Header("Skill Bubble Hint (Poporing)")]
        [SerializeField] private GameObject bubbleHintPrefab;
        [SerializeField, Min(0.1f)] private float bubbleHintDuration = 1.5f;
        [SerializeField, Min(1f)] private float bubbleHintScaleAmplitude = 1.15f;

        private bool isShowingBubbleHint;
        private Coroutine bubbleHintCoroutine;

        [Header("Skill Number Sort (Mochirun)")]
        [SerializeField, Min(3)] private int numberSortTargetCount = 3;
        [SerializeField] private bool numberSortConsumesMove = false;

        private bool isNumberSortSkillRunning;

        private const int MinActiveSpawnTypeCount = 3;
        private const int MaxActiveSpawnTypeCount = 5;

        private Block[,] blocks;
        private Block selectedBlock;
        private bool isBusy;

        /// <summary>보드 행 수.</summary>
        public int Rows => rows;

        /// <summary>보드 열 수.</summary>
        public int Columns => columns;

        /// <summary>한 칸의 월드 단위 크기.</summary>
        public float CellSize => cellSize;

        /// <summary>현재 보드 배열 참조.</summary>
        public Block[,] Blocks => blocks;

        /// <summary>현재 보드에서 일반 생성에 사용 중인 블록 종류 수(보정 전 원본 값).</summary>
        public int ActiveSpawnTypeCount => activeSpawnTypeCount;

        /// <summary>현재 첫 번째로 선택된 블록. 없으면 null.</summary>
        public Block SelectedBlock => selectedBlock;

        /// <summary>교환 애니메이션 등 비동기 처리 중인지 여부. true 동안 입력은 무시한다.</summary>
        public bool IsBusy => isBusy;

        /// <summary>현재 누적 점수.</summary>
        public int Score => score;

        private void Awake()
        {
            ValidateBoardSize();
        }

        private void Start()
        {
            if (generateOnStart)
            {
                InitializeBoard();
            }
        }

        /// <summary>
        /// 보드를 초기화한다. 기존 블록을 정리하고 모든 칸에 새 블록을 생성한 뒤,
        /// 가능한 이동이 하나도 없는 보드라면 한도 안에서 다시 생성한다.
        /// </summary>
        public void InitializeBoard()
        {
            TryApplyStageData();
            ValidateBoardSize();
            EnsureHelpers();
            ResetScore();
            InitGoalFromStageData();
            InitClearPopup();
            InitFailPopup();
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.ResetSkillUseCountsForStage();
            }
            GenerateBoardOnce();

            if (blockPrefab == null)
            {
                return;
            }

            EnsureBoardHasAvailableMove();

            Debug.Log(GetSpawnDebugSummary());

            // 83번 — 직전 실패 도움 흐름에서 예약된 힌트가 있다면 보드가 안정된 뒤 자동 표시.
            StartCoroutine(TryShowReservedAssistHintRoutine());

            // 84번 — 난이도 완화가 적용된 스테이지라면 안내 UI 표시.
            if (DifficultyReliefAgent.Instance != null
                && StageManager.Instance != null
                && StageManager.Instance.CurrentStageData != null)
            {
                int sid = StageManager.Instance.CurrentStageData.StageId;
                if (DifficultyReliefAgent.Instance.HasReliefForStage(sid))
                {
                    DifficultyReliefAgent.Instance.TryShowReliefInfoForStage(sid);
                }
            }
        }

        /// <summary>
        /// 보드 생성 직후 짧게 대기한 뒤, FailureAssistAgent에 예약된 힌트가 현재 스테이지와 일치하면
        /// HintAgent.ShowHint를 자동 실행한다. 보드 클리어/실패/사용 불가 상태에서는 그냥 예약을 유지.
        /// </summary>
        private IEnumerator TryShowReservedAssistHintRoutine()
        {
            yield return new WaitForSeconds(0.3f);

            if (FailureAssistAgent.Instance == null) yield break;
            if (StageManager.Instance == null || StageManager.Instance.CurrentStageData == null) yield break;

            int sid = StageManager.Instance.CurrentStageData.StageId;
            if (!FailureAssistAgent.Instance.ShouldShowHintOnStageStart(sid)) yield break;

            if (hintAgent != null && CanUseHintAgent())
            {
                Debug.Log($"BoardManager: Showing reserved assist hint for stage {sid}.");
                hintAgent.ShowHint();
                FailureAssistAgent.Instance.ClearPendingHint();
            }
            else
            {
                Debug.Log($"BoardManager: Reserved hint for stage {sid} kept (board not ready or hintAgent missing).");
            }
        }

        /// <summary>
        /// StageManager가 존재하고 유효한 StageData를 가지고 있으면,
        /// 보드 크기·등장 블록 종류·활성 스폰 타입 수를 그 값으로 덮어쓴다.
        /// 매니저나 데이터가 없으면 기존 Inspector 기본값을 그대로 사용한다.
        /// 이번 단계에서는 moveLimit/targetScore는 로그만 출력하고 적용은 후속 단계에서 처리한다.
        /// </summary>
        private void TryApplyStageData()
        {
            if (StageManager.Instance == null)
            {
                Debug.LogWarning("BoardManager: StageManager.Instance not found. Using default settings.");
                return;
            }
            if (!StageManager.Instance.HasCurrentStage())
            {
                Debug.LogWarning("BoardManager: No valid StageData. Using default settings.");
                return;
            }

            StageData stage = StageManager.Instance.CurrentStageData;

            // TODO (#74): StagePackManager.Instance.GetBoardRuleByStageId(stage.StageId)에서
            // StageBoardRule fallback을 읽어와 stage.BoardWidth/Height/AvailableBlockTypes가 비어 있을 때
            // 공통 보드 규칙을 적용하도록 확장. 본 단계는 데이터 구조만 준비.
            rows = stage.BoardHeight;
            columns = stage.BoardWidth;

            IReadOnlyList<BlockType> available = stage.AvailableBlockTypes;
            if (available != null && available.Count > 0)
            {
                spawnableTypes = new BlockType[available.Count];
                for (int i = 0; i < available.Count; i++)
                {
                    spawnableTypes[i] = available[i];
                }
                SetActiveSpawnTypeCount(available.Count);
            }

            if (moveManager != null)
            {
                // 84번 — DifficultyReliefAgent의 추가 이동 횟수를 더한다. StageData 원본은 수정하지 않는다.
                int baseMoveLimit = stage.MoveLimit;
                int extraMoves = DifficultyReliefAgent.Instance != null
                    ? DifficultyReliefAgent.Instance.GetExtraMovesForStage(stage.StageId)
                    : 0;
                int finalMoveLimit = baseMoveLimit + extraMoves;
                moveManager.SetMoves(finalMoveLimit);
                if (extraMoves > 0)
                {
                    Debug.Log($"BoardManager: Move limit initialized: {baseMoveLimit} (+{extraMoves} relief) = {finalMoveLimit}");
                }
                else
                {
                    Debug.Log($"BoardManager: Move limit initialized: {baseMoveLimit}");
                }
            }
            else
            {
                Debug.LogWarning($"BoardManager: MoveManager reference is missing. Move Limit {stage.MoveLimit} not applied.");
            }

            Debug.Log($"BoardManager: Applied StageData '{stage.StageName}' (id={stage.StageId})");
            Debug.Log($"BoardManager: Target Score={stage.TargetScore} (UI 연결은 후속 단계)");
            Debug.Log($"BoardManager: Board Size={stage.BoardWidth} x {stage.BoardHeight}, Reward='{stage.RewardCardId}' x {stage.RewardCardAmount}");
        }

        private void EnsureHelpers()
        {
            if (boardRoot == null)
            {
                boardRoot = transform;
            }

            if (hintAgent == null)
            {
                hintAgent = GetComponent<HintAgent>();
            }
            if (hintAgent == null)
            {
                hintAgent = gameObject.AddComponent<HintAgent>();
            }

            if (blockSwapper == null)
            {
                blockSwapper = GetComponent<BlockSwapper>();
            }
            if (blockSwapper == null)
            {
                blockSwapper = gameObject.AddComponent<BlockSwapper>();
            }

            if (matchFinder == null)
            {
                matchFinder = GetComponent<MatchFinder>();
            }
            if (matchFinder == null)
            {
                matchFinder = gameObject.AddComponent<MatchFinder>();
            }

            if (blockDropper == null)
            {
                blockDropper = GetComponent<BlockDropper>();
            }
            if (blockDropper == null)
            {
                blockDropper = gameObject.AddComponent<BlockDropper>();
            }

            if (moveManager == null)
            {
                moveManager = GetComponent<MoveManager>();
            }
            if (moveManager == null)
            {
                moveManager = FindAnyObjectByType<MoveManager>();
            }
        }

        private void GenerateBoardOnce()
        {
            ClearBoardObjects();

            if (boardRoot == null)
            {
                boardRoot = transform;
            }

            blocks = new Block[rows, columns];

            if (blockPrefab == null)
            {
                Debug.LogWarning("BoardManager: Block prefab is missing.");
                return;
            }

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    CreateBlock(row, column);
                }
            }
        }

        /// <summary>
        /// 현재 보드 상태에서 가로/세로 3개 이상 같은 타입으로 이어진 매치 블록 목록을 반환한다.
        /// 보드 또는 MatchFinder가 없으면 빈 리스트를 반환한다.
        /// </summary>
        public List<Block> FindCurrentMatches()
        {
            if (blocks == null)
            {
                return new List<Block>();
            }
            if (matchFinder == null)
            {
                Debug.LogWarning("BoardManager: MatchFinder reference is missing. Returning no matches.");
                return new List<Block>();
            }
            return matchFinder.FindMatches(blocks, rows, columns);
        }

        /// <summary>현재 보드에 매치가 존재하는지 여부를 반환한다.</summary>
        public bool HasCurrentMatches()
        {
            return FindCurrentMatches().Count > 0;
        }

        /// <summary>
        /// 현재 보드에 플레이 가능한 이동이 한 개라도 있는지 검사한다.
        /// HintAgent에 위임하므로 보드 상태(transform, Block.Row/Column, 점수, 이동수 등)는 바뀌지 않는다.
        /// </summary>
        public bool HasAvailableMove()
        {
            if (blocks == null)
            {
                return false;
            }
            if (hintAgent == null)
            {
                return false;
            }
            return hintAgent.HasAvailableMove(blocks, rows, columns);
        }

        /// <summary>
        /// 가능한 이동이 없으면 maxBoardRegenerationAttempts 한도 안에서 보드를 다시 생성한다.
        /// 무한 루프를 막기 위해 InitializeBoard를 재귀 호출하지 않고 GenerateBoardOnce만 호출한다.
        /// </summary>
        private bool EnsureBoardHasAvailableMove()
        {
            if (HasAvailableMove())
            {
                return true;
            }

            for (int attempt = 1; attempt <= maxBoardRegenerationAttempts; attempt++)
            {
                Debug.Log($"BoardManager: No available moves. Regenerating board (attempt {attempt}/{maxBoardRegenerationAttempts}).");
                GenerateBoardOnce();

                if (HasAvailableMove())
                {
                    Debug.Log($"BoardManager: Solvable board generated after {attempt} regeneration attempt(s).");
                    return true;
                }
            }

            Debug.LogWarning("BoardManager: Failed to generate board with available move.");
            return false;
        }

        /// <summary>
        /// 스테이지가 정한 등장 블록 종류 수를 외부에서 주입할 때 사용한다.
        /// 안전한 범위(3 ~ 안전 타입 개수)로 보정한 뒤 보관한다.
        /// </summary>
        public void SetActiveSpawnTypeCount(int count)
        {
            BlockType[] safeTypes = GetSpawnableTypesSafe();
            if (safeTypes == null || safeTypes.Length == 0)
            {
                safeTypes = GetDefaultSpawnableTypes();
            }

            int upperLimit = Mathf.Min(safeTypes.Length, MaxActiveSpawnTypeCount);
            if (upperLimit < MinActiveSpawnTypeCount)
            {
                upperLimit = MinActiveSpawnTypeCount;
            }

            activeSpawnTypeCount = Mathf.Clamp(count, MinActiveSpawnTypeCount, upperLimit);
        }

        /// <summary>
        /// 현재 활성 등장 블록 수와 실제 사용 가능한 타입 목록을 문자열로 반환한다.
        /// </summary>
        public string GetSpawnDebugSummary()
        {
            BlockType[] activeTypes = GetSpawnableTypesForCurrentStage();
            string typeList;
            if (activeTypes == null || activeTypes.Length == 0)
            {
                typeList = "<empty>";
            }
            else
            {
                string[] names = new string[activeTypes.Length];
                for (int i = 0; i < activeTypes.Length; i++)
                {
                    names[i] = activeTypes[i].ToString();
                }
                typeList = string.Join(", ", names);
            }

            return $"BoardManager Spawn → activeSpawnTypeCount={activeSpawnTypeCount}, types=[{typeList}]";
        }

        /// <summary>
        /// 보드를 다시 생성한다.
        /// </summary>
        public void RegenerateBoard()
        {
            InitializeBoard();
        }

        /// <summary>
        /// 보드의 모든 블록 GameObject를 정리하고 배열을 비운다.
        /// </summary>
        public void ClearBoardObjects()
        {
            if (blocks != null)
            {
                int blockRows = blocks.GetLength(0);
                int blockColumns = blocks.GetLength(1);

                for (int row = 0; row < blockRows; row++)
                {
                    for (int column = 0; column < blockColumns; column++)
                    {
                        Block block = blocks[row, column];

                        if (block != null)
                        {
                            Destroy(block.gameObject);
                        }
                    }
                }
            }

            blocks = null;
        }

        private Block CreateBlock(int row, int column)
        {
            return CreateBlockAt(row, column, GetWorldPosition(row, column));
        }

        /// <summary>
        /// 지정된 월드 위치에 새 Block을 생성한다.
        /// 타입은 좌/상단 이웃과 3연속이 되지 않는 후보로 뽑고, blocks 배열·BlockVisual을 갱신한다.
        /// 생성 즉시 위치 인자로 배치하므로 등장 애니메이션의 시작 위치를 지정할 수 있다.
        /// </summary>
        private Block CreateBlockAt(int row, int column, Vector3 spawnPosition)
        {
            if (!IsInsideBoard(row, column))
            {
                return null;
            }

            if (blockPrefab == null)
            {
                return null;
            }

            if (boardRoot == null)
            {
                boardRoot = transform;
            }

            if (blocks == null)
            {
                return null;
            }

            BlockType type = GetRandomTypeAvoidingInitialMatch(row, column);

            Block block = Instantiate(blockPrefab, spawnPosition, Quaternion.identity, boardRoot);
            block.name = $"Block_{row}_{column}_{type}";
            block.Initialize(type, row, column);

            blocks[row, column] = block;

            BlockVisual visual = block.GetComponent<BlockVisual>();
            if (visual != null)
            {
                visual.Refresh();
            }

            return block;
        }

        /// <summary>
        /// 보드 좌표(row, column)에 해당하는 월드 위치를 반환한다.
        /// boardRoot 기준 중앙 정렬, row가 증가할수록 아래로 내려간다.
        /// </summary>
        public Vector3 GetWorldPosition(int row, int column)
        {
            Vector3 localPosition = GetLocalPosition(row, column);

            if (boardRoot != null)
            {
                return boardRoot.TransformPoint(localPosition);
            }

            return localPosition;
        }

        private Vector3 GetLocalPosition(int row, int column)
        {
            float startX = -(columns - 1) * cellSize * 0.5f;
            float startY = (rows - 1) * cellSize * 0.5f;

            float x = startX + column * cellSize;
            float y = startY - row * cellSize;

            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// 해당 보드 좌표의 Block을 반환한다. 보드 밖이거나 비어 있으면 null.
        /// </summary>
        public Block GetBlock(int row, int column)
        {
            if (blocks == null || !IsInsideBoard(row, column))
            {
                return null;
            }

            return blocks[row, column];
        }

        /// <summary>
        /// 보드 좌표에 Block 참조를 저장한다. 좌표가 유효하지 않으면 false.
        /// </summary>
        public bool SetBlock(int row, int column, Block block)
        {
            if (blocks == null || !IsInsideBoard(row, column))
            {
                return false;
            }

            blocks[row, column] = block;

            if (block != null)
            {
                block.SetGridPosition(row, column);
            }

            return true;
        }

        /// <summary>
        /// 좌표가 보드 범위 안인지 검사한다.
        /// </summary>
        public bool IsInsideBoard(int row, int column)
        {
            return row >= 0 &&
                   row < rows &&
                   column >= 0 &&
                   column < columns;
        }

        private void ValidateBoardSize()
        {
            rows = Mathf.Max(3, rows);
            columns = Mathf.Max(3, columns);
        }

        private BlockType GetRandomSpawnableType()
        {
            BlockType[] limitedTypes = GetSpawnableTypesForCurrentStage();

            if (limitedTypes == null || limitedTypes.Length == 0)
            {
                limitedTypes = GetDefaultSpawnableTypes();
            }

            return limitedTypes[Random.Range(0, limitedTypes.Length)];
        }

        /// <summary>
        /// (row, column)에 배치할 때 가로 또는 세로로 3연속을 만들지 않는 타입을 반환한다.
        /// 1) 최대 maxAttempts 회 랜덤으로 시도
        /// 2) 실패 시 활성 타입 목록 순회로 안전 타입 탐색
        /// 3) 그래도 없으면 일반 랜덤 결과를 fallback으로 반환
        /// </summary>
        private BlockType GetRandomTypeAvoidingInitialMatch(int row, int column)
        {
            const int maxAttempts = 20;

            BlockType[] limitedTypes = GetSpawnableTypesForCurrentStage();
            if (limitedTypes == null || limitedTypes.Length == 0)
            {
                limitedTypes = GetDefaultSpawnableTypes();
            }

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                BlockType candidate = limitedTypes[Random.Range(0, limitedTypes.Length)];
                if (!WouldCreateInitialMatch(row, column, candidate))
                {
                    return candidate;
                }
            }

            for (int i = 0; i < limitedTypes.Length; i++)
            {
                BlockType candidate = limitedTypes[i];
                if (!WouldCreateInitialMatch(row, column, candidate))
                {
                    return candidate;
                }
            }

            return GetRandomSpawnableType();
        }

        /// <summary>
        /// (row, column)에 candidateType을 두면 좌측 2칸 또는 위쪽 2칸과 합쳐 3연속이 되는지 검사한다.
        /// 보드 밖이거나 candidateType이 Empty/Noise이면 안전하지 않은 것으로 간주해 true를 반환한다.
        /// blocks 배열의 아직 생성되지 않은 칸은 null로 안전 처리한다.
        /// </summary>
        private bool WouldCreateInitialMatch(int row, int column, BlockType candidateType)
        {
            if (!IsInsideBoard(row, column))
            {
                return true;
            }

            if (candidateType == BlockType.Empty || candidateType == BlockType.Noise)
            {
                return true;
            }

            if (blocks == null)
            {
                return false;
            }

            if (column >= 2)
            {
                Block left1 = blocks[row, column - 1];
                Block left2 = blocks[row, column - 2];
                if (left1 != null && left2 != null &&
                    left1.Type == candidateType && left2.Type == candidateType)
                {
                    return true;
                }
            }

            if (row >= 2)
            {
                Block up1 = blocks[row - 1, column];
                Block up2 = blocks[row - 2, column];
                if (up1 != null && up2 != null &&
                    up1.Type == candidateType && up2.Type == candidateType)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// activeSpawnTypeCount 만큼만 잘라낸 실제 사용 가능한 타입 배열을 반환한다.
        /// 안전 타입 개수보다 크면 안전 타입 개수까지, 작으면 최소 3까지로 보정한다.
        /// </summary>
        private BlockType[] GetSpawnableTypesForCurrentStage()
        {
            BlockType[] safeTypes = GetSpawnableTypesSafe();
            if (safeTypes == null || safeTypes.Length == 0)
            {
                safeTypes = GetDefaultSpawnableTypes();
            }

            int count = Mathf.Clamp(activeSpawnTypeCount, MinActiveSpawnTypeCount, safeTypes.Length);
            BlockType[] result = new BlockType[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = safeTypes[i];
            }
            return result;
        }

        private BlockType[] GetSpawnableTypesSafe()
        {
            if (spawnableTypes == null || spawnableTypes.Length == 0)
            {
                return GetDefaultSpawnableTypes();
            }

            List<BlockType> safeTypes = new List<BlockType>();

            foreach (BlockType blockType in spawnableTypes)
            {
                if (blockType == BlockType.Empty || blockType == BlockType.Noise)
                {
                    continue;
                }
                safeTypes.Add(blockType);
            }

            if (safeTypes.Count == 0)
            {
                return GetDefaultSpawnableTypes();
            }

            return safeTypes.ToArray();
        }

        private BlockType[] GetDefaultSpawnableTypes()
        {
            return new[]
            {
                BlockType.DreamBubble,
                BlockType.MoonRiceCake,
                BlockType.InkStar,
                BlockType.WaveCloud,
                BlockType.HeartLight
            };
        }

        /// <summary>
        /// 블록 클릭 입력을 받는다.
        /// - 첫 클릭이면 그 블록을 선택한다.
        /// - 같은 블록을 다시 클릭하면 선택을 해제한다.
        /// - 다른 블록을 클릭했고 인접하면 교환 후보로 인정 후 선택을 해제한다(실제 교환은 아직 없음).
        /// - 다른 블록을 클릭했고 인접하지 않으면 기존 선택을 해제하고 새 블록으로 선택을 옮긴다.
        /// 이번 단계에서는 교환/매치/이동수 차감/점수 계산은 수행하지 않는다.
        /// </summary>
        public void OnBlockClicked(Block block)
        {
            if (!CanAcceptInput())
            {
                return;
            }
            if (block == null)
            {
                return;
            }
            if (block.IsEmpty)
            {
                return;
            }

            // 합동 "트윈스타 팡" 스킬 모드가 가장 우선. 한 번 클릭으로 같은 색 일괄 제거.
            if (isColorClearSkillMode)
            {
                HandleColorClearSkillBlockSelected(block);
                return;
            }

            // 다별 "꿈결 움직이기" 스킬 모드: 일반 스왑 대신 스킬 선택/실행으로 라우팅.
            if (isMoveSkillMode)
            {
                HandleMoveSkillBlockClicked(block);
                return;
            }

            if (selectedBlock == null)
            {
                SelectBlock(block);
                return;
            }

            if (selectedBlock == block)
            {
                DeselectCurrentBlock();
                return;
            }

            if (AreAdjacent(selectedBlock, block))
            {
                TrySwapSelectedWith(block);
                return;
            }

            Debug.Log($"BoardManager: Blocks are not adjacent. Changing selection to row={block.Row}, column={block.Column}, type={block.Type}");
            DeselectCurrentBlock();
            SelectBlock(block);
        }

        /// <summary>
        /// 현재 selectedBlock과 targetBlock의 위치를 교환한다(인접 전제).
        /// 실제 교환은 SwapAndValidateRoutine 코루틴에서 수행한다.
        /// </summary>
        private void TrySwapSelectedWith(Block targetBlock)
        {
            if (selectedBlock == null || targetBlock == null)
            {
                return;
            }
            if (!AreAdjacent(selectedBlock, targetBlock))
            {
                return;
            }
            if (blockSwapper == null)
            {
                Debug.LogWarning("BoardManager: BlockSwapper reference is missing. Swap skipped.");
                return;
            }

            Block first = selectedBlock;
            Block second = targetBlock;

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx(SfxType.BlockSwap);

            StartCoroutine(SwapAndValidateRoutine(first, second));
        }

        /// <summary>
        /// 두 블록을 교환한 뒤 매치 여부를 검사하고,
        /// - 매치가 있으면 그 상태를 유지한다.
        /// - 매치가 없으면 다시 한 번 교환을 수행해 원위치로 되돌린다.
        /// 이 단계에서는 이동 횟수 차감/점수 계산/블록 제거/낙하/연쇄는 수행하지 않는다.
        /// </summary>
        private IEnumerator SwapAndValidateRoutine(Block first, Block second)
        {
            LockInput("swap started");

            try
            {
                ApplyBlockSelection(first, false);
                ApplyBlockSelection(second, false);
                selectedBlock = null;

                if (first == null || second == null || blocks == null || blockSwapper == null)
                {
                    yield break;
                }

                yield return StartCoroutine(blockSwapper.SwapRoutine(first, second, blocks));

                List<Block> matches = FindCurrentMatches();
                if (matches.Count > 0)
                {
                    Debug.Log($"BoardManager: Swap accepted. Match count: {matches.Count}");
                    ConsumeMove();
                    yield return StartCoroutine(ResolveBoardCascadeRoutine());
                }
                else
                {
                    Debug.Log("BoardManager: Swap rejected. Reverting.");
                    yield return StartCoroutine(blockSwapper.SwapRoutine(first, second, blocks));
                }
            }
            finally
            {
                if (isStageCleared)
                {
                    Debug.Log("BoardManager: Stage cleared — input remains locked.");
                }
                else if (isStageFailed)
                {
                    Debug.Log("BoardManager: Stage failed — input remains locked.");
                }
                else
                {
                    UnlockInput("board resolved");
                }
            }
        }

        /// <summary>현재 사용자가 보드 입력(블록 클릭/스왑)을 시작할 수 있는지 여부.</summary>
        private bool CanAcceptInput()
        {
            if (isBusy)
            {
                return false;
            }
            if (isStageCleared)
            {
                return false;
            }
            if (isStageFailed)
            {
                return false;
            }
            if (moveManager != null && moveManager.IsOutOfMoves)
            {
                return false;
            }
            // 82번 — HintAgent 펄스 진행 중에는 보드 클릭/스왑을 차단.
            if (isAgentHintRunning)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 사용자가 직접 시도한 유효 스왑(매치 발생 확정) 시점에 정확히 한 번만 호출된다.
        /// 연쇄 처리 도중에는 호출되지 않으므로 추가 차감이 발생하지 않는다.
        /// MoveManager가 없으면 무동작 (게임 중단 없음).
        /// </summary>
        private void ConsumeMove()
        {
            if (moveManager == null)
            {
                return;
            }
            bool used = moveManager.UseMove();
            if (!used)
            {
                return;
            }
            Debug.Log($"BoardManager: Move consumed. Remaining: {moveManager.RemainingMoves}");
            if (moveManager.IsOutOfMoves)
            {
                Debug.Log("BoardManager: No moves remaining. Fail check will run after board resolution.");
            }
        }

        /// <summary>
        /// 입력을 잠근다. 스왑·매치·연쇄·점수 계산이 끝날 때까지 false 상태가 유지되어야 한다.
        /// 중복 호출은 무해하며 상태 변화가 있을 때만 로그를 출력한다.
        /// </summary>
        private void LockInput(string reason = null)
        {
            if (!isBusy)
            {
                Debug.Log(string.IsNullOrEmpty(reason)
                    ? "BoardManager: Input locked"
                    : $"BoardManager: Input locked ({reason})");
            }
            isBusy = true;
        }

        /// <summary>
        /// 입력 잠금을 해제한다. SwapAndValidateRoutine의 finally에서 단일 진입점으로 호출되어,
        /// 매치 성공/실패/예외 등 어떤 경로로 종료되어도 정확히 한 번만 해제된다.
        /// </summary>
        private void UnlockInput(string reason = null)
        {
            if (isBusy)
            {
                Debug.Log(string.IsNullOrEmpty(reason)
                    ? "BoardManager: Input unlocked"
                    : $"BoardManager: Input unlocked ({reason})");
            }
            isBusy = false;
        }

        /// <summary>
        /// 1차 매치 처리 후 낙하·새 블록 생성으로 추가 매치가 발생하면 자동으로 반복 제거한다.
        /// 각 반복은: 매치 탐색 → 제거 → 낙하 → 새 블록 생성 → 안정화 대기.
        /// 매치가 더 이상 없으면 종료하고, MaxCascadeCount 도달 시 강제 종료한다.
        /// 호출자는 입력 잠금(isBusy)을 외부에서 유지해야 한다.
        /// </summary>
        private IEnumerator ResolveBoardCascadeRoutine()
        {
            Debug.Log("BoardManager: Cascade started");

            int passIndex = 0;
            int cascadeCount = 0;

            while (true)
            {
                List<Block> matches = FindCurrentMatches();
                if (matches == null || matches.Count == 0)
                {
                    break;
                }

                if (passIndex == 0)
                {
                    Debug.Log($"BoardManager: Initial match resolved (matched={matches.Count})");
                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx(SfxType.Match);
                }
                else
                {
                    cascadeCount++;
                    Debug.Log($"BoardManager: Cascade chain: {cascadeCount} (matched={matches.Count})");
                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx(SfxType.Cascade);
                }
                passIndex++;

                AddScore(matches.Count, cascadeCount);

                yield return StartCoroutine(RemoveMatchedBlocksRoutine(matches));
                if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx(SfxType.Drop);
                yield return StartCoroutine(DropExistingBlocksRoutine());
                yield return StartCoroutine(FillEmptyCellsWithNewBlocksRoutine());

                if (cascadeStabilizeDelay > 0f)
                {
                    yield return new WaitForSeconds(cascadeStabilizeDelay);
                }

                if (cascadeCount >= MaxCascadeCount)
                {
                    Debug.LogWarning("BoardManager: Cascade stopped: max cascade count reached.");
                    break;
                }
            }

            Debug.Log($"BoardManager: Cascade finished (total chains={cascadeCount})");

            CheckStageClear();
            if (isStageCleared && clearPopupCoroutine == null)
            {
                clearPopupCoroutine = StartCoroutine(ShowClearPopupAfterBoardSettled());
            }
            else
            {
                CheckStageFail();
                if (isStageFailed && failPopupCoroutine == null)
                {
                    failPopupCoroutine = StartCoroutine(ShowFailPopupAfterBoardSettled());
                }
            }
        }

        /// <summary>
        /// 매치 블록 수와 연쇄 깊이(cascadeCount)를 받아 점수를 가산하고 UI를 갱신한다.
        /// 공식: earned = matchedBlockCount * blockBaseScore * (1 + cascadeCount * 0.5).
        /// cascadeCount=0은 1차 매치(보너스 없음), 1부터 연쇄 보너스가 적용된다.
        /// </summary>
        private void AddScore(int matchedBlockCount, int cascadeCount)
        {
            if (matchedBlockCount <= 0)
            {
                return;
            }

            int safeCascade = Mathf.Max(0, cascadeCount);
            float multiplier = 1f + safeCascade * 0.5f;
            int earnedScore = Mathf.RoundToInt(matchedBlockCount * blockBaseScore * multiplier);

            score += earnedScore;

            if (safeCascade > 0)
            {
                Debug.Log($"BoardManager: Cascade Bonus x{multiplier:F1}");
            }
            Debug.Log($"BoardManager: Score +{earnedScore} / matched: {matchedBlockCount} / cascade: {safeCascade} / total: {score}");

            UpdateScoreUI();
            UpdateGoalUI();
            CheckGoalAchieved();
            CheckStageClear();
        }

        /// <summary>점수 텍스트 UI를 갱신한다. scoreText가 비어 있으면 아무것도 하지 않는다.</summary>
        private void UpdateScoreUI()
        {
            if (scoreText == null)
            {
                return;
            }
            scoreText.text = $"Score: {score:N0}";
        }

        /// <summary>점수를 0으로 초기화하고 UI를 갱신한다. 보드 초기화 시 호출된다.</summary>
        private void ResetScore()
        {
            score = 0;
            UpdateScoreUI();
        }

        /// <summary>
        /// StageData에서 목표 정보를 읽어 캐싱한다. 데이터가 없으면 defaultTargetScore 기반의 점수 목표를 사용한다.
        /// 보드 초기화 시 호출되며, 누적 수집 카운트는 0으로 리셋된다.
        /// </summary>
        private void InitGoalFromStageData()
        {
            if (StageManager.Instance != null && StageManager.Instance.HasCurrentStage())
            {
                StageData stage = StageManager.Instance.CurrentStageData;
                currentGoalType = stage.GoalType;
                // 84번 — DifficultyReliefAgent의 목표 완화 적용. enableGoalRelief=false이면 원본 그대로.
                targetScore = DifficultyReliefAgent.Instance != null
                    ? DifficultyReliefAgent.Instance.GetAdjustedTargetScore(stage.StageId, stage.TargetScore)
                    : stage.TargetScore;
                targetBlockType = stage.TargetBlockType;
                targetBlockCount = DifficultyReliefAgent.Instance != null
                    ? DifficultyReliefAgent.Instance.GetAdjustedTargetBlockCount(stage.StageId, stage.TargetBlockCount)
                    : stage.TargetBlockCount;
                isCurrentBossStage = stage.IsBossStage;
                currentBossStageType = stage.BossStageType;
                currentStageName = stage.StageName;
            }
            else
            {
                currentGoalType = StageGoalType.Score;
                targetScore = defaultTargetScore;
                targetBlockType = BlockType.DreamBubble;
                targetBlockCount = 0;
                isCurrentBossStage = false;
                currentBossStageType = BossStageType.None;
                currentStageName = string.Empty;
            }

            currentCollectedBlockCount = 0;
            goalAchievedLogged = false;

            Debug.Log($"BoardManager: Goal initialized: {currentGoalType}");
            Debug.Log($"BoardManager: Target Score: {targetScore}");
            Debug.Log($"BoardManager: Target Block: {targetBlockType}, Count: {targetBlockCount}");

            UpdateGoalUI();
        }

        /// <summary>현재 goalType에 맞춰 목표 UI 텍스트를 갱신한다. goalText가 null이면 무동작.</summary>
        private void UpdateGoalUI()
        {
            if (goalText == null)
            {
                return;
            }

            string body;
            switch (currentGoalType)
            {
                case StageGoalType.Score:
                    body = $"Score {score:N0} / {targetScore:N0}";
                    break;

                case StageGoalType.CollectBlock:
                    int shown = Mathf.Min(currentCollectedBlockCount, targetBlockCount);
                    body = $"{targetBlockType} {shown} / {targetBlockCount}";
                    break;

                case StageGoalType.ClearObstacle:
                    // TODO: 후속 단계에서 Noise 블록 잔여 카운트 기반으로 표시한다.
                    body = "Clear all obstacles";
                    break;

                default:
                    body = string.Empty;
                    break;
            }

            if (isCurrentBossStage)
            {
                string label = string.IsNullOrEmpty(currentStageName) ? currentBossStageType.ToString() : currentStageName;
                goalText.text = $"BOSS {label}: {body}";
            }
            else
            {
                goalText.text = body;
            }
        }

        /// <summary>현재 목표가 달성되었는지 여부. ClearObstacle은 후속 단계에서 구현하므로 false.</summary>
        private bool IsGoalAchieved()
        {
            switch (currentGoalType)
            {
                case StageGoalType.Score:
                    return targetScore > 0 && score >= targetScore;

                case StageGoalType.CollectBlock:
                    return targetBlockCount > 0 && currentCollectedBlockCount >= targetBlockCount;

                case StageGoalType.ClearObstacle:
                    // TODO: Noise 블록 잔여 수 == 0 시 true로 변경.
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 제거 예정 블록 목록에서 targetBlockType 카운트를 누적한다.
        /// 연쇄로 제거되는 블록도 포함되며, targetBlockCount를 초과하지 않도록 클램프된다.
        /// 매치 제거 코루틴이 블록을 null화하기 전에 호출되어야 한다.
        /// </summary>
        private void CountCollectedBlocks(List<Block> matches)
        {
            if (currentGoalType != StageGoalType.CollectBlock)
            {
                return;
            }
            if (matches == null || matches.Count == 0)
            {
                return;
            }
            if (targetBlockCount <= 0)
            {
                return;
            }

            for (int i = 0; i < matches.Count; i++)
            {
                Block b = matches[i];
                if (b == null)
                {
                    continue;
                }
                if (b.Type != targetBlockType)
                {
                    continue;
                }
                if (currentCollectedBlockCount < targetBlockCount)
                {
                    currentCollectedBlockCount++;
                }
            }
        }

        /// <summary>목표 달성 시 단 한 번 로그를 출력한다. 실제 클리어 흐름 연결은 후속 단계.</summary>
        private void CheckGoalAchieved()
        {
            if (goalAchievedLogged)
            {
                return;
            }
            if (IsGoalAchieved())
            {
                goalAchievedLogged = true;
                Debug.Log("BoardManager: Stage goal achieved.");
            }
        }

        /// <summary>
        /// 보드 초기화 시 ClearPopup 상태를 리셋한다.
        /// 진행 중인 popup 코루틴이 있으면 중지하고, GameObject는 비활성화한다.
        /// </summary>
        private void InitClearPopup()
        {
            if (clearPopupCoroutine != null)
            {
                StopCoroutine(clearPopupCoroutine);
                clearPopupCoroutine = null;
            }
            isStageCleared = false;
            if (clearPopup != null)
            {
                clearPopup.SetActive(false);
            }
        }

        /// <summary>
        /// IsGoalAchieved가 true이면 isStageCleared를 세팅한다.
        /// 여러 위치(AddScore, CountCollectedBlocks, cascade 종료)에서 호출되어도 isStageCleared 가드로 1회만 처리된다.
        /// 실제 ClearPopup 표시는 cascade가 종료된 후 단일 지점에서 큐잉된다.
        /// </summary>
        private void CheckStageClear()
        {
            if (isStageCleared)
            {
                return;
            }
            if (!IsGoalAchieved())
            {
                return;
            }
            isStageCleared = true;
            Debug.Log("BoardManager: Stage clear condition achieved.");

            if (StageManager.Instance != null && StageManager.Instance.CurrentStageData != null)
            {
                int clearedStageId = StageManager.Instance.CurrentStageData.StageId;
                StageManager.Instance.UnlockNextStage(clearedStageId);

                if (RegionRestoreManager.Instance != null)
                {
                    RegionRestoreManager.Instance.MarkStageCleared(clearedStageId);
                }

                if (AlbumProgressManager.Instance != null)
                {
                    AlbumProgressManager.Instance.UnlockPageByStageId(clearedStageId);
                }

                // 83번 — 클리어 시 해당 스테이지 실패 횟수 초기화 (정책에 따라)
                if (FailureAssistAgent.Instance != null)
                {
                    FailureAssistAgent.Instance.RecordStageClear(clearedStageId);
                }

                // 84번 — 난이도 완화 에이전트에도 클리어 알림. 실패 카운트가 0이 되어 자동으로 완화 해제됨.
                if (DifficultyReliefAgent.Instance != null)
                {
                    DifficultyReliefAgent.Instance.RecordStageClear(clearedStageId);
                }

                // 85번 — 클리어 보상 카드 ID를 학습 코치에 기록 (오늘 얻은 지식카드 목록).
                if (LearningCoachAgent.Instance != null)
                {
                    string rewardCardId = StageManager.Instance.CurrentStageData.RewardCardId;
                    if (!string.IsNullOrWhiteSpace(rewardCardId))
                    {
                        LearningCoachAgent.Instance.RecordCardEarnedToday(rewardCardId);
                    }
                }
            }
        }

        /// <summary>
        /// 짧은 대기 후 ClearPopup을 활성화한다. cascade가 자연 종료된 직후 단일 지점에서만 큐잉되며,
        /// clearPopupCoroutine 가드로 중복 시작을 방지한다.
        /// </summary>
        private IEnumerator ShowClearPopupAfterBoardSettled()
        {
            if (clearPopupDelay > 0f)
            {
                yield return new WaitForSeconds(clearPopupDelay);
            }
            else
            {
                yield return null;
            }

            ShowClearPopup();
            clearPopupCoroutine = null;
        }

        /// <summary>
        /// ClearPopup을 활성화하고 입력을 잠근다. clearPopup이 미연결이어도 LockInput은 호출되어
        /// 클리어 상태에서 추가 스왑이 들어오지 않게 한다.
        /// </summary>
        private void ShowClearPopup()
        {
            if (isStageFailed)
            {
                Debug.LogWarning("BoardManager: ShowClearPopup blocked because stage already failed.");
                return;
            }

            LockInput("stage cleared");

            if (clearPopup == null)
            {
                Debug.LogWarning("BoardManager: ClearPopup is not assigned.");
                Debug.Log("BoardManager: Stage cleared. (no popup UI)");
                return;
            }

            clearPopup.SetActive(true);
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(SfxType.Clear);
                SoundManager.Instance.PlaySfx(SfxType.Reward);
            }
            Debug.Log("BoardManager: ClearPopup shown.");
            // TODO: 보상 카드 지급 / 다음 스테이지 이동은 후속 단계에서 연결한다.
        }

        /// <summary>보드 초기화 시 FailPopup 상태를 리셋한다.</summary>
        private void InitFailPopup()
        {
            if (failPopupCoroutine != null)
            {
                StopCoroutine(failPopupCoroutine);
                failPopupCoroutine = null;
            }
            isStageFailed = false;
            if (failPopup != null)
            {
                failPopup.SetActive(false);
            }
        }

        /// <summary>
        /// 클리어 상태가 아니고, 이동 횟수가 0이고, 목표를 달성하지 못했으면 isStageFailed를 세팅한다.
        /// ResolveBoardCascadeRoutine 종료 직후의 단일 지점에서 호출되며, 클리어 우선 평가가 먼저 끝난 뒤에 실행된다.
        /// </summary>
        private void CheckStageFail()
        {
            if (isStageCleared)
            {
                return;
            }
            if (isStageFailed)
            {
                return;
            }
            if (moveManager == null)
            {
                return;
            }
            if (!moveManager.IsOutOfMoves)
            {
                return;
            }
            if (IsGoalAchieved())
            {
                return;
            }

            isStageFailed = true;
            Debug.Log("BoardManager: Stage failed: no moves remaining and goal not achieved.");

            // 83번 — 실패 확정 1회 트랜지션에서만 실패 횟수 기록. isStageFailed 가드로 중복 호출 방지됨.
            if (FailureAssistAgent.Instance != null
                && StageManager.Instance != null
                && StageManager.Instance.CurrentStageData != null)
            {
                int failedStageId = StageManager.Instance.CurrentStageData.StageId;
                FailureAssistAgent.Instance.RecordStageFail(failedStageId);
            }
        }

        /// <summary>
        /// 짧은 대기 후 FailPopup을 활성화한다. cascade가 자연 종료된 직후 단일 지점에서만 큐잉되며,
        /// failPopupCoroutine 가드로 중복 시작을 방지한다.
        /// </summary>
        private IEnumerator ShowFailPopupAfterBoardSettled()
        {
            if (failPopupDelay > 0f)
            {
                yield return new WaitForSeconds(failPopupDelay);
            }
            else
            {
                yield return null;
            }

            ShowFailPopup();
            failPopupCoroutine = null;
        }

        /// <summary>
        /// FailPopup을 활성화하고 입력을 잠근다. 클리어가 이미 발동했다면 표시하지 않는다.
        /// failPopup이 미연결이어도 LockInput은 호출되어 추가 스왑이 들어오지 않게 한다.
        /// </summary>
        private void ShowFailPopup()
        {
            if (isStageCleared)
            {
                Debug.LogWarning("BoardManager: ShowFailPopup blocked because stage already cleared.");
                return;
            }

            LockInput("stage failed");

            if (failPopup == null)
            {
                Debug.LogWarning("BoardManager: FailPopup is not assigned.");
                Debug.Log("BoardManager: Stage failed. (no popup UI)");
                return;
            }

            failPopup.SetActive(true);
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx(SfxType.Fail);
            Debug.Log("BoardManager: FailPopup shown.");
            // TODO: 재시작 / 스테이지 선택 화면 이동은 후속 단계에서 연결한다.
        }

        /// <summary>FailPopup의 Retry 버튼이 호출하는 진입점. 실제 재시작 로직은 후속 단계에서 추가.</summary>
        public void OnClickFailPopupRetry()
        {
            Debug.Log("BoardManager: Retry button clicked.");
            // TODO: Restart current stage (RegenerateBoard 또는 StageManager.LoadStageById 재호출).
        }

        /// <summary>FailPopup의 Home 버튼이 호출하는 진입점. 실제 화면 전환은 후속 단계에서 추가.</summary>
        public void OnClickFailPopupHome()
        {
            Debug.Log("BoardManager: Home button clicked.");
            // TODO: Go to stage select screen.
        }

        /// <summary>나별 힌트 스킬을 지금 사용할 수 있는 상태인지 여부.</summary>
        public bool CanUseHintSkill()
        {
            if (isBusy) return false;
            if (isStageCleared) return false;
            if (isStageFailed) return false;
            if (isShowingHint) return false;
            if (isShowingBubbleHint) return false;
            if (isMoveSkillMode) return false;
            if (isColorClearSkillMode) return false;
            if (isColorClearSkillRunning) return false;
            if (isNumberSortSkillRunning) return false;
            if (blocks == null) return false;
            if (hintAgent == null) return false;
            return true;
        }

        /// <summary>
        /// 현재 보드에서 매치를 만드는 인접 스왑 후보 한 쌍을 찾는다. HintAgent가 임시 스왑 후
        /// MatchFinder로 검증하고 원상복구까지 책임지므로 보드 상태는 변경되지 않는다.
        /// </summary>
        public bool TryFindHintMove(out Block first, out Block second)
        {
            first = null;
            second = null;

            if (blocks == null || hintAgent == null)
            {
                return false;
            }

            HintAgent.HintResult result = hintAgent.FindHint(blocks, rows, columns);
            if (!result.hasHint)
            {
                return false;
            }
            first = result.firstBlock;
            second = result.secondBlock;
            return true;
        }

        /// <summary>
        /// 나별 "별자리 보기" 스킬 진입점. 가능하면 hint를 찾아 펄스 애니메이션으로 표시한다.
        /// 보드 상태/Row/Column/점수/이동수/목표 진행도는 변경하지 않는다.
        /// </summary>
        public bool ShowHint()
        {
            if (!CanUseHintSkill())
            {
                Debug.LogWarning("BoardManager: Cannot use hint skill (board busy / cleared / failed / showing).");
                return false;
            }

            if (!TryFindHintMove(out Block a, out Block b))
            {
                Debug.Log("BoardManager: No hint move found.");
                // TODO: Trigger board shuffle when no hint exists.
                return false;
            }

            Debug.Log($"BoardManager: Hint found: ({a.Row},{a.Column}) -> ({b.Row},{b.Column})");

            if (hintCoroutine != null)
            {
                StopCoroutine(hintCoroutine);
                hintCoroutine = null;
            }
            hintCoroutine = StartCoroutine(ShowHintRoutine(a, b));
            return true;
        }

        private IEnumerator ShowHintRoutine(Block a, Block b)
        {
            isShowingHint = true;

            Vector3 originalScaleA = a != null ? a.transform.localScale : Vector3.one;
            Vector3 originalScaleB = b != null ? b.transform.localScale : Vector3.one;

            for (int i = 0; i < hintPulseCount; i++)
            {
                if (a != null) a.transform.localScale = originalScaleA * hintScaleMultiplier;
                if (b != null) b.transform.localScale = originalScaleB * hintScaleMultiplier;
                yield return new WaitForSeconds(hintPulseDuration);

                if (a != null) a.transform.localScale = originalScaleA;
                if (b != null) b.transform.localScale = originalScaleB;
                yield return new WaitForSeconds(hintPulseDuration);
            }

            isShowingHint = false;
            hintCoroutine = null;
            // TODO: Add skill cooldown.
            // TODO: Add limited skill count per stage.
            // TODO: Add skill gauge UI.
        }

        // ───────── 82번 — HintAgent v1 진입점 ─────────

        /// <summary>HintAgent v1이 진입 가능한 상태인지. 모든 입력 잠금/스킬 모드를 점검한다.</summary>
        public bool CanUseHintAgent()
        {
            if (isBusy) return false;
            if (isStageCleared) return false;
            if (isStageFailed) return false;
            if (isShowingHint) return false;
            if (isShowingBubbleHint) return false;
            if (isMoveSkillMode) return false;
            if (isColorClearSkillMode) return false;
            if (isColorClearSkillRunning) return false;
            if (isNumberSortSkillRunning) return false;
            if (isAgentHintRunning) return false;
            if (blocks == null) return false;
            if (matchFinder == null) return false;
            return true;
        }

        /// <summary>
        /// 두 좌표(column=x, row=y)를 한 번 스왑했을 때 매치가 발생하는지 검사한다.
        /// 임시 스왑 후 즉시 원상복구하므로 보드 배열/Transform은 변경되지 않는다.
        /// </summary>
        public bool WouldCreateMatch(Vector2Int from, Vector2Int to, out int matchCount)
        {
            matchCount = 0;
            if (blocks == null || matchFinder == null) return false;
            int fr = from.y, fc = from.x;
            int tr = to.y, tc = to.x;
            if (fr < 0 || fr >= rows || fc < 0 || fc >= columns) return false;
            if (tr < 0 || tr >= rows || tc < 0 || tc >= columns) return false;
            if (Mathf.Abs(fr - tr) + Mathf.Abs(fc - tc) != 1) return false;

            Block a = blocks[fr, fc];
            Block b = blocks[tr, tc];
            if (a == null || b == null) return false;
            if (a.IsEmpty || a.IsNoise || b.IsEmpty || b.IsNoise) return false;

            // 임시 스왑 → MatchFinder 호출 → 즉시 원상복구. Block.Transform은 건드리지 않는다.
            blocks[fr, fc] = b;
            blocks[tr, tc] = a;
            List<Block> matches = matchFinder.FindMatches(blocks, rows, columns);
            blocks[fr, fc] = a;
            blocks[tr, tc] = b;

            matchCount = matches != null ? matches.Count : 0;
            return matchCount > 0;
        }

        /// <summary>
        /// HintAgent가 호출하는 진입점. CanUseHintAgent 통과 후 AgentHintRoutine을 시작한다.
        /// 점수/이동수/목표 진행도는 절대 건드리지 않는다.
        /// </summary>
        public bool ShowAgentHint(HintAgent.HintMove move)
        {
            if (!CanUseHintAgent())
            {
                Debug.LogWarning("BoardManager: Cannot show agent hint right now.");
                return false;
            }
            int fr = move.from.y, fc = move.from.x;
            int tr = move.to.y, tc = move.to.x;
            if (fr < 0 || fr >= rows || fc < 0 || fc >= columns) return false;
            if (tr < 0 || tr >= rows || tc < 0 || tc >= columns) return false;

            Block a = blocks[fr, fc];
            Block b = blocks[tr, tc];
            if (a == null || b == null)
            {
                Debug.LogWarning($"BoardManager: ShowAgentHint blocks missing at ({fr},{fc}) or ({tr},{tc}).");
                return false;
            }

            Debug.Log($"BoardManager: Agent hint ({fr},{fc}) ↔ ({tr},{tc}), expected matches={move.expectedMatchCount}, priority={move.priorityScore}.");

            if (agentHintCoroutine != null)
            {
                StopCoroutine(agentHintCoroutine);
                agentHintCoroutine = null;
            }
            agentHintCoroutine = StartCoroutine(AgentHintRoutine(a, b));
            return true;
        }

        private IEnumerator AgentHintRoutine(Block a, Block b)
        {
            isAgentHintRunning = true;

            Vector3 originalScaleA = a != null ? a.transform.localScale : Vector3.one;
            Vector3 originalScaleB = b != null ? b.transform.localScale : Vector3.one;

            // 펄스 파라미터는 기존 나별 힌트와 공유 (hintPulseCount/hintPulseDuration/hintScaleMultiplier).
            for (int i = 0; i < hintPulseCount; i++)
            {
                if (a != null) a.transform.localScale = originalScaleA * hintScaleMultiplier;
                if (b != null) b.transform.localScale = originalScaleB * hintScaleMultiplier;
                yield return new WaitForSeconds(hintPulseDuration);

                if (a != null) a.transform.localScale = originalScaleA;
                if (b != null) b.transform.localScale = originalScaleB;
                yield return new WaitForSeconds(hintPulseDuration);
            }

            isAgentHintRunning = false;
            agentHintCoroutine = null;
        }

        public bool IsAgentHintRunning => isAgentHintRunning;

        // ───────── 다별 "꿈결 움직이기" 스킬 ─────────

        /// <summary>다별 이동 스킬을 지금 사용할 수 있는 상태인지 여부.</summary>
        public bool CanUseMoveSkill()
        {
            if (isBusy) return false;
            if (isStageCleared) return false;
            if (isStageFailed) return false;
            if (isShowingHint) return false;
            if (isShowingBubbleHint) return false;
            if (isMoveSkillMode) return false;
            if (isColorClearSkillMode) return false;
            if (isColorClearSkillRunning) return false;
            if (isNumberSortSkillRunning) return false;
            if (blocks == null) return false;
            if (blockSwapper == null) return false;
            if (moveManager != null && moveManager.IsOutOfMoves) return false;
            return true;
        }

        /// <summary>
        /// 다별 스킬 모드에 진입한다. 일반 스왑 선택 상태(selectedBlock)는 비운다.
        /// 입력 잠금은 걸지 않고, OnBlockClicked가 isMoveSkillMode 분기를 따라 처리한다.
        /// </summary>
        public bool EnterMoveSkillMode()
        {
            if (!CanUseMoveSkill())
            {
                Debug.LogWarning("BoardManager: Cannot enter move skill mode.");
                return false;
            }

            if (selectedBlock != null)
            {
                DeselectCurrentBlock();
            }

            isMoveSkillMode = true;
            moveSkillSelectedBlock = null;
            Debug.Log("BoardManager: Move skill mode entered.");
            return true;
        }

        /// <summary>다별 스킬 모드를 취소한다. 시각 선택 표시도 해제한다.</summary>
        public void CancelMoveSkillMode()
        {
            if (!isMoveSkillMode && moveSkillSelectedBlock == null)
            {
                return;
            }
            if (moveSkillSelectedBlock != null)
            {
                ApplyBlockSelection(moveSkillSelectedBlock, false);
            }
            isMoveSkillMode = false;
            moveSkillSelectedBlock = null;
            Debug.Log("BoardManager: Move skill mode cancelled.");
        }

        private void HandleMoveSkillBlockClicked(Block clicked)
        {
            if (clicked == null || clicked.IsEmpty)
            {
                return;
            }

            // 첫 클릭: 이동할 블록 선택.
            if (moveSkillSelectedBlock == null)
            {
                moveSkillSelectedBlock = clicked;
                ApplyBlockSelection(clicked, true);
                Debug.Log($"BoardManager: Move skill block selected: ({clicked.Row},{clicked.Column})");
                return;
            }

            // 같은 블록 재클릭: 선택 해제 (모드는 유지).
            if (clicked == moveSkillSelectedBlock)
            {
                ApplyBlockSelection(clicked, false);
                moveSkillSelectedBlock = null;
                Debug.Log("BoardManager: Move skill selection cleared.");
                return;
            }

            // 두 번째 클릭이 인접: 스킬 이동 실행.
            if (AreAdjacent(moveSkillSelectedBlock, clicked))
            {
                Block first = moveSkillSelectedBlock;
                Debug.Log($"BoardManager: Move skill executed: ({first.Row},{first.Column}) -> ({clicked.Row},{clicked.Column})");
                StartCoroutine(ExecuteMoveSkillSwapRoutine(first, clicked));
                return;
            }

            // 인접 아님: 선택을 새 블록으로 갱신.
            ApplyBlockSelection(moveSkillSelectedBlock, false);
            moveSkillSelectedBlock = clicked;
            ApplyBlockSelection(clicked, true);
            Debug.Log($"BoardManager: Move skill selection moved to: ({clicked.Row},{clicked.Column})");
        }

        /// <summary>
        /// 다별 스킬 이동 실행. 일반 스왑과 달리 매치가 없어도 보드 변경을 유지한다.
        /// 이동 횟수는 실행 시점에 1회만 차감되며, 매치가 있으면 ResolveBoardCascadeRoutine으로 위임한다.
        /// 매치 없는 경우에도 마지막 클리어/실패 평가는 직접 수행한다.
        /// </summary>
        private IEnumerator ExecuteMoveSkillSwapRoutine(Block first, Block second)
        {
            LockInput("dabyeol move skill started");

            try
            {
                // 스킬 모드 상태 종료 + 시각 선택 해제.
                if (moveSkillSelectedBlock != null)
                {
                    ApplyBlockSelection(moveSkillSelectedBlock, false);
                }
                isMoveSkillMode = false;
                moveSkillSelectedBlock = null;

                if (first == null || second == null || blocks == null || blockSwapper == null)
                {
                    yield break;
                }

                // TODO: Make this skill free (no move cost) by removing the ConsumeMove call below.
                ConsumeMove();

                // 다별 스킬 사용 횟수 차감 (B안: 실제 스왑 실행 시점).
                if (SkillManager.Instance != null)
                {
                    SkillManager.Instance.NotifySkillConsumed(SkillType.DabyeolMove);
                }

                yield return StartCoroutine(blockSwapper.SwapRoutine(first, second, blocks));

                List<Block> matches = FindCurrentMatches();
                if (matches.Count > 0)
                {
                    Debug.Log($"BoardManager: Move skill swap created matches. Match count: {matches.Count}");
                    yield return StartCoroutine(ResolveBoardCascadeRoutine());
                }
                else
                {
                    Debug.Log("BoardManager: Move skill swap created no matches. Board state kept as-is.");

                    // 스킬 이동은 매치가 없어도 원위치 복귀하지 않는다.
                    // ResolveBoardCascadeRoutine을 거치지 않으므로 클리어/실패 평가를 여기서 수행한다.
                    CheckStageClear();
                    if (isStageCleared && clearPopupCoroutine == null)
                    {
                        clearPopupCoroutine = StartCoroutine(ShowClearPopupAfterBoardSettled());
                    }
                    else
                    {
                        CheckStageFail();
                        if (isStageFailed && failPopupCoroutine == null)
                        {
                            failPopupCoroutine = StartCoroutine(ShowFailPopupAfterBoardSettled());
                        }
                    }
                }
            }
            finally
            {
                if (isStageCleared)
                {
                    Debug.Log("BoardManager: Stage cleared — input remains locked.");
                }
                else if (isStageFailed)
                {
                    Debug.Log("BoardManager: Stage failed — input remains locked.");
                }
                else
                {
                    UnlockInput("move skill resolved");
                }
            }
        }

        // ───────── 합동 "트윈스타 팡" 스킬 ─────────

        /// <summary>합동 스킬을 지금 사용할 수 있는 상태인지 여부.</summary>
        public bool CanUseColorClearSkill()
        {
            if (isBusy) return false;
            if (isStageCleared) return false;
            if (isStageFailed) return false;
            if (isShowingHint) return false;
            if (isShowingBubbleHint) return false;
            if (isMoveSkillMode) return false;
            if (isColorClearSkillMode) return false;
            if (isColorClearSkillRunning) return false;
            if (isNumberSortSkillRunning) return false;
            if (blocks == null) return false;
            if (moveManager != null && moveManager.IsOutOfMoves) return false;
            return true;
        }

        /// <summary>합동 스킬 모드에 진입한다. 입력 잠금은 걸지 않으며, 다음 클릭에서 즉시 실행된다.</summary>
        public bool EnterColorClearSkillMode()
        {
            if (!CanUseColorClearSkill())
            {
                Debug.LogWarning("BoardManager: Cannot enter twin star pop skill mode.");
                return false;
            }

            if (selectedBlock != null)
            {
                DeselectCurrentBlock();
            }

            isColorClearSkillMode = true;
            Debug.Log("BoardManager: Color clear skill mode entered.");
            return true;
        }

        /// <summary>합동 스킬 모드를 취소한다.</summary>
        public void CancelColorClearSkillMode()
        {
            if (!isColorClearSkillMode)
            {
                return;
            }
            isColorClearSkillMode = false;
            Debug.Log("BoardManager: Color clear skill mode cancelled.");
        }

        private void HandleColorClearSkillBlockSelected(Block clicked)
        {
            if (clicked == null || clicked.IsEmpty)
            {
                Debug.LogWarning("BoardManager: Twin skill cannot target empty/null block.");
                return;
            }

            BlockType selectedType = clicked.Type;
            Debug.Log($"BoardManager: Twin skill selected block type: {selectedType}");

            StartCoroutine(ExecuteColorClearSkillRoutine(selectedType, clicked));
        }

        /// <summary>
        /// 지정된 타입의 블록들을 보드 순회 순서로 수집한다.
        /// guaranteed가 결과에 반드시 포함되도록 선두에 배치하며, maxCount가 양수면 N개로 자른다.
        /// </summary>
        private List<Block> GetBlocksByType(BlockType type, Block guaranteed, int maxCount)
        {
            List<Block> candidates = new List<Block>();
            if (blocks == null)
            {
                return candidates;
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Block b = blocks[r, c];
                    if (b == null) continue;
                    if (b.IsEmpty) continue;
                    if (b.Type != type) continue;
                    candidates.Add(b);
                }
            }

            if (maxCount <= 0 || candidates.Count <= maxCount)
            {
                return candidates;
            }

            // 선택 블록을 반드시 포함하도록 선두에 배치 후 나머지는 보드 순회 순서대로 채운다.
            List<Block> result = new List<Block>(maxCount);
            if (guaranteed != null)
            {
                result.Add(guaranteed);
            }
            for (int i = 0; i < candidates.Count && result.Count < maxCount; i++)
            {
                Block b = candidates[i];
                if (b == guaranteed) continue;
                result.Add(b);
            }
            return result;
        }

        /// <summary>
        /// 합동 스킬 실행. 같은 타입 블록을 일괄 제거한 뒤 낙하 → 새 블록 생성 → 연쇄까지 자연스럽게 이어간다.
        /// 이동 횟수는 차감하지 않는다.
        /// </summary>
        private IEnumerator ExecuteColorClearSkillRoutine(BlockType selectedType, Block originBlock)
        {
            LockInput("twin star pop started");

            try
            {
                isColorClearSkillMode = false;
                isColorClearSkillRunning = true;

                if (blocks == null)
                {
                    yield break;
                }

                List<Block> targets = GetBlocksByType(selectedType, originBlock, twinSkillMaxRemoveCount);
                if (targets == null || targets.Count == 0)
                {
                    Debug.LogWarning("BoardManager: Twin skill found no removable blocks.");
                    yield break;
                }

                Debug.Log($"BoardManager: Twin skill removed blocks: {targets.Count}");
                Debug.Log("BoardManager: Twin skill does not consume move.");
                // TODO: Optionally consume one move when using twin skill for harder stages.

                // 합동 스킬 사용 횟수 차감 (B안: 실제 제거 실행 시점).
                if (SkillManager.Instance != null)
                {
                    SkillManager.Instance.NotifySkillConsumed(SkillType.TwinStarPop);
                }

                // 점수 가산. 스킬 1차 제거는 연쇄 보너스 없이 기본 1.0배.
                AddScore(targets.Count, 0);

                // 기존 매치 제거 코루틴 재사용: CollectBlock 카운트 + 시각 효과 + 배열 null화를 일괄 처리한다.
                yield return StartCoroutine(RemoveMatchedBlocksRoutine(targets));

                // 빈칸 낙하 + 새 블록 생성도 기존 코루틴 재사용.
                yield return StartCoroutine(DropExistingBlocksRoutine());
                yield return StartCoroutine(FillEmptyCellsWithNewBlocksRoutine());

                // 후속 자동 매치/연쇄 + 최종 클리어·실패 평가 + 팝업 큐잉까지 ResolveBoardCascadeRoutine에 위임.
                yield return StartCoroutine(ResolveBoardCascadeRoutine());
            }
            finally
            {
                isColorClearSkillRunning = false;

                if (isStageCleared)
                {
                    Debug.Log("BoardManager: Stage cleared — input remains locked.");
                }
                else if (isStageFailed)
                {
                    Debug.Log("BoardManager: Stage failed — input remains locked.");
                }
                else
                {
                    UnlockInput("twin star pop resolved");
                }
                // TODO: Add twin skill gauge.
                // TODO: Limit twin skill to once per stage.
                // TODO: Add cooldown UI.
            }
        }

        // ───────── 카피몽 "느긋한 숨결" 스킬 ─────────

        /// <summary>카피몽 이동수 증가 스킬을 지금 사용할 수 있는지 여부.</summary>
        public bool CanUseAddMoveSkill()
        {
            if (isBusy) return false;
            if (isStageCleared) return false;
            if (isStageFailed) return false;
            if (isShowingHint) return false;
            if (isShowingBubbleHint) return false;
            if (isMoveSkillMode) return false;
            if (isColorClearSkillMode) return false;
            if (isColorClearSkillRunning) return false;
            if (isNumberSortSkillRunning) return false;
            if (moveManager == null) return false;
            if (moveManager.IsOutOfMoves) return false; // 0 → FailPopup 임박 상태에서는 사용 불가
            return true;
        }

        /// <summary>
        /// 카피몽 "느긋한 숨결" 스킬. 남은 이동 횟수를 amount만큼(기본 1) 증가시킨다.
        /// MoveManager.AddMoves가 OnMovesChanged를 발행해 MoveTextUI가 자동 갱신된다.
        /// 점수/목표/클리어/실패에는 영향을 주지 않으며, 스테이지당 1회만 사용 가능하다.
        /// </summary>
        public bool AddMoveBySkill(int amount)
        {
            if (!CanUseAddMoveSkill())
            {
                Debug.LogWarning("BoardManager: Cannot use Capymong add-move skill right now.");
                return false;
            }
            if (amount <= 0)
            {
                Debug.LogWarning($"BoardManager: Invalid add move amount: {amount}");
                return false;
            }

            int before = moveManager.RemainingMoves;
            int desired = before + amount;
            int clamped = Mathf.Min(desired, Mathf.Max(1, maxRemainingMoves));
            int delta = clamped - before;

            if (delta <= 0)
            {
                Debug.LogWarning($"BoardManager: Move count already at max ({maxRemainingMoves}). Skill not consumed.");
                return false;
            }

            moveManager.AddMoves(delta); // 내부에서 OnMovesChanged 이벤트 발행 → MoveTextUI가 즉시 갱신

            Debug.Log($"BoardManager: Move added by Capymong skill: {before} -> {moveManager.RemainingMoves} (delta=+{delta}).");
            // 스테이지당 사용 횟수는 SkillManager(UseCapymongBreathSkill)가 통합 관리한다.
            return true;
        }

        // ───────── 포포링 "방울 힌트" 스킬 ─────────

        /// <summary>포포링 방울 힌트 스킬을 지금 사용할 수 있는지 여부.</summary>
        public bool CanUseBubbleHintSkill()
        {
            if (isBusy) return false;
            if (isStageCleared) return false;
            if (isStageFailed) return false;
            if (isShowingHint) return false;
            if (isShowingBubbleHint) return false;
            if (isMoveSkillMode) return false;
            if (isColorClearSkillMode) return false;
            if (isColorClearSkillRunning) return false;
            if (isNumberSortSkillRunning) return false;
            if (blocks == null) return false;
            if (hintAgent == null) return false;
            return true;
        }

        /// <summary>
        /// 포포링 방울 힌트 진입점. 52번 TryFindHintMove를 재사용해 후보를 찾고,
        /// 두 블록 위에 부드러운 방울 모션(옵션 prefab 포함)을 표시한다.
        /// 매치 후보가 없으면 사용 횟수를 차감하지 않는다.
        /// </summary>
        public bool ShowBubbleHint()
        {
            if (!CanUseBubbleHintSkill())
            {
                Debug.LogWarning("BoardManager: Cannot use bubble hint skill.");
                return false;
            }

            if (!TryFindHintMove(out Block a, out Block b))
            {
                Debug.Log("BoardManager: No bubble hint move found.");
                return false; // 1회 제한 소비하지 않음
            }

            Debug.Log($"BoardManager: Bubble hint found: ({a.Row},{a.Column}) -> ({b.Row},{b.Column})");

            // 스테이지당 사용 횟수는 SkillManager(UsePoporingBubbleHintSkill)가 통합 관리한다.

            if (bubbleHintCoroutine != null)
            {
                StopCoroutine(bubbleHintCoroutine);
                bubbleHintCoroutine = null;
            }
            bubbleHintCoroutine = StartCoroutine(ShowBubbleHintRoutine(a, b));
            return true;
        }

        private IEnumerator ShowBubbleHintRoutine(Block a, Block b)
        {
            isShowingBubbleHint = true;

            Vector3 startScaleA = a != null ? a.transform.localScale : Vector3.one;
            Vector3 startScaleB = b != null ? b.transform.localScale : Vector3.one;

            // bubbleHintPrefab이 있으면 두 블록 위치에 마커를 띄운다. 없으면 fallback으로 scale 모션만 사용.
            GameObject bubbleMarkerA = null;
            GameObject bubbleMarkerB = null;
            if (bubbleHintPrefab != null)
            {
                if (a != null) bubbleMarkerA = Instantiate(bubbleHintPrefab, a.transform.position, Quaternion.identity);
                if (b != null) bubbleMarkerB = Instantiate(bubbleHintPrefab, b.transform.position, Quaternion.identity);
            }

            float duration = Mathf.Max(0.1f, bubbleHintDuration);
            float amplitude = Mathf.Max(1f, bubbleHintScaleAmplitude) - 1f;
            float elapsed = 0f;

            // 두 번 부드럽게 통통 튀는 sin 모션. 나별의 빠른 펄스와 시각적으로 차별화된다.
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float bob = 1f + amplitude * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f));

                if (a != null) a.transform.localScale = startScaleA * bob;
                if (b != null) b.transform.localScale = startScaleB * bob;
                yield return null;
            }

            if (a != null) a.transform.localScale = startScaleA;
            if (b != null) b.transform.localScale = startScaleB;

            if (bubbleMarkerA != null) Destroy(bubbleMarkerA);
            if (bubbleMarkerB != null) Destroy(bubbleMarkerB);

            isShowingBubbleHint = false;
            bubbleHintCoroutine = null;
        }

        // ───────── 모찌룬 "숫자 블록 정렬" 스킬 ─────────

        /// <summary>모찌룬 숫자 정렬 스킬을 지금 사용할 수 있는지 여부.</summary>
        public bool CanUseNumberSortSkill()
        {
            if (isBusy) return false;
            if (isStageCleared) return false;
            if (isStageFailed) return false;
            if (isShowingHint) return false;
            if (isShowingBubbleHint) return false;
            if (isMoveSkillMode) return false;
            if (isColorClearSkillMode) return false;
            if (isColorClearSkillRunning) return false;
            if (isNumberSortSkillRunning) return false;
            if (blocks == null) return false;
            if (matchFinder == null) return false;
            return true;
        }

        /// <summary>
        /// 모찌룬 "숫자 블록 정렬" 스킬 진입점. 보드에서 가장 많은 타입을 찾아
        /// 가로 3칸(또는 세로 3칸)에 모아 즉시 매치를 만든다.
        /// 정렬 성공 후의 매치/제거/낙하/연쇄/점수/목표는 ResolveBoardCascadeRoutine에 위임.
        /// </summary>
        public bool SortNumberBlocksBySkill()
        {
            if (!CanUseNumberSortSkill())
            {
                Debug.LogWarning("BoardManager: Cannot use Mochirun number sort skill right now.");
                return false;
            }
            StartCoroutine(ExecuteNumberSortRoutine());
            return true;
        }

        private bool TryFindMostCommonBlockType(out BlockType targetType)
        {
            targetType = BlockType.DreamBubble;
            if (blocks == null) return false;

            Dictionary<BlockType, int> counts = new Dictionary<BlockType, int>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Block b = blocks[r, c];
                    if (b == null) continue;
                    if (b.IsEmpty || b.IsNoise) continue;
                    BlockType t = b.Type;
                    if (!counts.ContainsKey(t)) counts[t] = 0;
                    counts[t]++;
                }
            }

            int maxCount = 0;
            foreach (KeyValuePair<BlockType, int> kv in counts)
            {
                if (kv.Value >= 3 && kv.Value > maxCount)
                {
                    maxCount = kv.Value;
                    targetType = kv.Key;
                }
            }
            return maxCount >= 3;
        }

        private bool TryFindSortLine(out List<Vector2Int> targetCells)
        {
            targetCells = new List<Vector2Int>(3);
            if (blocks == null) return false;

            // 가로 3칸 우선. 모두 non-null인 첫 라인 채택.
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c + 2 < columns; c++)
                {
                    if (blocks[r, c] != null && blocks[r, c + 1] != null && blocks[r, c + 2] != null)
                    {
                        targetCells.Add(new Vector2Int(r, c));
                        targetCells.Add(new Vector2Int(r, c + 1));
                        targetCells.Add(new Vector2Int(r, c + 2));
                        return true;
                    }
                }
            }

            // 세로 3칸 fallback.
            for (int c = 0; c < columns; c++)
            {
                for (int r = 0; r + 2 < rows; r++)
                {
                    if (blocks[r, c] != null && blocks[r + 1, c] != null && blocks[r + 2, c] != null)
                    {
                        targetCells.Add(new Vector2Int(r, c));
                        targetCells.Add(new Vector2Int(r + 1, c));
                        targetCells.Add(new Vector2Int(r + 2, c));
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// targetCells 위치 외부에 있는 type 블록들을 보드 순회 순서로 maxCount개까지 수집한다.
        /// 정렬 대상 자리에 이미 있는 동일 타입 블록은 제외하므로 swap 매칭이 자연스럽게 일대일이 된다.
        /// </summary>
        private List<Block> GetSourceBlocksForSort(BlockType type, HashSet<Vector2Int> excludeCells, int maxCount)
        {
            List<Block> sources = new List<Block>();
            if (blocks == null || maxCount <= 0) return sources;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    if (sources.Count >= maxCount) return sources;
                    Block b = blocks[r, c];
                    if (b == null) continue;
                    if (b.Type != type) continue;
                    if (excludeCells.Contains(new Vector2Int(r, c))) continue;
                    sources.Add(b);
                }
            }
            return sources;
        }

        private IEnumerator ExecuteNumberSortRoutine()
        {
            LockInput("mochirun number sort started");

            try
            {
                isNumberSortSkillRunning = true;

                if (!TryFindMostCommonBlockType(out BlockType targetType))
                {
                    Debug.LogWarning("BoardManager: Number sort failed: no valid target type (need 3+ of same type).");
                    yield break;
                }
                Debug.Log($"BoardManager: Number sort target type: {targetType}");

                if (!TryFindSortLine(out List<Vector2Int> targetCells))
                {
                    Debug.LogWarning("BoardManager: Number sort failed: no valid 3-cell line.");
                    yield break;
                }
                Debug.Log($"BoardManager: Number sort target cells: {targetCells[0]}, {targetCells[1]}, {targetCells[2]}");

                // 정렬 대상 자리 중 이미 targetType인 블록은 유지. 나머지 자리만 외부 블록과 교환한다.
                HashSet<Vector2Int> targetSet = new HashSet<Vector2Int>(targetCells);
                List<Vector2Int> needsSwap = new List<Vector2Int>();
                foreach (Vector2Int cell in targetCells)
                {
                    Block b = blocks[cell.x, cell.y];
                    if (b == null || b.Type != targetType)
                    {
                        needsSwap.Add(cell);
                    }
                }

                List<Block> sources = GetSourceBlocksForSort(targetType, targetSet, needsSwap.Count);
                if (sources.Count < needsSwap.Count)
                {
                    Debug.LogWarning($"BoardManager: Number sort failed: not enough source blocks ({sources.Count} < {needsSwap.Count}).");
                    yield break;
                }

                // 실제 재배치: 배열·Block.Row/Column·Transform.position을 동시에 갱신.
                for (int i = 0; i < needsSwap.Count; i++)
                {
                    Vector2Int targetCell = needsSwap[i];
                    Block sourceBlock = sources[i];
                    Block targetBlock = blocks[targetCell.x, targetCell.y];

                    int sourceRow = sourceBlock.Row;
                    int sourceCol = sourceBlock.Column;

                    blocks[targetCell.x, targetCell.y] = sourceBlock;
                    blocks[sourceRow, sourceCol] = targetBlock;

                    sourceBlock.SetGridPosition(targetCell.x, targetCell.y);
                    if (targetBlock != null)
                    {
                        targetBlock.SetGridPosition(sourceRow, sourceCol);
                    }

                    sourceBlock.transform.position = GetWorldPosition(targetCell.x, targetCell.y);
                    if (targetBlock != null)
                    {
                        targetBlock.transform.position = GetWorldPosition(sourceRow, sourceCol);
                    }
                }

                // 모찌룬 스킬 사용 횟수 차감 (실제 정렬 성공 시점).
                if (SkillManager.Instance != null)
                {
                    SkillManager.Instance.NotifySkillConsumed(SkillType.MochirunNumberSort);
                }

                // 이동 횟수 정책: 기본 false. true면 한 번 차감.
                // TODO: Optionally consume one move when balancing harder stages.
                if (numberSortConsumesMove)
                {
                    ConsumeMove();
                }

                Debug.Log("BoardManager: Number sort skill completed.");
                // TODO: Add sorting animation effect.
                // TODO: Add skill gauge.
                // TODO: Use actual number block data instead of blockType.

                List<Block> matches = FindCurrentMatches();
                if (matches.Count > 0)
                {
                    Debug.Log($"BoardManager: Number sort created matches. Match count: {matches.Count}");
                    yield return StartCoroutine(ResolveBoardCascadeRoutine());
                }
                else
                {
                    Debug.LogWarning("BoardManager: Number sort completed but no match was created.");
                    // 매치 없으면 cascade 미진입. 클리어/실패 평가를 직접 수행.
                    CheckStageClear();
                    if (isStageCleared && clearPopupCoroutine == null)
                    {
                        clearPopupCoroutine = StartCoroutine(ShowClearPopupAfterBoardSettled());
                    }
                    else
                    {
                        CheckStageFail();
                        if (isStageFailed && failPopupCoroutine == null)
                        {
                            failPopupCoroutine = StartCoroutine(ShowFailPopupAfterBoardSettled());
                        }
                    }
                }
            }
            finally
            {
                isNumberSortSkillRunning = false;

                if (isStageCleared)
                {
                    Debug.Log("BoardManager: Stage cleared — input remains locked.");
                }
                else if (isStageFailed)
                {
                    Debug.Log("BoardManager: Stage failed — input remains locked.");
                }
                else
                {
                    UnlockInput("number sort resolved");
                }
            }
        }

        /// <summary>
        /// 매치 제거로 생긴 빈칸 위쪽 블록을 같은 column에서 아래로 떨어뜨린다.
        /// BlockDropper에 위임하며, 새 블록 생성 또는 연쇄 매치는 다음 단계에서 처리한다.
        /// </summary>
        private IEnumerator DropExistingBlocksRoutine()
        {
            if (blockDropper == null)
            {
                yield break;
            }
            if (blocks == null)
            {
                yield break;
            }

            yield return StartCoroutine(blockDropper.DropBlocksRoutine(blocks, rows, columns, cellSize, boardRoot));
        }

        private struct SpawnMoveData
        {
            public Block block;
            public Vector3 startPosition;
            public Vector3 targetPosition;
        }

        /// <summary>
        /// blocks 배열의 모든 null 빈칸에 새 Block을 즉시 생성한다.
        /// 등장 애니메이션 없이 생성 위치 = 목표 위치로 배치한다. 생성된 블록 수를 반환한다.
        /// </summary>
        private int FillEmptyCellsWithNewBlocksInstant()
        {
            if (blocks == null)
            {
                return 0;
            }
            if (blockPrefab == null)
            {
                Debug.LogWarning("BoardManager: Block prefab is missing. Skipping fill.");
                return 0;
            }

            int created = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (blocks[row, column] != null)
                    {
                        continue;
                    }

                    Vector3 target = GetWorldPosition(row, column);
                    Block block = CreateBlockAt(row, column, target);
                    if (block != null)
                    {
                        created++;
                    }
                }
            }
            return created;
        }

        /// <summary>
        /// blocks 배열의 모든 null 빈칸에 새 Block을 생성하고, 목표 위치보다 위쪽에서
        /// 내려오는 등장 애니메이션을 newBlockDropDuration 동안 동시에 재생한다.
        /// </summary>
        private IEnumerator FillEmptyCellsWithNewBlocksRoutine()
        {
            if (blocks == null)
            {
                yield break;
            }
            if (blockPrefab == null)
            {
                Debug.LogWarning("BoardManager: Block prefab is missing. Skipping fill.");
                yield break;
            }

            List<SpawnMoveData> spawnedBlocks = new List<SpawnMoveData>();

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (blocks[row, column] != null)
                    {
                        continue;
                    }

                    Vector3 target = GetWorldPosition(row, column);
                    Vector3 start = target + Vector3.up * newBlockSpawnOffsetY;

                    Block block = CreateBlockAt(row, column, start);
                    if (block == null)
                    {
                        continue;
                    }

                    spawnedBlocks.Add(new SpawnMoveData
                    {
                        block = block,
                        startPosition = start,
                        targetPosition = target
                    });
                }
            }

            if (spawnedBlocks.Count == 0)
            {
                yield break;
            }

            float duration = Mathf.Max(0.0001f, newBlockDropDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                for (int i = 0; i < spawnedBlocks.Count; i++)
                {
                    SpawnMoveData data = spawnedBlocks[i];
                    if (data.block == null)
                    {
                        continue;
                    }
                    data.block.transform.position = Vector3.Lerp(data.startPosition, data.targetPosition, t);
                }

                yield return null;
            }

            for (int i = 0; i < spawnedBlocks.Count; i++)
            {
                SpawnMoveData data = spawnedBlocks[i];
                if (data.block == null)
                {
                    continue;
                }
                data.block.transform.position = data.targetPosition;
            }
        }

        /// <summary>
        /// 매치된 블록들을 보드 배열에서 먼저 null 처리한 뒤,
        /// 각 블록의 BlockVisual 제거 효과를 동시에 실행하고 공통 대기 시간을 둔다.
        /// BlockVisual이 없는 블록은 즉시 Destroy 한다.
        /// 이번 단계에서는 점수/이동수 등은 다루지 않는다.
        /// </summary>
        private IEnumerator RemoveMatchedBlocksRoutine(List<Block> matches)
        {
            if (matches == null || matches.Count == 0)
            {
                yield break;
            }

            CountCollectedBlocks(matches);
            UpdateGoalUI();
            CheckGoalAchieved();
            CheckStageClear();

            for (int i = 0; i < matches.Count; i++)
            {
                Block block = matches[i];
                if (block == null)
                {
                    continue;
                }

                int r = block.Row;
                int c = block.Column;
                if (!IsInsideBoard(r, c))
                {
                    continue;
                }
                if (blocks == null)
                {
                    continue;
                }
                if (blocks[r, c] == block)
                {
                    blocks[r, c] = null;
                }
            }

            // Task 102 — kick off a parallel "pop" scale animation on every block
            // that does NOT have its own BlockVisual.PlayRemoveEffectThenDestroy.
            // Then wait the pop duration once (not per block) so the existing
            // drop/cascade flow continues unchanged.
            bool playedPop = false;
            if (SimpleAnimationManager.Instance != null && SimpleAnimationManager.Instance.AnimationsEnabled)
            {
                for (int i = 0; i < matches.Count; i++)
                {
                    Block block = matches[i];
                    if (block == null) continue;
                    if (block.GetComponent<BlockVisual>() != null) continue;
                    StartCoroutine(SimpleAnimationManager.Instance.PlayBlockPop(block.transform));
                    playedPop = true;
                }
            }

            bool hasVisualEffect = false;

            for (int i = 0; i < matches.Count; i++)
            {
                Block block = matches[i];
                if (block == null)
                {
                    continue;
                }

                BlockVisual visual = block.GetComponent<BlockVisual>();
                if (visual != null)
                {
                    visual.PlayRemoveEffectThenDestroy();
                    hasVisualEffect = true;
                }
                else if (playedPop)
                {
                    // Destroy after the pop coroutine finishes so the user actually
                    // sees the scale-down. Delay matches the manager's pop duration.
                    float popDuration = SimpleAnimationManager.Instance != null
                        ? SimpleAnimationManager.Instance.BlockPopDuration : 0f;
                    Destroy(block.gameObject, popDuration);
                }
                else
                {
                    Destroy(block.gameObject);
                }
            }

            if (hasVisualEffect)
            {
                yield return new WaitForSeconds(matchRemoveWaitTime);
            }
            else if (playedPop)
            {
                float popDuration = SimpleAnimationManager.Instance != null
                    ? SimpleAnimationManager.Instance.BlockPopDuration : 0f;
                if (popDuration > 0f) yield return new WaitForSeconds(popDuration);
            }
        }

        /// <summary>
        /// 두 블록이 상하좌우로 인접한지 검사한다.
        /// 같은 객체이거나 null, 대각선, 두 칸 이상 떨어진 경우는 false.
        /// </summary>
        public bool AreAdjacent(Block first, Block second)
        {
            if (first == null || second == null)
            {
                return false;
            }
            if (first == second)
            {
                return false;
            }

            int rowDiff = Mathf.Abs(first.Row - second.Row);
            int columnDiff = Mathf.Abs(first.Column - second.Column);

            return rowDiff + columnDiff == 1;
        }

        private void SelectBlock(Block block)
        {
            selectedBlock = block;
            ApplyBlockSelection(block, true);
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx(SfxType.BlockSelect);
            Debug.Log($"BoardManager: Selected {block.GetDebugLabel()}");
        }

        private void DeselectCurrentBlock()
        {
            if (selectedBlock == null)
            {
                return;
            }
            Block previous = selectedBlock;
            ApplyBlockSelection(previous, false);
            selectedBlock = null;
            Debug.Log($"BoardManager: Deselected {previous.GetDebugLabel()}");
        }

        private void ApplyBlockSelection(Block block, bool selected)
        {
            if (block == null)
            {
                return;
            }
            block.SetSelected(selected);
            BlockVisual visual = block.GetComponent<BlockVisual>();
            if (visual != null)
            {
                visual.SetSelected(selected);
            }
        }

        // 임시 호환 메서드: 후속 체크리스트(힌트 로직) 전까지 컴파일 통과용.
        public void LogHint()
        {
            Debug.Log("BoardManager: LogHint not implemented yet.");
        }
    }
}
