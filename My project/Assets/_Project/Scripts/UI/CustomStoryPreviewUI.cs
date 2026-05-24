using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Character;
using NabyeolDabyeolDreamPuzzle.Customization;
using NabyeolDabyeolDreamPuzzle.ParentMode;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 부모가 수정한 캐릭터 이름 별칭(#76), 대표 대사(#77), 스토리 대사 override(#78)를
    /// 실제 게임에 적용하기 전에 "플레이 화면처럼" 미리 확인하는 UI.
    /// - 부모 모드에서만 열린다.
    /// - StoryNode 원본은 절대 수정하지 않는다.
    /// - 제안문 임시 미리보기 토글은 UI 내부 표시만 바꾸며 StoryManager의 적용 대사 흐름에는 영향이 없다.
    /// </summary>
    public class CustomStoryPreviewUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject previewPanel;
        [SerializeField] private Button closeButton;

        [Header("Selector")]
        [SerializeField] private TMP_Dropdown stageDropdown;
        [SerializeField] private TMP_Dropdown dialogueTypeDropdown;
        [SerializeField] private TMP_Dropdown lineDropdown;

        [Header("Preview (플레이 화면 모사)")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Toggle previewProposedToggle;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Compare Section")]
        [SerializeField] private TextMeshProUGUI originalText;
        [SerializeField] private TextMeshProUGUI proposedText;
        [SerializeField] private TextMeshProUGUI approvedText;

        [Header("Actions")]
        [SerializeField] private Button approveButton;
        [SerializeField] private Button openEditButton;

        [Header("Links")]
        [SerializeField] private ParentModeUI parentModeUI;
        [Tooltip("openEditButton 클릭 시 열 편집 UI (#78 StoryDialogueEditUI).")]
        [SerializeField] private StoryDialogueEditUI storyEditUI;

        [Header("Stage Range Fallback")]
        [SerializeField, Min(1)] private int fallbackMinStageId = 1;
        [SerializeField, Min(1)] private int fallbackMaxStageId = 32;

        [Header("Messages")]
        [SerializeField] private string statusUsingOriginal = "원본 대사가 표시됩니다.";
        [SerializeField] private string statusPendingNotApplied = "수정 제안이 있지만 아직 승인되지 않았습니다.";
        [SerializeField] private string statusApproved = "승인된 문장이 게임에 표시됩니다.";
        [SerializeField] private string statusProposedPreviewing = "제안문을 임시 미리보기 중입니다. (실제 게임에는 적용되지 않습니다)";
        [SerializeField] private string noLineMessage = "선택된 대사 줄이 없어요.";
        [SerializeField] private string noProposedMessage = "제안 문장이 없습니다.";
        [SerializeField] private string noApprovedMessage = "승인된 문장이 없습니다.";
        [SerializeField] private string parentModeOnlyMessage = "보호자 확인이 필요해요.";
        [SerializeField] private string noStoryNodeMessage = "이 스테이지에는 등록된 스토리가 없어요.";
        [SerializeField] private string approveSuccessMessage = "승인되었어요. 이제 게임에 반영됩니다.";
        [SerializeField] private string approveFailMessage = "승인할 수 있는 제안 문장이 없어요.";

        private readonly List<int> stageIdCache = new List<int>();
        private readonly List<StoryDialogueType> typeCache = new List<StoryDialogueType>();
        private readonly List<StoryDialogueLine> lineCache = new List<StoryDialogueLine>();

        private void Awake()
        {
            if (previewPanel != null) previewPanel.SetActive(false);

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePreviewPanel);
                closeButton.onClick.AddListener(ClosePreviewPanel);
            }
            if (approveButton != null)
            {
                approveButton.onClick.RemoveListener(ApproveProposed);
                approveButton.onClick.AddListener(ApproveProposed);
            }
            if (openEditButton != null)
            {
                openEditButton.onClick.RemoveListener(OpenEditUI);
                openEditButton.onClick.AddListener(OpenEditUI);
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
            if (previewProposedToggle != null)
            {
                previewProposedToggle.onValueChanged.RemoveListener(OnPreviewProposedToggled);
                previewProposedToggle.onValueChanged.AddListener(OnPreviewProposedToggled);
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
            if (closeButton != null) closeButton.onClick.RemoveListener(ClosePreviewPanel);
            if (approveButton != null) approveButton.onClick.RemoveListener(ApproveProposed);
            if (openEditButton != null) openEditButton.onClick.RemoveListener(OpenEditUI);
            if (stageDropdown != null) stageDropdown.onValueChanged.RemoveListener(OnStageChanged);
            if (dialogueTypeDropdown != null) dialogueTypeDropdown.onValueChanged.RemoveListener(OnTypeChanged);
            if (lineDropdown != null) lineDropdown.onValueChanged.RemoveListener(OnLineChanged);
            if (previewProposedToggle != null) previewProposedToggle.onValueChanged.RemoveListener(OnPreviewProposedToggled);
        }

        /// <summary>커스터마이징 일괄 복구 후 미리보기를 다시 갱신한다.</summary>
        private void HandleCustomizationReset()
        {
            // 패널이 열려 있지 않으면 굳이 갱신할 필요 없지만, 다음 열림에 대비해서 새로 표시.
            if (previewPanel != null && previewPanel.activeSelf)
            {
                RefreshPreview();
                Debug.Log("CustomStoryPreviewUI: Refreshed preview after customization reset.");
            }
        }

        // ───────── 진입/종료 ─────────

        public void OpenPreviewPanel()
        {
            if (ParentModeManager.Instance == null || !ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                Debug.Log("CustomStoryPreviewUI: Parent mode required. Redirecting to parent check.");
                ShowStatus(parentModeOnlyMessage);
                if (parentModeUI != null) parentModeUI.OpenParentCheck();
                return;
            }

            PopulateStageDropdown();
            PopulateTypeDropdown();
            PopulateLineDropdown();
            RefreshPreview();

            if (previewPanel != null) previewPanel.SetActive(true);
            Debug.Log("CustomStoryPreviewUI: Preview panel opened.");
        }

        public void ClosePreviewPanel()
        {
            if (previewPanel != null) previewPanel.SetActive(false);
            Debug.Log("CustomStoryPreviewUI: Preview panel closed.");
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
            int stageId = GetSelectedStageId();
            StoryDialogueType type = GetSelectedType();

            if (StoryManager.Instance != null && stageId > 0)
            {
                List<StoryDialogueLine> source = StoryManager.Instance.GetStageOriginalDialogues(stageId, type);
                if (source != null)
                {
                    for (int i = 0; i < source.Count; i++)
                    {
                        if (source[i] == null) continue;
                        lineCache.Add(source[i]);
                    }
                }
                else
                {
                    Debug.Log($"CustomStoryPreviewUI: StoryNode dialogues null for stage={stageId}, type={type}.");
                }
            }

            if (lineDropdown != null)
            {
                lineDropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < lineCache.Count; i++)
                {
                    StoryDialogueLine ln = lineCache[i];
                    string speaker = ResolveSpeakerNameForLabel(ln);
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

        // ───────── Dropdown 이벤트 ─────────

        private void OnStageChanged(int idx)
        {
            Debug.Log($"CustomStoryPreviewUI: Stage changed to index {idx} (stageId={GetSelectedStageId()}).");
            PopulateLineDropdown();
            RefreshPreview();
        }

        private void OnTypeChanged(int idx)
        {
            Debug.Log($"CustomStoryPreviewUI: Dialogue type changed to {GetSelectedType()}.");
            PopulateLineDropdown();
            RefreshPreview();
        }

        private void OnLineChanged(int idx)
        {
            Debug.Log($"CustomStoryPreviewUI: Line changed to index {idx}.");
            RefreshPreview();
        }

        private void OnPreviewProposedToggled(bool on)
        {
            Debug.Log($"CustomStoryPreviewUI: previewProposedToggle = {on}.");
            RefreshPreview();
        }

        // ───────── 미리보기 갱신 ─────────

        private void RefreshPreview()
        {
            int stageId = GetSelectedStageId();
            StoryDialogueType type = GetSelectedType();
            int lineIndex = GetSelectedLineIndex();

            StoryDialogueLine line = (lineIndex >= 0 && lineIndex < lineCache.Count) ? lineCache[lineIndex] : null;
            string original = line != null ? line.Dialogue : string.Empty;

            string proposed = string.Empty;
            string approved = string.Empty;
            bool isApproved = false;
            if (StoryDialogueOverrideManager.Instance != null && stageId > 0 && lineIndex >= 0)
            {
                proposed = StoryDialogueOverrideManager.Instance.GetProposedDialogue(stageId, type, lineIndex);
                approved = StoryDialogueOverrideManager.Instance.GetApprovedDialogue(stageId, type, lineIndex);
                isApproved = StoryDialogueOverrideManager.Instance.IsApproved(stageId, type, lineIndex);
            }

            // 비교 영역 (항상 표시)
            if (originalText != null) originalText.text = original ?? string.Empty;
            if (proposedText != null) proposedText.text = string.IsNullOrWhiteSpace(proposed) ? noProposedMessage : proposed;
            if (this.approvedText != null) this.approvedText.text = string.IsNullOrWhiteSpace(approved) ? noApprovedMessage : approved;

            // 실제 적용 기준 vs 제안문 임시 미리보기 결정
            bool wantsProposedPreview = previewProposedToggle != null && previewProposedToggle.isOn;
            string displayDialogue;
            string status;

            if (wantsProposedPreview && !string.IsNullOrWhiteSpace(proposed))
            {
                // UI 내부에서만 proposed를 보여준다. StoryManager의 적용 대사에는 영향 없음.
                displayDialogue = proposed;
                status = statusProposedPreviewing;
            }
            else if (isApproved && !string.IsNullOrWhiteSpace(approved))
            {
                displayDialogue = approved;
                status = statusApproved;
            }
            else if (!string.IsNullOrWhiteSpace(proposed))
            {
                // 제안만 있는 상태 — 실제 적용 미리보기에는 원본 표시
                displayDialogue = original;
                status = statusPendingNotApplied;
            }
            else
            {
                displayDialogue = original;
                status = statusUsingOriginal;
            }

            ApplyDisplayLine(line, displayDialogue);
            ShowStatus(status);
            UpdateApproveButtonInteractable(proposed, isApproved);

            if (line == null)
            {
                Debug.Log($"CustomStoryPreviewUI: No line selected (stage={stageId}, type={type}, idx={lineIndex}). Possibly missing StoryNode.");
            }
        }

        private void ApplyDisplayLine(StoryDialogueLine line, string dialogue)
        {
            if (dialogueText != null) dialogueText.text = dialogue ?? string.Empty;

            CharacterPackData pack = ResolveCharacterPack(line);

            if (speakerNameText != null)
            {
                string speaker = ResolveSpeakerName(line, pack);
                speakerNameText.text = speaker ?? string.Empty;
            }
            if (portraitImage != null)
            {
                Sprite sp = ResolvePortrait(line, pack);
                if (sp != null)
                {
                    portraitImage.sprite = sp;
                    portraitImage.enabled = true;
                }
                else
                {
                    // 기존 이미지 유지하되 없으면 비워둠
                    portraitImage.enabled = portraitImage.sprite != null;
                }
            }
        }

        private CharacterPackData ResolveCharacterPack(StoryDialogueLine line)
        {
            if (line == null) return null;
            if (CharacterPackManager.Instance == null) return null;
            string speakerId = line.SpeakerId;
            if (string.IsNullOrWhiteSpace(speakerId)) return null;
            return CharacterPackManager.Get(speakerId);
        }

        private string ResolveSpeakerName(StoryDialogueLine line, CharacterPackData pack)
        {
            // 별칭 우선: speakerId가 CharacterPack의 characterId와 일치하면 alias 적용
            if (pack != null && CharacterAliasManager.Instance != null)
            {
                string aliased = CharacterAliasManager.Instance.GetDisplayName(pack);
                if (!string.IsNullOrWhiteSpace(aliased)) return aliased;
            }
            // fallback: 원본 speakerName
            if (line != null && !string.IsNullOrWhiteSpace(line.SpeakerName)) return line.SpeakerName;
            // 마지막 fallback: pack의 원본 이름
            return pack != null ? pack.CharacterName : string.Empty;
        }

        private string ResolveSpeakerNameForLabel(StoryDialogueLine line)
        {
            if (line == null) return "?";
            // 드롭다운 라벨에서도 별칭 반영
            return ResolveSpeakerName(line, ResolveCharacterPack(line));
        }

        private Sprite ResolvePortrait(StoryDialogueLine line, CharacterPackData pack)
        {
            if (line != null && line.Portrait != null) return line.Portrait;
            if (pack != null && pack.ProfileSprite != null) return pack.ProfileSprite;
            return null;
        }

        private void UpdateApproveButtonInteractable(string proposed, bool isApproved)
        {
            if (approveButton == null) return;
            // 제안 문장이 있고 아직 승인되지 않았으며 부모 모드일 때만 활성화
            bool canApprove = !string.IsNullOrWhiteSpace(proposed)
                              && !isApproved
                              && ParentModeManager.Instance != null
                              && ParentModeManager.Instance.CanAccessParentOnlyMenu();
            approveButton.interactable = canApprove;
        }

        // ───────── 액션 ─────────

        public void ApproveProposed()
        {
            if (StoryDialogueOverrideManager.Instance == null) return;
            int stageId = GetSelectedStageId();
            StoryDialogueType type = GetSelectedType();
            int lineIndex = GetSelectedLineIndex();
            if (stageId <= 0 || lineIndex < 0) { ShowStatus(noLineMessage); return; }

            string proposed = StoryDialogueOverrideManager.Instance.GetProposedDialogue(stageId, type, lineIndex);
            if (string.IsNullOrWhiteSpace(proposed))
            {
                ShowStatus(approveFailMessage);
                Debug.Log("CustomStoryPreviewUI: Approve failed — no proposed text.");
                return;
            }

            bool ok = StoryDialogueOverrideManager.Instance.ApproveDialogue(stageId, type, lineIndex);
            if (!ok)
            {
                ShowStatus(parentModeOnlyMessage);
                Debug.Log("CustomStoryPreviewUI: Approve failed — parent mode required or manager rejected.");
                return;
            }
            Debug.Log($"CustomStoryPreviewUI: Approved (stage={stageId}, type={type}, line={lineIndex}).");
            ShowStatus(approveSuccessMessage);
            // 승인 후 미리보기 갱신
            RefreshPreview();
        }

        public void OpenEditUI()
        {
            if (storyEditUI == null)
            {
                Debug.Log("CustomStoryPreviewUI: storyEditUI not assigned. TODO: Connect preview screen to StoryDialogueEditUI.");
                return;
            }
            storyEditUI.OpenEditMenu();
        }

        /// <summary>
        /// 캐릭터 대표 대사 미리보기 (#15).
        /// 별칭 반영 이름 + 대표 대사 매니저의 표시문 + 프로필 스프라이트.
        /// 스토리 대사 미리보기와 동일 슬롯을 재사용한다. 비교 영역은 비워 둠.
        /// </summary>
        public void PreviewRepresentativeDialogue(CharacterPackData characterPack)
        {
            if (characterPack == null)
            {
                Debug.Log("CustomStoryPreviewUI: PreviewRepresentativeDialogue called with null pack.");
                return;
            }
            if (previewPanel != null) previewPanel.SetActive(true);

            string speaker = CharacterAliasManager.Instance != null
                ? CharacterAliasManager.Instance.GetDisplayName(characterPack)
                : characterPack.CharacterName;
            string text = CharacterRepresentativeDialogueManager.Instance != null
                ? CharacterRepresentativeDialogueManager.Instance.GetRepresentativeDialogueText(characterPack)
                : characterPack.CharacterName;
            Sprite sp = characterPack.ProfileSprite;

            if (speakerNameText != null) speakerNameText.text = speaker ?? string.Empty;
            if (dialogueText != null) dialogueText.text = text ?? string.Empty;
            if (portraitImage != null)
            {
                if (sp != null) { portraitImage.sprite = sp; portraitImage.enabled = true; }
                else portraitImage.enabled = portraitImage.sprite != null;
            }
            if (originalText != null) originalText.text = string.Empty;
            if (proposedText != null) proposedText.text = string.Empty;
            if (this.approvedText != null) this.approvedText.text = string.Empty;
            ShowStatus($"대표 대사 미리보기: {characterPack.CharacterId}");
            if (approveButton != null) approveButton.interactable = false;
        }

        // ───────── 선택값 헬퍼 ─────────

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

        private void ShowStatus(string msg)
        {
            if (statusText != null) statusText.text = msg ?? string.Empty;
        }
    }
}
