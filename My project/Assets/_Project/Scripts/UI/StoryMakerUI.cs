using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Agents;
using NabyeolDabyeolDreamPuzzle.ParentMode;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 부모가 미리 준비된 안전 후보 중에서 스토리 대사를 골라 proposedText로 저장하는 UI.
    /// - 자유 입력 없음. 3개 후보 버튼 중 하나를 누르면 자동 저장.
    /// - 게임 본문에 즉시 적용되지 않음. 78번 StoryDialogueEditUI 또는 79번 미리보기에서 부모 승인 후에만 적용.
    /// - 부모 모드에서만 열린다.
    /// TODO: Open preview directly with current selection (deep-link to CustomStoryPreviewUI).
    /// </summary>
    public class StoryMakerUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject storyMakerPanel;
        [SerializeField] private Button closeButton;

        [Header("Selector")]
        [SerializeField] private TMP_Dropdown stageDropdown;
        [SerializeField] private TMP_Dropdown dialogueTypeDropdown;
        [SerializeField] private TMP_Dropdown lineDropdown;

        [Header("Original")]
        [SerializeField] private TextMeshProUGUI originalText;
        [SerializeField] private TextMeshProUGUI speakerText;

        [Header("Candidate Buttons")]
        [SerializeField] private Button candidate1Button;
        [SerializeField] private Button candidate2Button;
        [SerializeField] private Button candidate3Button;
        [SerializeField] private TextMeshProUGUI candidate1Text;
        [SerializeField] private TextMeshProUGUI candidate2Text;
        [SerializeField] private TextMeshProUGUI candidate3Text;

        [Header("Misc")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button previewButton;

        [Header("Links")]
        [SerializeField] private ParentModeUI parentModeUI;
        [SerializeField] private CustomStoryPreviewUI previewUI;

        [Header("Stage Range Fallback")]
        [SerializeField, Min(1)] private int fallbackMinStageId = 1;
        [SerializeField, Min(1)] private int fallbackMaxStageId = 32;

        [Header("Strings")]
        [SerializeField] private string parentModeOnlyMessage = "보호자 확인이 필요해요.";
        [SerializeField] private string noLineMessage = "선택된 대사 줄이 없어요.";
        [SerializeField] private string noCandidateMessage = "이 화자/대사 타입에 맞는 후보가 없어요.";
        [SerializeField] private string proposedSavedMessage = "후보 문장을 저장했어요. 승인하면 게임에 적용돼요.";
        [SerializeField] private string proposedFailMessage = "후보 저장에 실패했어요.";
        [SerializeField] private string emptyCandidateLabel = "(후보 없음)";

        private readonly List<int> stageIdCache = new List<int>();
        private readonly List<StoryDialogueType> typeCache = new List<StoryDialogueType>();
        private readonly List<StoryDialogueLine> lineCache = new List<StoryDialogueLine>();
        private readonly List<StoryCandidateData> candidateCache = new List<StoryCandidateData>();

        private Button[] candidateButtons;
        private TextMeshProUGUI[] candidateTexts;

        private void Awake()
        {
            if (storyMakerPanel != null) storyMakerPanel.SetActive(false);

            candidateButtons = new[] { candidate1Button, candidate2Button, candidate3Button };
            candidateTexts = new[] { candidate1Text, candidate2Text, candidate3Text };

            for (int i = 0; i < candidateButtons.Length; i++)
            {
                int idx = i; // 람다 캡처
                Button b = candidateButtons[i];
                if (b == null) continue;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => OnCandidateClicked(idx));
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
                closeButton.onClick.AddListener(ClosePanel);
            }
            if (previewButton != null)
            {
                previewButton.onClick.RemoveListener(OnPreviewClicked);
                previewButton.onClick.AddListener(OnPreviewClicked);
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
        }

        private void OnDestroy()
        {
            if (candidateButtons != null)
            {
                for (int i = 0; i < candidateButtons.Length; i++)
                {
                    if (candidateButtons[i] != null) candidateButtons[i].onClick.RemoveAllListeners();
                }
            }
            if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);
            if (previewButton != null) previewButton.onClick.RemoveListener(OnPreviewClicked);
            if (stageDropdown != null) stageDropdown.onValueChanged.RemoveListener(OnStageChanged);
            if (dialogueTypeDropdown != null) dialogueTypeDropdown.onValueChanged.RemoveListener(OnTypeChanged);
            if (lineDropdown != null) lineDropdown.onValueChanged.RemoveListener(OnLineChanged);
        }

        public void OpenPanel()
        {
            if (ParentModeManager.Instance == null || !ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                ShowMessage(parentModeOnlyMessage);
                if (parentModeUI != null) parentModeUI.OpenParentCheck();
                Debug.Log("StoryMakerUI: Parent mode required. Redirecting to parent check.");
                return;
            }
            PopulateStageDropdown();
            PopulateTypeDropdown();
            PopulateLineDropdown();
            RefreshOriginalAndCandidates();
            if (storyMakerPanel != null) storyMakerPanel.SetActive(true);
            ShowMessage(string.Empty);
            Debug.Log("StoryMakerUI: Panel opened.");
        }

        public void ClosePanel()
        {
            if (storyMakerPanel != null) storyMakerPanel.SetActive(false);
            Debug.Log("StoryMakerUI: Panel closed.");
        }

        // ───────── Dropdown 구성 ─────────

        private void PopulateStageDropdown()
        {
            stageIdCache.Clear();
            if (StoryManager.Instance != null)
            {
                for (int sid = fallbackMinStageId; sid <= fallbackMaxStageId; sid++)
                {
                    if (StoryManager.Instance.HasStoryForStage(sid)) stageIdCache.Add(sid);
                }
            }
            if (stageIdCache.Count == 0)
            {
                for (int sid = fallbackMinStageId; sid <= fallbackMaxStageId; sid++) stageIdCache.Add(sid);
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
            int sid = GetSelectedStageId();
            StoryDialogueType type = GetSelectedType();

            if (StoryManager.Instance != null && sid > 0)
            {
                List<StoryDialogueLine> src = StoryManager.Instance.GetStageOriginalDialogues(sid, type);
                if (src != null)
                {
                    for (int i = 0; i < src.Count; i++)
                    {
                        if (src[i] == null) continue;
                        lineCache.Add(src[i]);
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
                    string speaker = ln != null && !string.IsNullOrWhiteSpace(ln.SpeakerName) ? ln.SpeakerName : "?";
                    string body = ln != null ? (ln.Dialogue ?? string.Empty) : string.Empty;
                    if (body.Length > 18) body = body.Substring(0, 18) + "…";
                    options.Add($"{i} {speaker}: {body}");
                }
                if (options.Count == 0) options.Add(noLineMessage);
                lineDropdown.AddOptions(options);
                lineDropdown.value = 0;
                lineDropdown.RefreshShownValue();
            }
        }

        // ───────── 원본 표시 + 후보 갱신 ─────────

        private void RefreshOriginalAndCandidates()
        {
            StoryDialogueLine line = GetSelectedLine();
            string original = line != null ? line.Dialogue : string.Empty;
            string speaker = line != null
                ? (!string.IsNullOrWhiteSpace(line.SpeakerName) ? line.SpeakerName : line.SpeakerId)
                : string.Empty;
            if (originalText != null) originalText.text = original ?? string.Empty;
            if (speakerText != null) speakerText.text = speaker ?? string.Empty;

            // 후보 3개 갱신
            candidateCache.Clear();
            if (StoryMakerAgent.Instance != null && line != null)
            {
                List<StoryCandidateData> got = StoryMakerAgent.Instance.GetCandidatesForLine(line, GetSelectedType());
                if (got != null) candidateCache.AddRange(got);
            }

            ApplyCandidateButtons();

            if (line == null) ShowMessage(noLineMessage);
            else if (candidateCache.Count == 0) ShowMessage(noCandidateMessage);
        }

        private void ApplyCandidateButtons()
        {
            if (candidateButtons == null || candidateTexts == null) return;
            int slotCount = candidateButtons.Length;
            for (int i = 0; i < slotCount; i++)
            {
                Button b = candidateButtons[i];
                TextMeshProUGUI t = candidateTexts[i];
                if (i < candidateCache.Count)
                {
                    if (b != null) b.interactable = true;
                    if (t != null) t.text = candidateCache[i] != null ? candidateCache[i].Text : emptyCandidateLabel;
                }
                else
                {
                    if (b != null) b.interactable = false;
                    if (t != null) t.text = emptyCandidateLabel;
                }
            }
        }

        // ───────── 이벤트 ─────────

        private void OnStageChanged(int idx) { PopulateLineDropdown(); RefreshOriginalAndCandidates(); ShowMessage(string.Empty); }
        private void OnTypeChanged(int idx)  { PopulateLineDropdown(); RefreshOriginalAndCandidates(); ShowMessage(string.Empty); }
        private void OnLineChanged(int idx)  { RefreshOriginalAndCandidates(); ShowMessage(string.Empty); }

        private void OnCandidateClicked(int idx)
        {
            if (idx < 0 || idx >= candidateCache.Count) return;
            StoryCandidateData c = candidateCache[idx];
            if (c == null) return;
            int sid = GetSelectedStageId();
            int lineIndex = GetSelectedLineIndex();
            if (sid <= 0 || lineIndex < 0) { ShowMessage(noLineMessage); return; }
            if (StoryMakerAgent.Instance == null) { ShowMessage(proposedFailMessage); return; }

            bool ok = StoryMakerAgent.Instance.SaveCandidateAsProposed(sid, GetSelectedType(), lineIndex, c);
            ShowMessage(ok ? proposedSavedMessage : proposedFailMessage);
            Debug.Log($"StoryMakerUI: Candidate clicked idx={idx}, id='{c.Id}', saved={ok}.");
        }

        private void OnPreviewClicked()
        {
            if (previewUI == null)
            {
                Debug.Log("StoryMakerUI: previewUI not assigned. TODO: Open preview directly with current selection.");
                return;
            }
            previewUI.OpenPreviewPanel();
            Debug.Log("StoryMakerUI: Preview panel opened from Story Maker.");
        }

        // ───────── Selectors ─────────

        private int GetSelectedStageId()
        {
            if (stageDropdown == null || stageIdCache.Count == 0) return -1;
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
            if (lineDropdown == null || lineCache.Count == 0) return -1;
            int idx = lineDropdown.value;
            if (idx < 0 || idx >= lineCache.Count) return -1;
            return idx;
        }

        private StoryDialogueLine GetSelectedLine()
        {
            int idx = GetSelectedLineIndex();
            if (idx < 0) return null;
            return lineCache[idx];
        }

        private void ShowMessage(string msg)
        {
            if (messageText != null) messageText.text = msg ?? string.Empty;
        }
    }
}
