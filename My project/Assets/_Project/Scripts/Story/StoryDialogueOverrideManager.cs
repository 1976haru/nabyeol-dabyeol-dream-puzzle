using System;
using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Agents;
using NabyeolDabyeolDreamPuzzle.ParentMode;

namespace NabyeolDabyeolDreamPuzzle.Story
{
    /// <summary>
    /// 스토리 대사 override 저장/승인/조회를 담당하는 Singleton.
    /// - 원본 StoryNode asset은 절대 수정하지 않는다.
    /// - 모든 override는 PlayerPrefs에 분산 저장된다. (proposed / approved / isApproved 3개 키)
    /// - 게임 표시 시 GetDisplayDialogue를 통해 마지막에만 교체된다.
    /// - 승인된 문장만 게임에 적용. proposed만 있는 상태는 절대 게임에 노출되지 않는다.
    /// - 승인/취소는 부모 모드(또는 디버그 bypass)에서만 동작한다.
    /// TODO: Add blocked word filter for child-safe story edits.
    /// TODO: Add parent review history (timestamp + edit log).
    /// TODO: Add export/import of approved story overrides (JSON).
    /// </summary>
    public class StoryDialogueOverrideManager : MonoBehaviour
    {
        public static StoryDialogueOverrideManager Instance { get; private set; }

        private const string KeyPrefix = "StoryOverride_";
        private const string KeyListPrefName = "StoryOverride_KeyList";
        private const char KeyListDelimiter = ';';

        [Header("Validation")]
        [SerializeField, Min(1)] private int maxDialogueLength = 50;

        public int MaxDialogueLength => maxDialogueLength;

        /// <summary>(stageId, type, lineIndex, isApproved) 통지.</summary>
        public event Action<int, StoryDialogueType, int, bool> OnOverrideChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("StoryDialogueOverrideManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ───────── Key helpers ─────────

        private static string BaseKey(int stageId, StoryDialogueType type, int lineIndex)
        {
            return $"{stageId}_{type}_{lineIndex}";
        }

        private static string ProposedKey(int stageId, StoryDialogueType type, int lineIndex)
        {
            return KeyPrefix + BaseKey(stageId, type, lineIndex) + "_Proposed";
        }

        private static string ApprovedKey(int stageId, StoryDialogueType type, int lineIndex)
        {
            return KeyPrefix + BaseKey(stageId, type, lineIndex) + "_Approved";
        }

        private static string IsApprovedKey(int stageId, StoryDialogueType type, int lineIndex)
        {
            return KeyPrefix + BaseKey(stageId, type, lineIndex) + "_IsApproved";
        }

        // ───────── KeyList 관리 ─────────

        private HashSet<string> LoadKeyList()
        {
            HashSet<string> set = new HashSet<string>();
            string raw = PlayerPrefs.GetString(KeyListPrefName, string.Empty);
            if (string.IsNullOrEmpty(raw)) return set;
            string[] parts = raw.Split(KeyListDelimiter);
            for (int i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(parts[i])) set.Add(parts[i]);
            }
            return set;
        }

        private void SaveKeyList(HashSet<string> set)
        {
            PlayerPrefs.SetString(KeyListPrefName, string.Join(KeyListDelimiter.ToString(), set));
        }

        private void RegisterKey(int stageId, StoryDialogueType type, int lineIndex)
        {
            HashSet<string> set = LoadKeyList();
            if (set.Add(BaseKey(stageId, type, lineIndex)))
            {
                SaveKeyList(set);
            }
        }

        private void UnregisterKey(int stageId, StoryDialogueType type, int lineIndex)
        {
            HashSet<string> set = LoadKeyList();
            if (set.Remove(BaseKey(stageId, type, lineIndex)))
            {
                SaveKeyList(set);
            }
        }

        // ───────── 문장 정리 ─────────

        /// <summary>앞뒤 공백 제거 + 줄바꿈/탭을 한 칸 공백으로 + 연속 공백 단일화.</summary>
        private string SanitizeDialogue(string text)
        {
            if (text == null) return string.Empty;
            string s = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
            // 연속 공백 단일화 (단순 구현)
            while (s.Contains("  "))
            {
                s = s.Replace("  ", " ");
            }
            return s;
        }

        private bool ValidateLength(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.Length >= 1 && text.Length <= maxDialogueLength;
        }

        // ───────── 저장/승인/취소 ─────────

        /// <summary>
        /// 보호자 수정 제안을 저장. 게임에는 아직 적용되지 않는다 (승인 대기).
        /// </summary>
        public bool SaveProposedDialogue(int stageId, StoryDialogueType dialogueType, int lineIndex, string proposedText)
        {
            if (stageId <= 0)
            {
                Debug.LogWarning($"StoryDialogueOverrideManager: invalid stageId {stageId}.");
                return false;
            }
            if (lineIndex < 0)
            {
                Debug.LogWarning($"StoryDialogueOverrideManager: invalid lineIndex {lineIndex}.");
                return false;
            }
            string cleaned = SanitizeDialogue(proposedText);
            if (!ValidateLength(cleaned))
            {
                Debug.LogWarning($"StoryDialogueOverrideManager: proposedText length invalid (got {cleaned?.Length ?? 0}, max {maxDialogueLength}).");
                return false;
            }

            // 87번 — 안전 필터 v1. proposedText에 금칙어/개인정보/무서운 표현/부정 표현이 있으면 저장 거부.
            if (SafetyFilterAgent.Instance != null)
            {
                SafetyFilterResult safety = SafetyFilterAgent.Instance.CheckDialogue(cleaned);
                if (!safety.isSafe)
                {
                    Debug.LogWarning($"StoryDialogueOverrideManager: SaveProposedDialogue blocked by safety filter (reason={safety.reason}).");
                    return false;
                }
            }

            PlayerPrefs.SetString(ProposedKey(stageId, dialogueType, lineIndex), cleaned);
            // 새 제안은 자동 승인 취소 (이미 승인된 상태였다면 안전상 무효화)
            if (PlayerPrefs.GetInt(IsApprovedKey(stageId, dialogueType, lineIndex), 0) != 0)
            {
                PlayerPrefs.SetInt(IsApprovedKey(stageId, dialogueType, lineIndex), 0);
                PlayerPrefs.DeleteKey(ApprovedKey(stageId, dialogueType, lineIndex));
                Debug.Log($"StoryDialogueOverrideManager: New proposal invalidates previous approval (stage={stageId}, type={dialogueType}, line={lineIndex}).");
            }
            RegisterKey(stageId, dialogueType, lineIndex);
            PlayerPrefs.Save();
            Debug.Log($"StoryDialogueOverrideManager: Proposed saved (stage={stageId}, type={dialogueType}, line={lineIndex}, len={cleaned.Length}).");
            OnOverrideChanged?.Invoke(stageId, dialogueType, lineIndex, false);
            return true;
        }

        /// <summary>
        /// 부모 모드에서 제안 문장을 승인. proposedText를 approvedText로 복사하고 isApproved=true.
        /// 부모 모드가 아닌 상태에서 호출되면 거부.
        /// </summary>
        public bool ApproveDialogue(int stageId, StoryDialogueType dialogueType, int lineIndex)
        {
            if (!RequireParentMode("ApproveDialogue")) return false;
            if (stageId <= 0 || lineIndex < 0)
            {
                Debug.LogWarning($"StoryDialogueOverrideManager: ApproveDialogue invalid args (stage={stageId}, line={lineIndex}).");
                return false;
            }
            string proposed = PlayerPrefs.GetString(ProposedKey(stageId, dialogueType, lineIndex), string.Empty);
            if (string.IsNullOrWhiteSpace(proposed))
            {
                Debug.LogWarning($"StoryDialogueOverrideManager: No proposedText to approve (stage={stageId}, type={dialogueType}, line={lineIndex}).");
                return false;
            }

            // 87번 — 안전 필터 v1. 승인 직전 다시 한번 검증. proposed가 PlayerPrefs 외부 편집되었을 가능성 방어.
            if (SafetyFilterAgent.Instance != null)
            {
                SafetyFilterResult safety = SafetyFilterAgent.Instance.CheckDialogue(proposed);
                if (!safety.isSafe)
                {
                    Debug.LogWarning($"StoryDialogueOverrideManager: ApproveDialogue blocked by safety filter (reason={safety.reason}).");
                    return false;
                }
            }

            PlayerPrefs.SetString(ApprovedKey(stageId, dialogueType, lineIndex), proposed);
            PlayerPrefs.SetInt(IsApprovedKey(stageId, dialogueType, lineIndex), 1);
            RegisterKey(stageId, dialogueType, lineIndex);
            PlayerPrefs.Save();
            Debug.Log($"StoryDialogueOverrideManager: Approved (stage={stageId}, type={dialogueType}, line={lineIndex}).");
            OnOverrideChanged?.Invoke(stageId, dialogueType, lineIndex, true);
            return true;
        }

        /// <summary>
        /// 승인을 취소. isApproved=false + approvedText 삭제.
        /// proposedText는 유지된다 (사용자가 다시 수정 후 재승인할 수 있게).
        /// 부모 모드에서만 동작.
        /// </summary>
        public bool RevokeApproval(int stageId, StoryDialogueType dialogueType, int lineIndex)
        {
            if (!RequireParentMode("RevokeApproval")) return false;
            if (stageId <= 0 || lineIndex < 0) return false;

            PlayerPrefs.DeleteKey(ApprovedKey(stageId, dialogueType, lineIndex));
            PlayerPrefs.SetInt(IsApprovedKey(stageId, dialogueType, lineIndex), 0);
            PlayerPrefs.Save();
            Debug.Log($"StoryDialogueOverrideManager: Approval revoked (stage={stageId}, type={dialogueType}, line={lineIndex}).");
            OnOverrideChanged?.Invoke(stageId, dialogueType, lineIndex, false);
            return true;
        }

        /// <summary>
        /// 특정 라인의 override 데이터 일체를 삭제. proposed/approved/isApproved 모두 제거.
        /// 게임 표시 대사는 원본으로 복귀.
        /// </summary>
        public void ClearOverride(int stageId, StoryDialogueType dialogueType, int lineIndex)
        {
            if (stageId <= 0 || lineIndex < 0) return;
            PlayerPrefs.DeleteKey(ProposedKey(stageId, dialogueType, lineIndex));
            PlayerPrefs.DeleteKey(ApprovedKey(stageId, dialogueType, lineIndex));
            PlayerPrefs.DeleteKey(IsApprovedKey(stageId, dialogueType, lineIndex));
            UnregisterKey(stageId, dialogueType, lineIndex);
            PlayerPrefs.Save();
            Debug.Log($"StoryDialogueOverrideManager: Override cleared (stage={stageId}, type={dialogueType}, line={lineIndex}).");
            OnOverrideChanged?.Invoke(stageId, dialogueType, lineIndex, false);
        }

        [ContextMenu("Clear All Story Dialogue Overrides")]
        public void ClearAllOverrides()
        {
            HashSet<string> set = LoadKeyList();
            int cleared = 0;
            foreach (string baseKey in set)
            {
                if (string.IsNullOrWhiteSpace(baseKey)) continue;
                PlayerPrefs.DeleteKey(KeyPrefix + baseKey + "_Proposed");
                PlayerPrefs.DeleteKey(KeyPrefix + baseKey + "_Approved");
                PlayerPrefs.DeleteKey(KeyPrefix + baseKey + "_IsApproved");
                cleared++;
            }
            PlayerPrefs.DeleteKey(KeyListPrefName);
            PlayerPrefs.Save();
            Debug.Log($"StoryDialogueOverrideManager: Cleared {cleared} override entries.");
            OnOverrideChanged?.Invoke(0, StoryDialogueType.StageStart, -1, false);
        }

        // ───────── 조회 ─────────

        public bool HasProposed(int stageId, StoryDialogueType type, int lineIndex)
        {
            if (stageId <= 0 || lineIndex < 0) return false;
            string s = PlayerPrefs.GetString(ProposedKey(stageId, type, lineIndex), string.Empty);
            return !string.IsNullOrWhiteSpace(s);
        }

        public bool HasApproved(int stageId, StoryDialogueType type, int lineIndex)
        {
            if (stageId <= 0 || lineIndex < 0) return false;
            if (PlayerPrefs.GetInt(IsApprovedKey(stageId, type, lineIndex), 0) == 0) return false;
            string s = PlayerPrefs.GetString(ApprovedKey(stageId, type, lineIndex), string.Empty);
            return !string.IsNullOrWhiteSpace(s);
        }

        public string GetProposedText(int stageId, StoryDialogueType type, int lineIndex)
        {
            if (stageId <= 0 || lineIndex < 0) return string.Empty;
            return PlayerPrefs.GetString(ProposedKey(stageId, type, lineIndex), string.Empty);
        }

        public string GetApprovedText(int stageId, StoryDialogueType type, int lineIndex)
        {
            if (stageId <= 0 || lineIndex < 0) return string.Empty;
            if (PlayerPrefs.GetInt(IsApprovedKey(stageId, type, lineIndex), 0) == 0) return string.Empty;
            return PlayerPrefs.GetString(ApprovedKey(stageId, type, lineIndex), string.Empty);
        }

        // ───────── Export / Import 보조 ─────────

        /// <summary>
        /// 현재 PlayerPrefs에 저장된 모든 override를 export 가능한 형태로 수집한다.
        /// KeyList를 순회해 stageId/dialogueType/lineIndex를 파싱한다.
        /// </summary>
        public System.Collections.Generic.List<NabyeolDabyeolDreamPuzzle.Customization.StoryDialogueOverrideExportData> GetAllOverrideExportData()
        {
            var result = new System.Collections.Generic.List<NabyeolDabyeolDreamPuzzle.Customization.StoryDialogueOverrideExportData>();
            HashSet<string> set = LoadKeyList();
            foreach (string baseKey in set)
            {
                if (string.IsNullOrWhiteSpace(baseKey)) continue;
                if (!TryParseBaseKey(baseKey, out int stageId, out StoryDialogueType type, out int lineIndex))
                {
                    Debug.LogWarning($"StoryDialogueOverrideManager: Failed to parse baseKey '{baseKey}' during export. Skipping.");
                    continue;
                }
                string proposed = PlayerPrefs.GetString(ProposedKey(stageId, type, lineIndex), string.Empty);
                string approved = PlayerPrefs.GetString(ApprovedKey(stageId, type, lineIndex), string.Empty);
                bool isApproved = PlayerPrefs.GetInt(IsApprovedKey(stageId, type, lineIndex), 0) != 0
                                  && !string.IsNullOrWhiteSpace(approved);

                // proposed/approved 모두 비어 있으면 의미 있는 데이터 아님 — skip
                if (string.IsNullOrWhiteSpace(proposed) && string.IsNullOrWhiteSpace(approved)) continue;

                result.Add(new NabyeolDabyeolDreamPuzzle.Customization.StoryDialogueOverrideExportData
                {
                    stageId = stageId,
                    dialogueType = type.ToString(),
                    lineIndex = lineIndex,
                    proposedText = proposed ?? string.Empty,
                    approvedText = approved ?? string.Empty,
                    isApproved = isApproved
                });
            }
            return result;
        }

        /// <summary>
        /// export된 override 목록을 PlayerPrefs에 적용한다.
        /// 부모 모드 가드 없음 — 호출자(PackExportImportManager)가 가드를 책임진다.
        /// 각 항목은 길이/형식 검증을 통과해야 하며 실패 시 해당 항목만 skip.
        /// </summary>
        public int ApplyOverrideExportData(System.Collections.Generic.List<NabyeolDabyeolDreamPuzzle.Customization.StoryDialogueOverrideExportData> overrides)
        {
            int applied = 0;
            if (overrides == null) return 0;
            for (int i = 0; i < overrides.Count; i++)
            {
                var e = overrides[i];
                if (e == null) continue;
                if (e.stageId <= 0 || e.lineIndex < 0) continue;
                if (string.IsNullOrWhiteSpace(e.dialogueType)) continue;
                if (!Enum.TryParse(e.dialogueType, false, out StoryDialogueType type)) continue;

                string proposed = SanitizeDialogue(e.proposedText);
                string approved = SanitizeDialogue(e.approvedText);

                // 두 값 모두 비어있는 항목은 의미 없음 — skip
                if (string.IsNullOrWhiteSpace(proposed) && string.IsNullOrWhiteSpace(approved)) continue;

                // 길이 초과는 해당 항목만 거부
                if (!string.IsNullOrWhiteSpace(proposed) && proposed.Length > maxDialogueLength) continue;
                if (!string.IsNullOrWhiteSpace(approved) && approved.Length > maxDialogueLength) continue;

                // 87번 — 안전 필터. 가져온 외부 팩에 부적절한 문장이 있을 수 있어 항목별 검증.
                if (SafetyFilterAgent.Instance != null)
                {
                    if (!string.IsNullOrWhiteSpace(proposed))
                    {
                        SafetyFilterResult sP = SafetyFilterAgent.Instance.CheckDialogue(proposed);
                        if (!sP.isSafe)
                        {
                            Debug.LogWarning($"StoryDialogueOverrideManager: ApplyOverrideExportData proposed blocked (reason={sP.reason}, stage={e.stageId}, line={e.lineIndex}).");
                            continue;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(approved))
                    {
                        SafetyFilterResult sA = SafetyFilterAgent.Instance.CheckDialogue(approved);
                        if (!sA.isSafe)
                        {
                            Debug.LogWarning($"StoryDialogueOverrideManager: ApplyOverrideExportData approved blocked (reason={sA.reason}, stage={e.stageId}, line={e.lineIndex}).");
                            continue;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(proposed))
                {
                    PlayerPrefs.SetString(ProposedKey(e.stageId, type, e.lineIndex), proposed);
                }
                if (e.isApproved && !string.IsNullOrWhiteSpace(approved))
                {
                    PlayerPrefs.SetString(ApprovedKey(e.stageId, type, e.lineIndex), approved);
                    PlayerPrefs.SetInt(IsApprovedKey(e.stageId, type, e.lineIndex), 1);
                }
                else
                {
                    // isApproved=true인데 approvedText가 비면 skip 정책 — proposed만 적용된 셈
                    PlayerPrefs.SetInt(IsApprovedKey(e.stageId, type, e.lineIndex), 0);
                }
                RegisterKey(e.stageId, type, e.lineIndex);
                applied++;
            }
            PlayerPrefs.Save();
            // 일괄 적용 후 한 번 알림 (개별 항목별로는 발행하지 않음)
            OnOverrideChanged?.Invoke(0, StoryDialogueType.StageStart, -1, false);
            Debug.Log($"StoryDialogueOverrideManager: Applied {applied} overrides from export data.");
            return applied;
        }

        /// <summary>baseKey "<stageId>_<type>_<lineIndex>"를 분해한다.</summary>
        private bool TryParseBaseKey(string baseKey, out int stageId, out StoryDialogueType type, out int lineIndex)
        {
            stageId = -1; type = StoryDialogueType.StageStart; lineIndex = -1;
            if (string.IsNullOrWhiteSpace(baseKey)) return false;
            string[] parts = baseKey.Split('_');
            if (parts.Length != 3) return false;
            if (!int.TryParse(parts[0], out stageId) || stageId <= 0) return false;
            if (!Enum.TryParse(parts[1], false, out type)) return false;
            if (!int.TryParse(parts[2], out lineIndex) || lineIndex < 0) return false;
            return true;
        }

        // ───────── Naming aliases (외부 명세 호환) ─────────

        /// <summary>GetProposedText 별칭. 미리보기/외부 사용 편의를 위한 명명.</summary>
        public string GetProposedDialogue(int stageId, StoryDialogueType type, int lineIndex)
            => GetProposedText(stageId, type, lineIndex);

        /// <summary>GetApprovedText 별칭.</summary>
        public string GetApprovedDialogue(int stageId, StoryDialogueType type, int lineIndex)
            => GetApprovedText(stageId, type, lineIndex);

        /// <summary>승인 + 실제 비어 있지 않은 approvedText가 모두 만족하는지. HasApproved 별칭.</summary>
        public bool IsApproved(int stageId, StoryDialogueType type, int lineIndex)
            => HasApproved(stageId, type, lineIndex);

        /// <summary>
        /// 게임 표시용 최종 문장. 승인된 문장이 있으면 그것을, 없으면 originalText 반환.
        /// 절대로 승인되지 않은 proposedText를 반환하지 않는다.
        /// </summary>
        public string GetDisplayDialogue(int stageId, StoryDialogueType dialogueType, int lineIndex, string originalText)
        {
            if (HasApproved(stageId, dialogueType, lineIndex))
            {
                return GetApprovedText(stageId, dialogueType, lineIndex);
            }
            return originalText ?? string.Empty;
        }

        /// <summary>UI 디버깅용 결합 객체 반환 (원본 + proposed + approved + isApproved).</summary>
        public StoryDialogueOverrideData GetOverrideSnapshot(int stageId, StoryDialogueType type, int lineIndex, string originalText)
        {
            StoryDialogueOverrideData data = new StoryDialogueOverrideData(stageId, type, lineIndex);
            data.originalText = originalText ?? string.Empty;
            data.proposedText = GetProposedText(stageId, type, lineIndex);
            data.approvedText = GetApprovedText(stageId, type, lineIndex);
            data.isApproved = PlayerPrefs.GetInt(IsApprovedKey(stageId, type, lineIndex), 0) != 0
                              && !string.IsNullOrWhiteSpace(data.approvedText);
            return data;
        }

        // ───────── 부모 모드 가드 ─────────

        private bool RequireParentMode(string op)
        {
            if (ParentModeManager.Instance == null)
            {
                Debug.LogWarning($"StoryDialogueOverrideManager: {op} blocked. ParentModeManager not found.");
                return false;
            }
            if (!ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                Debug.LogWarning($"StoryDialogueOverrideManager: {op} blocked. Parent mode is not active.");
                return false;
            }
            return true;
        }
    }
}
