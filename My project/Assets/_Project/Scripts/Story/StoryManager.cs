using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Stage;

namespace NabyeolDabyeolDreamPuzzle.Story
{
    /// <summary>
    /// 스테이지별 StoryNode를 보관·검색·로드하는 매니저.
    /// linkedStageId 방식으로 StageManager.CurrentStageData.StageId와 연결되며,
    /// 후속 단계의 StoryPopup이 GetDialoguesForCurrentStage를 호출해 화면에 표시한다.
    /// 기존 StoryTextUI 호환을 위해 GetCurrentStage*Line() shim 3개도 제공한다.
    /// </summary>
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance { get; private set; }

        [Header("Story")]
        [SerializeField] private List<StoryNode> storyNodes = new List<StoryNode>();
        [SerializeField] private StoryNode currentStoryNode;

        [Header("Boot")]
        [SerializeField] private bool autoLoadOnStageChange = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("StoryManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ValidateStoryNodes();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (autoLoadOnStageChange && StageManager.Instance != null && StageManager.Instance.CurrentStageData != null)
            {
                LoadStoryNode(StageManager.Instance.CurrentStageData.StageId);
            }
        }

        public StoryNode CurrentStoryNode => currentStoryNode;

        /// <summary>해당 stageId에 연결된 StoryNode가 등록되어 있는지 여부.</summary>
        public bool HasStoryForStage(int stageId)
        {
            return FindStoryNodeForStage(stageId) != null;
        }

        /// <summary>현재 StageManager.CurrentStageData에 연결된 StoryNode가 있는지 여부.</summary>
        public bool HasCurrentStageStory()
        {
            if (StageManager.Instance == null) return false;
            if (StageManager.Instance.CurrentStageData == null) return false;
            return HasStoryForStage(StageManager.Instance.CurrentStageData.StageId);
        }

        /// <summary>
        /// stageId로 StoryNode를 찾아 currentStoryNode로 세팅한다.
        /// 발견 못 하면 currentStoryNode = null로 두고 false 반환.
        /// </summary>
        public bool LoadStoryNode(int stageId)
        {
            StoryNode node = FindStoryNodeForStage(stageId);
            if (node == null)
            {
                Debug.LogWarning($"StoryManager: No StoryNode found for stageId: {stageId}");
                currentStoryNode = null;
                return false;
            }
            currentStoryNode = node;
            Debug.Log($"StoryManager: Story loaded: '{node.StoryTitle}' (storyId={node.StoryId}, linkedStageId={node.LinkedStageId})");
            return true;
        }

        /// <summary>지정된 stageId의 StoryNode에서 type 대사 리스트를 반환한다.</summary>
        public List<StoryDialogueLine> GetDialoguesForStage(int stageId, StoryDialogueType type)
        {
            StoryNode node = FindStoryNodeForStage(stageId);
            if (node == null)
            {
                Debug.LogWarning($"StoryManager: No StoryNode for stageId: {stageId}");
                return new List<StoryDialogueLine>();
            }
            return node.GetDialogues(type);
        }

        /// <summary>현재 스테이지의 type 대사 리스트를 반환한다.</summary>
        public List<StoryDialogueLine> GetDialoguesForCurrentStage(StoryDialogueType type)
        {
            if (StageManager.Instance == null || StageManager.Instance.CurrentStageData == null)
            {
                return new List<StoryDialogueLine>();
            }
            return GetDialoguesForStage(StageManager.Instance.CurrentStageData.StageId, type);
        }

        /// <summary>
        /// 표시용 대사 리스트. 원본 StoryNode 리스트를 가져온 뒤 각 lineIndex에 대해
        /// StoryDialogueOverrideManager가 승인된 override를 보유하면 dialogue만 교체한 사본을 반환한다.
        /// 원본 StoryNode/StoryDialogueLine 자산은 절대 수정되지 않는다.
        /// </summary>
        public List<StoryDialogueLine> GetDisplayDialoguesForStage(int stageId, StoryDialogueType type)
        {
            List<StoryDialogueLine> source = GetDialoguesForStage(stageId, type);
            return ApplyOverridesToDialogues(stageId, type, source);
        }

        /// <summary>
        /// override를 적용하지 않은 원본 StoryNode 대사 리스트.
        /// 미리보기 UI가 원본과 적용문을 비교할 때 사용. GetDialoguesForStage의 명시적 별칭.
        /// </summary>
        public List<StoryDialogueLine> GetStageOriginalDialogues(int stageId, StoryDialogueType dialogueType)
        {
            return GetDialoguesForStage(stageId, dialogueType);
        }

        /// <summary>GetDisplayDialoguesForStage의 명시적 별칭 (외부 명세 호환).</summary>
        public List<StoryDialogueLine> GetStageDisplayDialogues(int stageId, StoryDialogueType dialogueType)
        {
            return GetDisplayDialoguesForStage(stageId, dialogueType);
        }

        /// <summary>현재 스테이지에 대해 GetDisplayDialoguesForStage를 호출하는 헬퍼.</summary>
        public List<StoryDialogueLine> GetCurrentStageDisplayDialogues(StoryDialogueType type)
        {
            if (StageManager.Instance == null || StageManager.Instance.CurrentStageData == null)
            {
                return new List<StoryDialogueLine>();
            }
            return GetDisplayDialoguesForStage(StageManager.Instance.CurrentStageData.StageId, type);
        }

        private List<StoryDialogueLine> ApplyOverridesToDialogues(int stageId, StoryDialogueType type, List<StoryDialogueLine> source)
        {
            List<StoryDialogueLine> result = new List<StoryDialogueLine>();
            if (source == null) return result;

            StoryDialogueOverrideManager om = StoryDialogueOverrideManager.Instance;
            for (int i = 0; i < source.Count; i++)
            {
                StoryDialogueLine line = source[i];
                if (line == null) { result.Add(null); continue; }

                if (om != null && om.HasApproved(stageId, type, i))
                {
                    string approved = om.GetApprovedText(stageId, type, i);
                    // 원본을 수정하지 않기 위해 dialogue만 바뀐 사본을 반환
                    result.Add(line.CloneWithDialogue(approved));
                }
                else
                {
                    // override 없거나 미승인 → 원본 인스턴스 그대로 (읽기 전용 가정)
                    result.Add(line);
                }
            }
            return result;
        }

        private StoryNode FindStoryNodeForStage(int stageId)
        {
            // 1차: 기존 storyNodes 직접 등록 목록
            if (storyNodes != null)
            {
                for (int i = 0; i < storyNodes.Count; i++)
                {
                    StoryNode n = storyNodes[i];
                    if (n != null && n.LinkedStageId == stageId) return n;
                }
            }

            // 2차 fallback: StoryPackManager가 있으면 데이터베이스 전체 팩에서 검색
            if (StoryPackManager.Instance != null)
            {
                StoryNode fromPack = StoryPackManager.Instance.GetStoryNodeByStageId(stageId);
                if (fromPack != null) return fromPack;
            }

            return null;
        }

        private void ValidateStoryNodes()
        {
            if (storyNodes == null || storyNodes.Count == 0) return;

            HashSet<int> seenStoryIds = new HashSet<int>();
            HashSet<int> seenLinkedStageIds = new HashSet<int>();
            for (int i = 0; i < storyNodes.Count; i++)
            {
                StoryNode n = storyNodes[i];
                if (n == null)
                {
                    Debug.LogWarning($"StoryManager: storyNodes[{i}] is null.");
                    continue;
                }
                if (!n.IsValid())
                {
                    Debug.LogWarning($"StoryManager: storyNodes[{i}] '{n.name}' failed IsValid().");
                }
                if (!seenStoryIds.Add(n.StoryId))
                {
                    Debug.LogWarning($"StoryManager: Duplicate storyId {n.StoryId} at storyNodes[{i}].");
                }
                if (!seenLinkedStageIds.Add(n.LinkedStageId))
                {
                    Debug.LogWarning($"StoryManager: Duplicate linkedStageId {n.LinkedStageId} at storyNodes[{i}].");
                }
            }
        }

        /// <summary>디버그용: 현재 스테이지의 모든 대사 묶음을 콘솔에 출력한다.</summary>
        [ContextMenu("Log Current Stage Dialogues")]
        public void LogCurrentStageDialogues()
        {
            foreach (StoryDialogueType t in System.Enum.GetValues(typeof(StoryDialogueType)))
            {
                List<StoryDialogueLine> lines = GetDialoguesForCurrentStage(t);
                Debug.Log($"--- {t} ({lines.Count} lines) ---");
                for (int i = 0; i < lines.Count; i++)
                {
                    StoryDialogueLine line = lines[i];
                    if (line == null) continue;
                    Debug.Log($"  {line.SpeakerName}: {line.Dialogue}");
                }
            }
        }

        // ───────── 기존 StoryTextUI 호환 shim ─────────

        /// <summary>호환용. 현재 스테이지의 StageStart 첫 대사를 StoryLineData로 변환해 반환.</summary>
        public StoryLineData GetCurrentStageStartLine()
        {
            return GetFirstLineAsLegacy(StoryDialogueType.StageStart);
        }

        /// <summary>호환용. 현재 스테이지의 StageClear 첫 대사를 StoryLineData로 변환해 반환.</summary>
        public StoryLineData GetCurrentStageClearLine()
        {
            return GetFirstLineAsLegacy(StoryDialogueType.StageClear);
        }

        /// <summary>호환용. 현재 스테이지의 StageFail 첫 대사를 StoryLineData로 변환해 반환.</summary>
        public StoryLineData GetCurrentStageFailLine()
        {
            return GetFirstLineAsLegacy(StoryDialogueType.StageFail);
        }

        private StoryLineData GetFirstLineAsLegacy(StoryDialogueType type)
        {
            List<StoryDialogueLine> lines = GetDialoguesForCurrentStage(type);
            if (lines == null || lines.Count == 0) return null;
            StoryDialogueLine line = lines[0];
            if (line == null || !line.IsValid()) return null;

            StoryLineData legacy = new StoryLineData();
            legacy.Set(line.SpeakerName, line.Dialogue);
            return legacy;
        }
    }
}
