using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Sound;

namespace NabyeolDabyeolDreamPuzzle.Stage
{
    /// <summary>
    /// 현재 선택된 StageData(ScriptableObject)를 보관·로드·검증하는 매니저.
    /// 다른 시스템은 StageManager.Instance.CurrentStageData를 통해 정보만 읽어 사용한다.
    /// BoardManager 등 게임 로직을 직접 제어하지 않는다.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [Header("Stage")]
        [SerializeField] private StageData currentStageData;
        [SerializeField] private StageData defaultStageData;
        [SerializeField] private List<StageData> stageList = new List<StageData>();

        [Header("Boot")]
        [SerializeField] private bool autoLoadOnAwake = true;
        [SerializeField] private bool persistAcrossScenes = false;

        [Header("Unlock")]
        [SerializeField] private int highestUnlockedStageId = 1;
        [SerializeField] private bool ignoreUnlockForDebug = false;

        // TODO: If worlds become independent, split unlock keys by world id (e.g. "Unlock_BubbleForest").
        private const string HighestUnlockedStageKey = "HighestUnlockedStageId";

        /// <summary>현재 로드된 StageData. 미로드면 null.</summary>
        public StageData CurrentStageData => currentStageData;

        /// <summary>기존 GoalManager 등의 stageManager.TargetScore 호출을 위한 호환 프로퍼티.</summary>
        public int TargetScore => currentStageData != null ? currentStageData.TargetScore : 0;

        /// <summary>현재 스테이지 ID. 미로드면 0.</summary>
        public int StageNumber => currentStageData != null ? currentStageData.StageId : 0;

        /// <summary>현재까지 해금된 가장 높은 stageId.</summary>
        public int HighestUnlockedStageId => highestUnlockedStageId;

        /// <summary>기존 코드 호환을 위한 별칭. CurrentStageData와 동일.</summary>
        public StageData CurrentStage => currentStageData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("StageManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            ValidateStageList();
            LoadUnlockProgress();

            if (autoLoadOnAwake)
            {
                AutoLoadInitialStage();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>StageData가 정해진 순서대로 로드 시도된다: currentStageData → defaultStageData.</summary>
        private void AutoLoadInitialStage()
        {
            if (currentStageData != null)
            {
                LoadStage(currentStageData);
                return;
            }
            if (defaultStageData != null)
            {
                LoadStage(defaultStageData);
                return;
            }
            Debug.LogWarning("StageManager: No StageData assigned. CurrentStageData remains null.");
        }

        /// <summary>현재 스테이지가 존재하고 IsValid를 통과하는지 여부.</summary>
        public bool HasCurrentStage()
        {
            return currentStageData != null && currentStageData.IsValid();
        }

        /// <summary>
        /// 지정된 StageData를 현재 스테이지로 로드한다. null 또는 IsValid 실패면 false.
        /// 성공 시 주요 값을 Console에 로그로 남긴다.
        /// </summary>
        public bool LoadStage(StageData stageData)
        {
            if (stageData == null)
            {
                Debug.LogWarning("StageManager: LoadStage called with null.");
                return false;
            }
            if (!stageData.IsValid())
            {
                Debug.LogWarning($"StageManager: Stage '{stageData.name}' failed IsValid(). Load aborted.");
                return false;
            }

            currentStageData = stageData;

            Debug.Log($"StageManager: Stage loaded: {stageData.StageName} (id={stageData.StageId})");
            Debug.Log($"StageManager: Move Limit: {stageData.MoveLimit}, Target Score: {stageData.TargetScore}");
            Debug.Log($"StageManager: Board Size: {stageData.BoardWidth} x {stageData.BoardHeight}");
            Debug.Log($"StageManager: Reward Card: '{stageData.RewardCardId}' x {stageData.RewardCardAmount}");
            Debug.Log($"StageManager: Boss Stage: {stageData.IsBossStage}, Type: {stageData.BossStageType}");

            if (SoundManager.Instance != null)
            {
                BgmType bgm = stageData.IsBossStage ? BgmType.Boss : BgmType.Puzzle;
                SoundManager.Instance.PlayBgmWithFade(bgm);
            }

            return true;
        }

        /// <summary>stageList에서 stageId로 StageData를 찾아 로드한다. 잠긴 스테이지/미발견/무효 시 false.</summary>
        public bool LoadStageById(int stageId)
        {
            if (!ignoreUnlockForDebug && !IsStageUnlocked(stageId))
            {
                Debug.LogWarning($"StageManager: Stage is locked. stageId: {stageId} (highestUnlocked: {highestUnlockedStageId})");
                return false;
            }

            // 1차: 기존 stageList 검색 (우선)
            StageData target = null;
            if (stageList != null && stageList.Count > 0)
            {
                for (int i = 0; i < stageList.Count; i++)
                {
                    StageData candidate = stageList[i];
                    if (candidate != null && candidate.StageId == stageId)
                    {
                        target = candidate;
                        break;
                    }
                }
            }

            // 2차 fallback: StagePackManager 데이터베이스에서 검색
            if (target == null && StagePackManager.Instance != null)
            {
                target = StagePackManager.Instance.GetStageById(stageId);
                if (target != null)
                {
                    Debug.Log($"StageManager: Stage loaded from StagePackDatabase (fallback). stageId: {stageId}");
                }
            }

            if (target == null)
            {
                Debug.LogWarning($"StageManager: Stage not found. stageId: {stageId}");
                return false;
            }
            return LoadStage(target);
        }

        /// <summary>PlayerPrefs에서 해금 진행도를 읽어와 highestUnlockedStageId를 세팅한다.</summary>
        private void LoadUnlockProgress()
        {
            int saved = PlayerPrefs.GetInt(HighestUnlockedStageKey, 1);
            highestUnlockedStageId = Mathf.Max(1, saved);
            Debug.Log($"StageManager: Highest unlocked stage: {highestUnlockedStageId}");
        }

        /// <summary>현재 highestUnlockedStageId를 PlayerPrefs에 저장한다.</summary>
        private void SaveUnlockProgress()
        {
            PlayerPrefs.SetInt(HighestUnlockedStageKey, highestUnlockedStageId);
            PlayerPrefs.Save();
        }

        /// <summary>해당 stageId가 현재 플레이 가능한 상태인지 여부.</summary>
        public bool IsStageUnlocked(int stageId)
        {
            if (stageId < 1)
            {
                return false;
            }
            return stageId <= highestUnlockedStageId;
        }

        /// <summary>stageList에 해당 stageId의 StageData가 등록되어 있는지 여부.</summary>
        public bool HasStage(int stageId)
        {
            if (stageList == null || stageList.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < stageList.Count; i++)
            {
                StageData s = stageList[i];
                if (s != null && s.StageId == stageId)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// clearedStageId의 다음 스테이지(clearedStageId+1)를 해금한다.
        /// 이미 해금되어 있거나 stageList에 등록이 없으면 로그만 출력하고 무동작.
        /// 클리어 확정 시점에서 호출되며, 중복 호출되어도 안전하다.
        /// </summary>
        public void UnlockNextStage(int clearedStageId)
        {
            int nextStageId = clearedStageId + 1;

            if (nextStageId <= highestUnlockedStageId)
            {
                Debug.Log($"StageManager: Stage already unlocked. highestUnlockedStageId: {highestUnlockedStageId}");
                return;
            }

            if (!HasStage(nextStageId))
            {
                Debug.Log($"StageManager: No next stage to unlock after id={clearedStageId} (stageList has no id {nextStageId}).");
                return;
            }

            highestUnlockedStageId = nextStageId;
            SaveUnlockProgress();
            Debug.Log($"StageManager: Stage unlocked: {nextStageId}");
        }

        /// <summary>디버그용. 해금 진행도를 1번 스테이지만 열린 초기 상태로 되돌린다.</summary>
        [ContextMenu("Reset Stage Unlock Progress")]
        public void ResetStageUnlockProgress()
        {
            highestUnlockedStageId = 1;
            SaveUnlockProgress();
            Debug.Log("StageManager: Stage unlock progress reset.");
        }

        /// <summary>디버그용. stageList의 최대 stageId까지 모두 해금한다.</summary>
        [ContextMenu("Unlock All Stages For Debug")]
        public void UnlockAllStagesForDebug()
        {
            int maxId = 1;
            if (stageList != null)
            {
                for (int i = 0; i < stageList.Count; i++)
                {
                    StageData s = stageList[i];
                    if (s != null && s.StageId > maxId)
                    {
                        maxId = s.StageId;
                    }
                }
            }
            highestUnlockedStageId = maxId;
            SaveUnlockProgress();
            Debug.Log($"StageManager: Unlocked all stages up to {maxId}.");
        }

        /// <summary>stageList의 null/무효/중복 stageId 항목을 검사하고 경고를 남긴다.</summary>
        private void ValidateStageList()
        {
            if (stageList == null || stageList.Count == 0)
            {
                return;
            }

            HashSet<int> seenIds = new HashSet<int>();
            for (int i = 0; i < stageList.Count; i++)
            {
                StageData s = stageList[i];
                if (s == null)
                {
                    Debug.LogWarning($"StageManager: stageList[{i}] is null.");
                    continue;
                }
                if (!s.IsValid())
                {
                    Debug.LogWarning($"StageManager: stageList[{i}] '{s.name}' failed IsValid().");
                }
                if (!seenIds.Add(s.StageId))
                {
                    Debug.LogWarning($"StageManager: Duplicate stageId {s.StageId} at stageList[{i}].");
                }
            }
        }
    }
}
