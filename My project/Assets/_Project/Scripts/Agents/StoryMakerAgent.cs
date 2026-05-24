using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.ParentMode;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.Agents
{
    /// <summary>
    /// Story Maker v1 — 부모가 자유 입력 대신 미리 큐레이션된 후보 문장을 선택해 스토리 대사를 바꾸도록 돕는다.
    /// - 실제 AI 호출 없음. StoryCandidatePack(ScriptableObject) 기반의 안전 문장만 제공.
    /// - 선택된 후보는 StoryDialogueOverrideManager.SaveProposedDialogue로 proposedText 저장.
    /// - 게임 본문에는 78번 부모 승인이 통과한 approvedText만 반영.
    /// - 부모 모드에서만 동작.
    /// TODO: Plug in real AI generation as v2.
    /// TODO: Add cooldown/limit to prevent excessive proposed override changes.
    /// </summary>
    public class StoryMakerAgent : MonoBehaviour
    {
        public static StoryMakerAgent Instance { get; private set; }

        [Header("Candidate Pack")]
        [SerializeField] private StoryCandidatePack candidatePack;

        [Header("Validation")]
        [SerializeField, Min(1)] private int maxCandidateLength = 50;
        [SerializeField, Min(1)] private int defaultMaxCount = 3;

        public int MaxCandidateLength => maxCandidateLength;
        public int DefaultMaxCount => defaultMaxCount;
        public StoryCandidatePack CandidatePack => candidatePack;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("StoryMakerAgent: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ───────── 후보 조회 ─────────

        /// <summary>
        /// speakerId/dialogueType 기준 후보 N개 반환.
        /// 1차: speakerId 일치 + (dialogueType 일치 또는 applicableToAllTypes)
        /// 2차: 공통 후보 (targetSpeakerId 비어있음) + dialogueType 일치
        /// 후보 부족 시 공통/모든 타입 후보로 보충
        /// </summary>
        public List<StoryCandidateData> GetCandidates(string speakerId, StoryDialogueType dialogueType, int maxCount = -1)
        {
            int limit = maxCount > 0 ? maxCount : defaultMaxCount;
            List<StoryCandidateData> result = new List<StoryCandidateData>();
            if (candidatePack == null)
            {
                Debug.LogWarning("StoryMakerAgent: candidatePack not assigned.");
                return result;
            }

            IReadOnlyList<StoryCandidateData> all = candidatePack.Candidates;
            if (all == null) return result;

            // 1차 — 화자별 + 대사 타입 정확 매칭
            for (int i = 0; i < all.Count; i++)
            {
                StoryCandidateData c = all[i];
                if (c == null || !c.IsValid()) continue;
                if (!IsLengthOk(c)) continue;
                bool sameSpeaker = !string.IsNullOrWhiteSpace(speakerId)
                                   && c.TargetSpeakerId == speakerId;
                bool typeOk = c.ApplicableToAllTypes || c.TargetDialogueType == dialogueType;
                if (sameSpeaker && typeOk)
                {
                    result.Add(c);
                    if (result.Count >= limit) return result;
                }
            }

            // 2차 — 공통 화자 + 대사 타입 정확 매칭 (보충)
            for (int i = 0; i < all.Count && result.Count < limit; i++)
            {
                StoryCandidateData c = all[i];
                if (c == null || !c.IsValid()) continue;
                if (!IsLengthOk(c)) continue;
                if (result.Contains(c)) continue;
                bool isCommon = string.IsNullOrWhiteSpace(c.TargetSpeakerId);
                bool typeOk = c.ApplicableToAllTypes || c.TargetDialogueType == dialogueType;
                if (isCommon && typeOk)
                {
                    result.Add(c);
                }
            }

            // 3차 — 그래도 부족하면 applicableToAllTypes 후보로 채움
            for (int i = 0; i < all.Count && result.Count < limit; i++)
            {
                StoryCandidateData c = all[i];
                if (c == null || !c.IsValid()) continue;
                if (!IsLengthOk(c)) continue;
                if (result.Contains(c)) continue;
                if (c.ApplicableToAllTypes) result.Add(c);
            }

            return result;
        }

        /// <summary>StoryDialogueLine을 받아 speakerId 기반 후보를 반환.</summary>
        public List<StoryCandidateData> GetCandidatesForLine(StoryDialogueLine line, StoryDialogueType dialogueType, int maxCount = -1)
        {
            string sid = line != null ? line.SpeakerId : string.Empty;
            return GetCandidates(sid, dialogueType, maxCount);
        }

        private bool IsLengthOk(StoryCandidateData c)
        {
            if (c == null || string.IsNullOrWhiteSpace(c.Text)) return false;
            if (c.Text.Length > maxCandidateLength)
            {
                Debug.LogWarning($"StoryMakerAgent: candidate '{c.Id}' exceeds {maxCandidateLength} chars ({c.Text.Length}). Filtered out.");
                return false;
            }
            return true;
        }

        // ───────── 저장 ─────────

        /// <summary>
        /// 선택한 후보를 proposedText로 저장. 부모 모드에서만 동작.
        /// 게임 본문에는 즉시 반영되지 않으며, 78번 승인 후에만 적용된다.
        /// </summary>
        public bool SaveCandidateAsProposed(int stageId, StoryDialogueType dialogueType, int lineIndex, StoryCandidateData candidate)
        {
            if (!RequireParentMode("SaveCandidateAsProposed")) return false;
            if (candidate == null)
            {
                Debug.LogWarning("StoryMakerAgent: candidate is null.");
                return false;
            }
            if (!IsLengthOk(candidate)) return false;

            // 87번 — 후보팩에 실수로 부적절한 문장이 들어가도 저장되지 않도록 명시 검사.
            if (SafetyFilterAgent.Instance != null)
            {
                SafetyFilterResult safety = SafetyFilterAgent.Instance.CheckDialogue(candidate.Text);
                if (!safety.isSafe)
                {
                    Debug.LogWarning($"StoryMakerAgent: Candidate '{candidate.Id}' blocked by safety filter (reason={safety.reason}).");
                    return false;
                }
            }

            if (StoryDialogueOverrideManager.Instance == null)
            {
                Debug.LogWarning("StoryMakerAgent: StoryDialogueOverrideManager not found.");
                return false;
            }
            bool ok = StoryDialogueOverrideManager.Instance.SaveProposedDialogue(stageId, dialogueType, lineIndex, candidate.Text);
            if (ok)
            {
                Debug.Log($"StoryMakerAgent: Candidate '{candidate.Id}' saved as proposed (stage={stageId}, type={dialogueType}, line={lineIndex}).");
            }
            return ok;
        }

        private bool RequireParentMode(string op)
        {
            if (ParentModeManager.Instance == null)
            {
                Debug.LogWarning($"StoryMakerAgent: {op} blocked. ParentModeManager not found.");
                return false;
            }
            if (!ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                Debug.LogWarning($"StoryMakerAgent: {op} blocked. Parent mode is not active.");
                return false;
            }
            return true;
        }
    }
}
