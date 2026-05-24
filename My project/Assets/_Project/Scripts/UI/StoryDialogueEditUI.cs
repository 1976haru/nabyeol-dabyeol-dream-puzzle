using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Customization;
using NabyeolDabyeolDreamPuzzle.ParentMode;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 보호자가 스토리 대사를 수정/승인하는 UI.
    /// 부모 모드에서만 접근. 승인 전 문장은 절대 게임에 표시되지 않는다.
    /// stageId/dialogueType/lineIndex 삼중 dropdown으로 한 라인을 선택 → 원본·제안·승인 상태 표시 → 4개 액션.
    /// </summary>
    public class StoryDialogueEditUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject editPanel;
        [SerializeField] private Button closeButton;

        [Header("Selector")]
        [SerializeField] private TMP_Dropdown stageDropdown;
        [SerializeField] private TMP_Dropdown dialogueTypeDropdown;
        [SerializeField] private TMP_Dropdown lineDropdown;

        [Header("Display")]
        [SerializeField] private TextMeshProUGUI originalText;
        [SerializeField] private TMP_InputField proposedInput;
        [SerializeField] private TextMeshProUGUI approvedText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Actions")]
        [SerializeField] private Button saveProposedButton;
        [SerializeField] private Button approveButton;
        [SerializeField] private Button revokeButton;
        [SerializeField] private Button clearButton;

        [Header("Parent Mode Link")]
        [SerializeField] private ParentModeUI parentModeUI;

        [Header("Stage Range Fallback")]
        [Tooltip("StagePackManager / StageManager로부터 stage 목록을 얻을 수 없을 때 직접 채울 stageId 범위 (최소~최대).")]
        [SerializeField, Min(1)] private int fallbackMinStageId = 1;
        [SerializeField, Min(1)] private int fallbackMaxStageId = 32;

        [Header("Messages")]
        [SerializeField] private string parentModeOnlyMessage = "보호자 확인이 필요해요.";
        [SerializeField] private string emptyInputMessage = "수정할 문장을 입력해 주세요.";
        [SerializeField] private string savedProposedMessage = "수정 제안을 저장했어요. 승인 후에만 게임에 적용돼요.";
        [SerializeField] private string approvedMessage = "승인되었어요. 게임에 적용돼요.";
        [SerializeField] private string revokedMessage = "승인을 취소했어요. 원본 문장으로 돌아가요.";
        [SerializeField] private string clearedMessage = "원본 문장으로 복구했어요.";
        [SerializeField] private string noLineMessage = "선택된 대사 줄이 없어요.";
        [SerializeField] private string statusUsingOriginal = "원본 사용 중";
        [SerializeField] private string statusPending = "승인 대기 중";
        [SerializeField] private string statusApproved = "승인된 문장 적용 중";
        [SerializeField] private string lengthLimitFormat = "최대 {0}자까지 입력할 수 있어요.";

        private readonly List<int> stageIdCache = new List<int>();
        private readonly List<StoryDialogueType> typeCache = new List<StoryDialogueType>();
        private readonly List<StoryDialogueLine> lineCache = new List<StoryDialogueLine>();

        private void Awake()
        {
            if (editPanel != null) editPanel.SetActive(false);

            if (saveProposedButton != null)
            {
                saveProposedButton.onClick.RemoveListener(SaveProposed);
                saveProposedButton.onClick.AddListener(SaveProposed);
            }
            if (approveButton != null)
            {
                approveButton.onClick.RemoveListener(Approve);
                approveButton.onClick.AddListener(Approve);
            }
            if (revokeButton != null)
            {
                revokeButton.onClick.RemoveListener(Revoke);
                revokeButton.onClick.AddListener(Revoke);
            }
            if (clearButton != null)
            {
                clearButton.onClick.RemoveListener(ClearOverride);
                clearButton.onClick.AddListener(ClearOverride);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseEditMenu);
                closeButton.onClick.AddListener(CloseEditMenu);
            }
            if (stageDropdown != null)
            {
                stageDropdown.onValueChanged.RemoveListener(OnStageChanged);
                stageDropdown.onValueChanged.AddListener(OnStageChanged);
            }
            if (dialogueTypeDropdown != null)
            {
                dialogueTypeDropdown.onValueChanged.RemoveListener(OnTypeChanged);
                dialogueTypeDropdown.onValueChanged.AddListener(OnTypeChanged);
            }
            if (lineDropdown != null)
            {
                lineDropdown.onValueChanged.RemoveListener(OnLineChanged);
                lineDropdown.onValueChanged.AddListener(OnLineChanged);
            }
            if (proposedInput != null)
            {
                proposedInput.contentType = TMP_InputField.ContentType.Standard;
                int max = StoryDialogueOverrideManager.Instance != null
                    ? StoryDialogueOverrideManager.Instance.MaxDialogueLength
                    : 50;
                proposedInput.characterLimit = max;
            }
        }

        private void OnEnable()
        {
            if (CustomizationResetManager.Instance != null)
            {
                CustomizationResetManager.Instance.OnCustomizationReset += HandleCustomizationReset;
            }
        }

        private void OnDisable()
        {
            if (CustomizationResetManager.Instance != null)
            {
                CustomizationResetManager.Instance.OnCustomizationReset -= HandleCustomizationReset;
            }
        }

        private void OnDestroy()
        {
            if (saveProposedButton != null) saveProposedButton.onClick.RemoveListener(SaveProposed);
            if (approveButton != null) approveButton.onClick.RemoveListener(Approve);
            if (revokeButton != null) revokeButton.onClick.RemoveListener(Revoke);
            if (clearButton != null) clearButton.onClick.RemoveListener(ClearOverride);
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseEditMenu);
            if (stageDropdown != null) stageDropdown.onValueChanged.RemoveListener(OnStageChanged);
            if (dialogueTypeDropdown != null) dialogueTypeDropdown.onValueChanged.RemoveListener(OnTypeChanged);
            if (lineDropdown != null) lineDropdown.onValueChanged.RemoveListener(OnLineChanged);
        }

        /// <summary>커스터마이징 일괄 복구 후 현재 선택 라인의 상태를 다시 표시한다.</summary>
        private void HandleCustomizationReset()
        {
            if (editPanel != null && editPanel.activeSelf)
            {
                if (proposedInput != null) proposedInput.text = string.Empty;
                RefreshDisplay();
                Debug.Log("StoryDialogueEditUI: Refreshed display after customization reset.");
            }
        }

        public void OpenEditMenu()
        {
            if (ParentModeManager.Instance == null || !ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                ShowMessage(parentModeOnlyMessage);
                if (parentModeUI != null) parentModeUI.OpenParentCheck();
                Debug.Log("StoryDialogueEditUI: Parent mode required. Redirecting to parent check.");
                return;
            }

            PopulateStageDropdown();
            PopulateTypeDropdown();
            PopulateLineDropdown();
            RefreshDisplay();
            if (editPanel != null) editPanel.SetActive(true);
            ShowMessage(string.Empty);
            Debug.Log("StoryDialogueEditUI: Edit panel opened.");
        }

        public void CloseEditMenu()
        {
            if (editPanel != null) editPanel.SetActive(false);
        }

        // ───────── Dropdown 구성 ─────────

        private void PopulateStageDropdown()
        {
            stageIdCache.Clear();
            // StoryManager가 있으면 등록된 StoryNode의 linkedStageId만 노출하고, 없으면 fallback 범위.
            if (StoryManager.Instance != null)
            {
                for (int sid = fallbackMinStageId; sid <= fallbackMaxStageId; sid++)
                {
                    if (StoryManager.Instance.HasStoryForStage(sid))
                    {
                        stageIdCache.Add(sid);
                    }
                }
            }
            if (stageIdCache.Count == 0)
            {
                // 마지막 fallback: 범위 그대로
                for (int sid = fallbackMinStageId; sid <= fallbackMaxStageId; sid++)
                {
                    stageIdCache.Add(sid);
                }
            }

            if (stageDropdown != null)
            {
                stageDropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < stageIdCache.Count; i++) options.Add("Stage " + stageIdCache[i]);
                stageDropdown.AddOptions(options);
                stageDropdown.value = 0;
                stageDropdown.RefreshShownValue();
            }
        }

        private void PopulateTypeDropdown()
        {
            typeCache.Clear();
            typeCache.Add(StoryDialogueType.StageStart);
            typeCache.Add(StoryDialogueType.StageClear);
            typeCache.Add(StoryDialogueType.StageFail);
            typeCache.Add(StoryDialogueType.BossIntro);

            if (dialogueTypeDropdown != null)
            {
                dialogueTypeDropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < typeCache.Count; i++) options.Add(typeCache[i].ToString());
                dialogueTypeDropdown.AddOptions(options);
                dialogueTypeDropdown.value = 0;
                dialogueTypeDropdown.RefreshShownValue();
            }
        }

        private void PopulateLineDropdown()
        {
            lineCache.Clear();
            int stageId = GetSelectedStageId();
            StoryDialogueType type = GetSelectedType();

            if (StoryManager.Instance != null && stageId > 0)
            {
                // 원본 대사 리스트 사용 (override 적용 전). 보호자에게 원본을 보여주기 위함.
                List<StoryDialogueLine> source = StoryManager.Instance.GetDialoguesForStage(stageId, type);
                if (source != null)
                {
                    for (int i = 0; i < source.Count; i++)
                    {
                        if (source[i] == null) continue;
                        lineCache.Add(source[i]);
                    }
                }
            }

            if (lineDropdown != null)
            {
                lineDropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < lineCache.Count; i++)
                {
                    StoryDialogueLine ln = lineCache[i];
                    string preview = ln != null ? ln.Dialogue : "(null)";
                    if (preview != null && preview.Length > 24) preview = preview.Substring(0, 24) + "…";
                    string speaker = (ln != null && !string.IsNullOrWhiteSpace(ln.SpeakerName)) ? ln.SpeakerName : "?";
                    options.Add($"[{i}] {speaker}: {preview}");
                }
                if (options.Count == 0) options.Add(noLineMessage);
                lineDropdown.AddOptions(options);
                lineDropdown.value = 0;
                lineDropdown.RefreshShownValue();
            }
        }

        // ───────── 표시 갱신 ─────────

        private void RefreshDisplay()
        {
            int stageId = GetSelectedStageId();
            StoryDialogueType type = GetSelectedType();
            int lineIndex = GetSelectedLineIndex();

            string originalDialogue = string.Empty;
            if (lineIndex >= 0 && lineIndex < lineCache.Count && lineCache[lineIndex] != null)
            {
                originalDialogue = lineCache[lineIndex].Dialogue;
            }
            if (originalText != null) originalText.text = originalDialogue;

            string proposed = string.Empty;
            string approved = string.Empty;
            bool isApproved = false;
            if (StoryDialogueOverrideManager.Instance != null && stageId > 0 && lineIndex >= 0)
            {
                proposed = StoryDialogueOverrideManager.Instance.GetProposedText(stageId, type, lineIndex);
                approved = StoryDialogueOverrideManager.Instance.GetApprovedText(stageId, type, lineIndex);
                isApproved = StoryDialogueOverrideManager.Instance.HasApproved(stageId, type, lineIndex);
            }

            if (proposedInput != null) proposedInput.text = proposed;
            if (approvedText != null) approvedText.text = approved;

            if (statusText != null)
            {
                if (isApproved) statusText.text = statusApproved;
                else if (!string.IsNullOrWhiteSpace(proposed)) statusText.text = statusPending;
                else statusText.text = statusUsingOriginal;
            }
        }

        // ───────── Dropdown 이벤트 ─────────

        private void OnStageChanged(int idx)
        {
            PopulateLineDropdown();
            RefreshDisplay();
            ShowMessage(string.Empty);
        }

        private void OnTypeChanged(int idx)
        {
            PopulateLineDropdown();
            RefreshDisplay();
            ShowMessage(string.Empty);
        }

        private void OnLineChanged(int idx)
        {
            RefreshDisplay();
            ShowMessage(string.Empty);
        }

        // ───────── 버튼 핸들러 ─────────

        public void SaveProposed()
        {
            if (StoryDialogueOverrideManager.Instance == null) return;
            int stageId = GetSelectedStageId();
            StoryDialogueType type = GetSelectedType();
            int lineIndex = GetSelectedLineIndex();
            if (lineIndex < 0) { ShowMessage(noLineMessage); return; }

            string input = proposedInput != null ? proposedInput.text : string.Empty;
            if (string.IsNullOrWhiteSpace(input)) { ShowMessage(emptyInputMessage); return; }

            bool ok = StoryDialogueOverrideManager.Instance.SaveProposedDialogue(stageId, type, lineIndex, input);
            if (!ok)
            {
                int max = StoryDialogueOverrideManager.Instance.MaxDialogueLength;
                ShowMessage(string.Format(lengthLimitFormat, max));
                return;
            }
            ShowMessage(savedProposedMessage);
            RefreshDisplay();
        }

        public void Approve()
        {
            if (StoryDialogueOverrideManager.Instance == null) return;
            int stageId = GetSelectedStageId();
            StoryDialogueType type = GetSelectedType();
            int lineIndex = GetSelectedLineIndex();
            if (lineIndex < 0) { ShowMessage(noLineMessage); return; }

            bool ok = StoryDialogueOverrideManager.Instance.ApproveDialogue(stageId, type, lineIndex);
            if (!ok)
            {
                ShowMessage(parentModeOnlyMessage);
                return;
            }
            ShowMessage(approvedMessage);
            RefreshDisplay();
        }

        public void Revoke()
        {
            if (StoryDialogueOverrideManager.Instance == null) return;
            int stageId = GetSelectedStageId();
            StoryDialogueType type = GetSelectedType();
            int lineIndex = GetSelectedLineIndex();
            if (lineIndex < 0) { ShowMessage(noLineMessage); return; }

            bool ok = StoryDialogueOverrideManager.Instance.RevokeApproval(stageId, type, lineIndex);
            if (!ok)
            {
                ShowMessage(parentModeOnlyMessage);
                return;
            }
            ShowMessage(revokedMessage);
            RefreshDisplay();
        }

        public void ClearOverride()
        {
            if (StoryDialogueOverrideManager.Instance == null) return;
            int stageId = GetSelectedStageId();
            StoryDialogueType type = GetSelectedType();
            int lineIndex = GetSelectedLineIndex();
            if (lineIndex < 0) { ShowMessage(noLineMessage); return; }

            StoryDialogueOverrideManager.Instance.ClearOverride(stageId, type, lineIndex);
            ShowMessage(clearedMessage);
            if (proposedInput != null) proposedInput.text = string.Empty;
            RefreshDisplay();
        }

        // ───────── 선택값 헬퍼 ─────────

        private int GetSelectedStageId()
        {
            if (stageDropdown == null) return -1;
            int idx = stageDropdown.value;
            if (idx < 0 || idx >= stageIdCache.Count) return -1;
            return stageIdCache[idx];
        }

        private StoryDialogueType GetSelectedType()
        {
            if (dialogueTypeDropdown == null || typeCache.Count == 0) return StoryDialogueType.StageStart;
            int idx = dialogueTypeDropdown.value;
            if (idx < 0 || idx >= typeCache.Count) return StoryDialogueType.StageStart;
            return typeCache[idx];
        }

        private int GetSelectedLineIndex()
        {
            if (lineDropdown == null) return -1;
            if (lineCache.Count == 0) return -1;
            int idx = lineDropdown.value;
            if (idx < 0 || idx >= lineCache.Count) return -1;
            return idx;
        }

        private void ShowMessage(string msg)
        {
            if (messageText != null) messageText.text = msg ?? string.Empty;
        }
    }
}
